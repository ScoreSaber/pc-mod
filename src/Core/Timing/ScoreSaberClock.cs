using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Zenject;

namespace ScoreSaber.Core.Timing {
    internal sealed class ScoreSaberClock : IInitializable, IDisposable {
        private const int NtpPort = 123;
        private const int NtpPacketSize = 48;
        private const int NtpTimeoutMs = 1500;
        private const int MaxAcceptedRoundTripMs = 5000;
        private const long NtpUnixEpochOffsetSeconds = 2208988800L;
        private const long LudusServerTimeAccuracyMs = 1000;
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(30);
        private static readonly string[] NtpServers = {
            "time.cloudflare.com",
            "time.google.com",
            "pool.ntp.org",
            "time.windows.com"
        };

        private readonly object _lock = new object();
        private CancellationTokenSource _cancellation;
        private bool _hasNetworkTime;
        private long _anchorUnixMs;
        private long _anchorStopwatchTicks;
        private long _lastSyncStopwatchTicks;
        private long _accuracyMs = long.MaxValue;

        public void Initialize() {
            _cancellation = new CancellationTokenSource();
            RefreshLoop(_cancellation.Token).RunTask();
        }

        public void Dispose() {
            try {
                _cancellation?.Cancel();
                _cancellation?.Dispose();
            } catch (Exception ex) {
                Plugin.Log.Warn($"ScoreSaber clock: failed to stop sync loop: {ex.Message}");
            }

            _cancellation = null;
        }

        internal long UnixTimeMilliseconds() {
            bool hasNetworkTime;
            long anchorUnixMs;
            long anchorStopwatchTicks;
            lock (_lock) {
                hasNetworkTime = _hasNetworkTime;
                anchorUnixMs = _anchorUnixMs;
                anchorStopwatchTicks = _anchorStopwatchTicks;
            }

            if (!hasNetworkTime) {
                return LocalUnixTimeMilliseconds();
            }

            return anchorUnixMs + ElapsedMilliseconds(anchorStopwatchTicks, Stopwatch.GetTimestamp());
        }

        internal long MonotonicMilliseconds() {
            return StopwatchTicksToMilliseconds(Stopwatch.GetTimestamp());
        }

        internal void RecordLudusServerTime(long serverTimeUnixMs) {
            if (serverTimeUnixMs <= 0) {
                return;
            }

            long localUnixMs = LocalUnixTimeMilliseconds();
            var sample = new ClockSample(
                serverTimeUnixMs,
                Stopwatch.GetTimestamp(),
                LudusServerTimeAccuracyMs,
                serverTimeUnixMs - localUnixMs,
                "Ludus server");
            ApplySample(sample, false);
        }

        private async Task RefreshLoop(CancellationToken cancellationToken) {
            bool failureLogged = false;

            while (!cancellationToken.IsCancellationRequested) {
                bool synchronized = await TrySynchronize(cancellationToken);
                if (synchronized) {
                    failureLogged = false;
                } else if (!failureLogged) {
                    failureLogged = true;
                    Plugin.Log.Warn("ScoreSaber clock: NTP sync failed; using local clock until retry.");
                }

                TimeSpan delay = synchronized ? RefreshInterval : RetryInterval;
                await Task.Delay(delay, cancellationToken);
            }
        }

        private async Task<bool> TrySynchronize(CancellationToken cancellationToken) {
            try {
                Task<ClockSample>[] tasks = NtpServers.Select(server => QueryNtpServerAsync(server, cancellationToken)).ToArray();
                ClockSample[] samples = await Task.WhenAll(tasks);
                ClockSample bestSample = samples
                    .Where(sample => sample != null)
                    .OrderBy(sample => sample.AccuracyMs)
                    .FirstOrDefault();

                if (bestSample == null) {
                    return false;
                }

                ApplySample(bestSample, true);
                return true;
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                Plugin.Log.Debug($"ScoreSaber clock: NTP sync failed: {ex.Message}");
                return false;
            }
        }

        private static Task<ClockSample> QueryNtpServerAsync(string server, CancellationToken cancellationToken) {
            return Task.Run(() => QueryNtpServer(server, cancellationToken), cancellationToken);
        }

        private static ClockSample QueryNtpServer(string server, CancellationToken cancellationToken) {
            try {
                foreach (IPAddress address in Dns.GetHostAddresses(server)) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (address.AddressFamily != AddressFamily.InterNetwork && address.AddressFamily != AddressFamily.InterNetworkV6) {
                        continue;
                    }

                    ClockSample sample = QueryNtpAddress(server, address, cancellationToken);
                    if (sample != null) {
                        return sample;
                    }
                }
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                Plugin.Log.Debug($"ScoreSaber clock: {server} did not answer NTP: {ex.Message}");
            }

            return null;
        }

