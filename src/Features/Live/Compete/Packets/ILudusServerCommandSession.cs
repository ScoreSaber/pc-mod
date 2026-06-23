using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Compete.Services;
using ScoreSaber.Live.V1;
using System.Collections.Generic;
using System.Threading;

namespace ScoreSaber.Features.Live.Compete.Packets {
    internal interface ILudusServerCommandSession {
        string LocalPlayerId { get; }
        CompeteRoom TournamentRoom { get; set; }
        CancellationToken ConnectionCancellationToken { get; }
        CompeteSongService SongService { get; }
        CompeteDirectoryService DirectoryService { get; }
        CompeteGameplayLauncher GameplayLauncher { get; }
        CompeteGameplayControl GameplayControl { get; }
        void NotifyPlayerFollowRequested(int viewerCount);
        void NotifyViewersUpdated(IReadOnlyList<LiveRoomViewerState> viewers);
        void NotifyRoomUpdated(CompeteRoom room);
        void NotifyPromptReceived(CompeteOrganizerPrompt prompt);
        void NotifyStatusChanged(string status);
        void SendDownloadState(LudusDownloadState state, string errorMessage = "");
        void SendPresence(LudusPlayState playState, LudusDownloadState downloadState, string currentMapHash);
        CancellationToken BeginMapStartCountdown(string matchId, int delayMs, CancellationToken cancellationToken);
        bool TryCancelPendingMapStart(string matchId);
        void CompletePendingMapStart(string matchId);
    }
}
