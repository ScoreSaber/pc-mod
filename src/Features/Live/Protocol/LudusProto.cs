using ProtoBuf;
using ScoreSaber.Live.V1;
using System;
using System.Collections.Generic;
using System.IO;
using ProtoLudusEnvelope = ScoreSaber.Live.V1.LudusEnvelope;

namespace ScoreSaber.Features.Live.Protocol {
    internal enum LudusEnvelopeType {
        Unknown,
        ConnectAccepted,
        RoomContextUpdated,
        HeartbeatAck,
        ReconnectRequested,
        RoomSnapshot,
        ServerCommand,
        ChatMessage,
        ChatSnapshot,
        Error
    }

    internal class DecodedLudusEnvelope {
        internal LudusEnvelopeType Type { get; set; }
        internal ulong Sequence { get; set; }
        internal string ConnectionId { get; set; }
        internal LudusRoomContextType RoomContext { get; set; }
        internal string TournamentId { get; set; }
        internal string CurrentMatchId { get; set; }
        internal int HeartbeatIntervalMs { get; set; }
        internal LudusClientType ClientType { get; set; } = LudusClientType.LudusClientTypePlayer;
        internal string ReconnectWebSocketUrl { get; set; }
        internal string ReconnectReason { get; set; }
        internal int ReconnectRetryAfterMs { get; set; }
        internal List<LiveMatchRoomState> Rooms { get; set; } = new List<LiveMatchRoomState>();
        internal ServerCommand ServerCommand { get; set; }
        internal LiveChatMessage ChatMessage { get; set; }
        internal LiveChatSnapshot ChatSnapshot { get; set; }
        internal string ErrorCode { get; set; }
        internal string ErrorMessage { get; set; }
        internal bool Retryable { get; set; }
    }

    internal static class LudusProto {
        private const int ProtocolVersion = 1;
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static byte[] EncodeConnect(
            string sessionId,
            string sessionKey,
            string tournamentId,
            string playerId,
            LivePlayerPlatform platform,
            string gameVersion,
            string clientVersion,
            LudusRoomContextType initialRoomContext,
            bool publicLivePresenceOptOut,
            ulong sequence) {

            return Encode(sequence, null, new ProtoLudusEnvelope {
                ConnectRequest = new ConnectRequest {
                    SessionId = sessionId ?? string.Empty,
                    SessionKey = sessionKey ?? string.Empty,
                    TournamentId = tournamentId ?? string.Empty,
                    PlayerId = playerId ?? string.Empty,
                    Platform = platform,
                    ClientType = LudusClientType.LudusClientTypePlayer,
                    GameVersion = gameVersion ?? string.Empty,
                    ClientVersion = clientVersion ?? string.Empty,
                    InitialRoomContext = initialRoomContext,
                    PublicLivePresenceOptOut = publicLivePresenceOptOut
                }
            });
        }

        internal static byte[] EncodeJoinRoom(string matchId, string roomId, ulong sequence, string connectionId) {
            return Encode(sequence, connectionId, new ProtoLudusEnvelope {
                JoinRoomRequest = new JoinRoomRequest {
                    MatchId = matchId ?? string.Empty,
                    RoomId = roomId ?? string.Empty
                }
            });
        }

        internal static byte[] EncodeSetRoomContext(LudusRoomContextType roomContext, string tournamentId, ulong sequence, string connectionId) {
            return Encode(sequence, connectionId, new ProtoLudusEnvelope {
                SetRoomContextRequest = new SetRoomContextRequest {
                    RoomContext = roomContext,
                    TournamentId = tournamentId ?? string.Empty
                }
            });
        }

        internal static byte[] EncodeSetClientType(LudusClientType clientType, ulong sequence, string connectionId) {
            return Encode(sequence, connectionId, new ProtoLudusEnvelope {
                SetClientTypeRequest = new SetClientTypeRequest {
                    ClientType = clientType
                }
            });
        }

        internal static byte[] EncodeLeaveRoom(string matchId, ulong sequence, string connectionId) {
            return Encode(sequence, connectionId, new ProtoLudusEnvelope {
                LeaveRoomRequest = new LeaveRoomRequest {
                    MatchId = matchId ?? string.Empty
                }
            });
        }

        internal static byte[] EncodePresence(
            LudusPlayState playState,
            LudusDownloadState downloadState,
            string currentRoomId,
            string currentMapHash,
            ulong sequence,
            string connectionId) {

            return Encode(sequence, connectionId, new ProtoLudusEnvelope {
                PresenceUpdate = new PresenceUpdate {
                    PlayState = playState,
                    DownloadState = downloadState,
                    CurrentRoomId = currentRoomId ?? string.Empty,
                    CurrentMapHash = currentMapHash ?? string.Empty
                }
            });
        }

        internal static byte[] EncodeReplayPacket(ReplayStreamPacket packet, ulong sequence, string connectionId) {
            packet.ConnectionId = connectionId ?? string.Empty;
            return Encode(sequence, connectionId, new ProtoLudusEnvelope {
                ReplayPacket = packet
            });
        }

        internal static byte[] EncodeReadyState(string matchId, bool ready, ulong sequence, string connectionId) {
            return Encode(sequence, connectionId, new ProtoLudusEnvelope {
                ReadyStateUpdate = new ReadyStateUpdate {
                    MatchId = matchId ?? string.Empty,
                    ReadyState = ready ? LudusReadyState.LudusReadyStateReady : LudusReadyState.LudusReadyStateNotReady
                }
            });
        }