        private static ClockSample QueryNtpAddress(string server, IPAddress address, CancellationToken cancellationToken) {
            try {
                using (var client = new UdpClient(address.AddressFamily)) {
                    client.Client.ReceiveTimeout = NtpTimeoutMs;
                    client.Client.SendTimeout = NtpTimeoutMs;

                    byte[] request = new byte[NtpPacketSize];
                    request[0] = 0x23;
                    long t1UnixMs = LocalUnixTimeMilliseconds();
                    WriteNtpTimestamp(request, 40, t1UnixMs);

                    client.Send(request, request.Length, new IPEndPoint(address, NtpPort));
                    cancellationToken.ThrowIfCancellationRequested();

                    IPEndPoint remoteEndpoint = new IPEndPoint(address.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
                    byte[] response = client.Receive(ref remoteEndpoint);
                    long t4UnixMs = LocalUnixTimeMilliseconds();
                    long t4StopwatchTicks = Stopwatch.GetTimestamp();

                    if (!IsValidNtpResponse(response)) {
                        return null;
                    }

                    long t2UnixMs = ReadNtpTimestamp(response, 32);
                    long t3UnixMs = ReadNtpTimestamp(response, 40);
                    long roundTripMs = (t4UnixMs - t1UnixMs) - (t3UnixMs - t2UnixMs);
                    if (roundTripMs < 0 || roundTripMs > MaxAcceptedRoundTripMs) {
                        return null;
                    }

                    long offsetMs = ((t2UnixMs - t1UnixMs) + (t3UnixMs - t4UnixMs)) / 2;
                    return new ClockSample(t4UnixMs + offsetMs, t4StopwatchTicks, roundTripMs, offsetMs, $"NTP {server}");
                }
            } catch (SocketException) {
            } catch (ObjectDisposedException) {
            }

            return null;
        }

        private void ApplySample(ClockSample sample, bool logSync) {
            bool applied = false;
            lock (_lock) {
                long ageMs = _hasNetworkTime ? ElapsedMilliseconds(_lastSyncStopwatchTicks, sample.StopwatchTicks) : long.MaxValue;
                if (!_hasNetworkTime || sample.AccuracyMs < _accuracyMs || ageMs >= (long)RefreshInterval.TotalMilliseconds) {
                    _hasNetworkTime = true;
                    _anchorUnixMs = sample.UnixMs;
                    _anchorStopwatchTicks = sample.StopwatchTicks;
                    _lastSyncStopwatchTicks = sample.StopwatchTicks;
                    _accuracyMs = sample.AccuracyMs;
                    applied = true;
                }
            }

            if (applied && logSync) {
                Plugin.Log.Info($"ScoreSaber clock: synchronized with {sample.Source} (offset {sample.OffsetMs:+0;-0;0}ms, accuracy ~= {sample.AccuracyMs}ms).");
            }
        }

        private static bool IsValidNtpResponse(byte[] response) {
            if (response == null || response.Length < NtpPacketSize) {
                return false;
            }

            int leapIndicator = (response[0] >> 6) & 0x3;
            int mode = response[0] & 0x7;
            int stratum = response[1];
            return leapIndicator != 3 && (mode == 4 || mode == 5) && stratum > 0;
        }

        private static long LocalUnixTimeMilliseconds() {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static long ReadNtpTimestamp(byte[] bytes, int offset) {
            ulong seconds =
                ((ulong)bytes[offset] << 24) |
                ((ulong)bytes[offset + 1] << 16) |
                ((ulong)bytes[offset + 2] << 8) |
                bytes[offset + 3];
            ulong fraction =
                ((ulong)bytes[offset + 4] << 24) |
                ((ulong)bytes[offset + 5] << 16) |
                ((ulong)bytes[offset + 6] << 8) |
                bytes[offset + 7];

            long unixSeconds = (long)seconds - NtpUnixEpochOffsetSeconds;
            long fractionMs = (long)((fraction * 1000UL) >> 32);
            return unixSeconds * 1000L + fractionMs;
        }

        private static void WriteNtpTimestamp(byte[] bytes, int offset, long unixMs) {
            ulong seconds = (ulong)(unixMs / 1000L + NtpUnixEpochOffsetSeconds);
            ulong fraction = (ulong)((unixMs % 1000L) * 0x100000000L / 1000L);

            bytes[offset] = (byte)(seconds >> 24);
            bytes[offset + 1] = (byte)(seconds >> 16);
            bytes[offset + 2] = (byte)(seconds >> 8);
            bytes[offset + 3] = (byte)seconds;
            bytes[offset + 4] = (byte)(fraction >> 24);
            bytes[offset + 5] = (byte)(fraction >> 16);
            bytes[offset + 6] = (byte)(fraction >> 8);
            bytes[offset + 7] = (byte)fraction;
        }

        private static long ElapsedMilliseconds(long startTicks, long endTicks) {
            long elapsedTicks = endTicks - startTicks;
            return elapsedTicks / Stopwatch.Frequency * 1000L + elapsedTicks % Stopwatch.Frequency * 1000L / Stopwatch.Frequency;
        }

        private static long StopwatchTicksToMilliseconds(long ticks) {
            return ticks / Stopwatch.Frequency * 1000L + ticks % Stopwatch.Frequency * 1000L / Stopwatch.Frequency;
        }

        private sealed class ClockSample {
            internal ClockSample(long unixMs, long stopwatchTicks, long accuracyMs, long offsetMs, string source) {
                UnixMs = unixMs;
                StopwatchTicks = stopwatchTicks;
                AccuracyMs = Math.Max(0, accuracyMs);
                OffsetMs = offsetMs;
                Source = source;
            }

            internal long UnixMs { get; }
            internal long StopwatchTicks { get; }
            internal long AccuracyMs { get; }
            internal long OffsetMs { get; }
            internal string Source { get; }
        }
    }
}
