using ScoreSaber.Core;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Live.Ludus.Services {
    internal sealed class LudusSessionTransport {
        private readonly LudusMainThreadQueue _mainThread;
        private readonly object _sendTaskLock = new object();
        private ClientWebSocket _socket;
        private CancellationTokenSource _cancellation;
        private Task _sendTask = Task.CompletedTask;

        internal LudusSessionTransport(LudusMainThreadQueue mainThread) {
            _mainThread = mainThread;
        }

        internal event Action<byte[]> MessageReceived;
        internal event Action<string> ReceiveFailed;
        internal event Action<string> SendFailed;
        internal event Action<string> ReconnectRequested;
        internal event Action Disconnected;

        internal bool IsOpen => _socket != null && _socket.State == WebSocketState.Open;
        internal CancellationToken Token => _cancellation?.Token ?? CancellationToken.None;

        internal void Prepare() {
            DisposeSocket();
            _cancellation = new CancellationTokenSource();
            _socket = new ClientWebSocket();
        }

        internal Task ConnectAsync(Uri uri) {
            return _socket.ConnectAsync(uri, Token);
        }

        internal void StartReceiveLoop() {
            ReceiveLoop(_socket, _cancellation).RunTask();
        }

        internal void Send(byte[] bytes) {
            if (bytes == null || bytes.Length == 0) {
                return;
            }

            ClientWebSocket socket = _socket;
            CancellationTokenSource cancellation = _cancellation;
            lock (_sendTaskLock) {
                QueueSendTask(SendAfter(PendingSendTask(), socket, cancellation, bytes));
            }
        }

        internal bool SendDeferred(Func<byte[]> bytesFactory) {
            if (bytesFactory == null) {
                return false;
            }

            ClientWebSocket socket = _socket;
            CancellationTokenSource cancellation = _cancellation;
            lock (_sendTaskLock) {
                QueueSendTask(SendAfter(PendingSendTask(), socket, cancellation, bytesFactory));
            }

            return true;
        }

        internal void DisposeSocket() {
            CancellationTokenSource cancellation = _cancellation;
            try {
                cancellation?.Cancel();
            } catch (Exception ex) {
                Plugin.Log.Warn($"Failed to cancel ludus socket: {ex.Message}");
            }

            try {
                _socket?.Dispose();
            } catch (Exception ex) {
                Plugin.Log.Warn($"Failed to close ludus socket: {ex.Message}");
            } finally {
                cancellation?.Dispose();
            }

            _socket = null;
            _cancellation = null;
            lock (_sendTaskLock) {
                _sendTask = Task.CompletedTask;
            }
        }

        private async Task ReceiveLoop(ClientWebSocket socket, CancellationTokenSource cancellation) {
            byte[] buffer = new byte[64 * 1024];
            var message = new List<byte>();

            try {
                while (socket.State == WebSocketState.Open) {
                    WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellation.Token);
                    if (result.MessageType == WebSocketMessageType.Close) {
                        break;
                    }

                    for (int i = 0; i < result.Count; i++) {
                        message.Add(buffer[i]);
                    }

                    if (result.EndOfMessage) {
                        byte[] bytes = message.ToArray();
                        message.Clear();
                        _mainThread.Enqueue(() => MessageReceived?.Invoke(bytes));
                    }
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                _mainThread.Enqueue(() => ReceiveFailed?.Invoke(ex.Message));
            }

            _mainThread.Enqueue(() => {
                if (_socket == socket) {
                    Disconnected?.Invoke();
                }
            });
        }

        private async Task SendAfter(Task previousSend, ClientWebSocket socket, CancellationTokenSource cancellation, Func<byte[]> bytesFactory) {
            try {
                await previousSend.ConfigureAwait(false);
            } catch {
            }

            if (socket != _socket || cancellation != _cancellation) {
                return;
            }

            if (!CanSendToSocket(socket, cancellation)) {
                _mainThread.Enqueue(() => ReconnectRequested?.Invoke("socket is not open"));
                return;
            }

            byte[] bytes;
            try {
                bytes = bytesFactory();
            } catch (Exception ex) {
                _mainThread.Enqueue(() => SendFailed?.Invoke(ex.Message));
                return;
            }

            await SendAsync(socket, cancellation, bytes).ConfigureAwait(false);
        }

        private async Task SendAfter(Task previousSend, ClientWebSocket socket, CancellationTokenSource cancellation, byte[] bytes) {
            try {
                await previousSend.ConfigureAwait(false);
            } catch {
            }

            await SendAsync(socket, cancellation, bytes).ConfigureAwait(false);
        }

        private async Task SendAsync(ClientWebSocket socket, CancellationTokenSource cancellation, byte[] bytes) {
            if (!CanSendToSocket(socket, cancellation)) {
                _mainThread.Enqueue(() => ReconnectRequested?.Invoke("socket is not open"));
                return;
            }

            if (bytes == null || bytes.Length == 0) {
                return;
            }

            if (socket != _socket || cancellation != _cancellation) {
                return;
            }

            try {
                // replay streaming can queue many chunks, and ClientWebSocket allows one send at a time.
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, cancellation.Token).ConfigureAwait(false);
            } catch (OperationCanceledException) {
            } catch (ObjectDisposedException) {
            } catch (Exception ex) {
                _mainThread.Enqueue(() => {
                    SendFailed?.Invoke(ex.Message);
                    ReconnectRequested?.Invoke(ex.Message);
                });
            }
        }

        private static bool CanSendToSocket(ClientWebSocket socket, CancellationTokenSource cancellation) {
            return socket != null && cancellation != null && socket.State == WebSocketState.Open;
        }

        private Task PendingSendTask() {
            if (_sendTask.IsCompleted) {
                _sendTask = Task.CompletedTask;
            }

            return _sendTask;
        }

        private void QueueSendTask(Task sendTask) {
            _sendTask = sendTask;
            sendTask.ContinueWith(task => {
                lock (_sendTaskLock) {
                    if (ReferenceEquals(_sendTask, task)) {
                        _sendTask = Task.CompletedTask;
                    }
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            sendTask.RunTask();
        }

    }
}