        internal static byte[] EncodeDownloadState(string matchId, LudusDownloadState state, string errorMessage, ulong sequence, string connectionId) {
            return Encode(sequence, connectionId, new ProtoLudusEnvelope {
                DownloadStateUpdate = new DownloadStateUpdate {
                    MatchId = matchId ?? string.Empty,
                    DownloadState = state,
                    ErrorMessage = errorMessage ?? string.Empty
                }
            });
        }

        internal static byte[] EncodePromptResponse(
            string commandId,
            string matchId,
            string playerId,
            bool accepted,
            ulong sequence,
            string connectionId) {

            return Encode(sequence, connectionId, new ProtoLudusEnvelope {
                PromptResponse = new PromptResponse {
                    CommandId = commandId ?? string.Empty,
                    MatchId = matchId ?? string.Empty,
                    PlayerId = playerId ?? string.Empty,
                    Accepted = accepted,
                    RespondedAtUnixMs = UnixNowMs()
                }
            });
        }

        internal static byte[] EncodeChatMessage(string matchId, string text, string senderDisplayName, ulong sequence, string connectionId) {
            return Encode(sequence, connectionId, new ProtoLudusEnvelope {
                ChatMessageRequest = new LiveChatMessageRequest {
                    MatchId = matchId ?? string.Empty,
                    Text = text ?? string.Empty,
                    SenderDisplayName = senderDisplayName ?? string.Empty
                }
            });
        }

        internal static byte[] EncodeHeartbeat(ulong lastReceivedSequence, ulong sequence, string connectionId) {
            return Encode(sequence, connectionId, new ProtoLudusEnvelope {
                Heartbeat = new Heartbeat {
                    LastReceivedSequence = lastReceivedSequence
                }
            });
        }

        internal static DecodedLudusEnvelope Decode(byte[] bytes) {
            ProtoLudusEnvelope frame;
            try {
                using (var stream = new MemoryStream(bytes)) {
                    frame = Serializer.Deserialize<ProtoLudusEnvelope>(stream);
                }
            } catch (Exception ex) {
                Plugin.Log.Warn($"Failed to parse ludus protobuf frame: {ex.Message}");
                return null;
            }

            var envelope = new DecodedLudusEnvelope {
                Sequence = frame.Sequence
            };

            if (frame.ConnectAccepted != null) {
                envelope.Type = LudusEnvelopeType.ConnectAccepted;
                envelope.ConnectionId = frame.ConnectAccepted.ConnectionId;
                envelope.RoomContext = frame.ConnectAccepted.RoomContext;
                envelope.TournamentId = frame.ConnectAccepted.TournamentId;
                envelope.CurrentMatchId = frame.ConnectAccepted.CurrentMatchId;
                envelope.HeartbeatIntervalMs = (int)frame.ConnectAccepted.HeartbeatIntervalMs;
                envelope.ClientType = frame.ConnectAccepted.ClientType;
                return envelope;
            }

            if (frame.RoomContextUpdated != null) {
                envelope.Type = LudusEnvelopeType.RoomContextUpdated;
                envelope.RoomContext = frame.RoomContextUpdated.RoomContext;
                envelope.TournamentId = frame.RoomContextUpdated.TournamentId;
                envelope.CurrentMatchId = frame.RoomContextUpdated.CurrentMatchId;
                envelope.ClientType = frame.RoomContextUpdated.ClientType;
                return envelope;
            }

            if (frame.HeartbeatAck != null) {
                envelope.Type = LudusEnvelopeType.HeartbeatAck;
                return envelope;
            }

            if (frame.ReconnectRequested != null) {
                envelope.Type = LudusEnvelopeType.ReconnectRequested;
                envelope.ReconnectWebSocketUrl = frame.ReconnectRequested.WebsocketUrl;
                envelope.ReconnectReason = frame.ReconnectRequested.Reason;
                envelope.ReconnectRetryAfterMs = (int)frame.ReconnectRequested.RetryAfterMs;
                return envelope;
            }

            if (frame.RoomSnapshot != null) {
                envelope.Type = LudusEnvelopeType.RoomSnapshot;
                envelope.Rooms = frame.RoomSnapshot.Rooms ?? new List<LiveMatchRoomState>();
                return envelope;
            }

            if (frame.ServerCommand != null) {
                envelope.Type = LudusEnvelopeType.ServerCommand;
                envelope.ServerCommand = frame.ServerCommand;
                return envelope;
            }

            if (frame.ChatMessage != null) {
                envelope.Type = LudusEnvelopeType.ChatMessage;
                envelope.ChatMessage = frame.ChatMessage;
                return envelope;
            }

            if (frame.ChatSnapshot != null) {
                envelope.Type = LudusEnvelopeType.ChatSnapshot;
                envelope.ChatSnapshot = frame.ChatSnapshot;
                return envelope;
            }

            if (frame.Error != null) {
                envelope.Type = LudusEnvelopeType.Error;
                envelope.ErrorCode = frame.Error.Code;
                envelope.ErrorMessage = frame.Error.Message;
                envelope.Retryable = frame.Error.Retryable;
                return envelope;
            }

            envelope.Type = LudusEnvelopeType.Unknown;
            return envelope;
        }

        private static byte[] Encode(ulong sequence, string connectionId, ProtoLudusEnvelope envelope) {
            envelope.ProtocolVersion = ProtocolVersion;
            envelope.MessageId = Guid.NewGuid().ToString("N");
            envelope.ConnectionId = connectionId ?? string.Empty;
            envelope.Sequence = sequence;
            envelope.ClientTimeUnixMs = UnixNowMs();
            using (var stream = new MemoryStream()) {
                Serializer.Serialize(stream, envelope);
                return stream.ToArray();
            }
        }

        private static long UnixNowMs() {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
        }
    }
}
