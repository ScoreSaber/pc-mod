using ScoreSaber.Core.Api;
using ScoreSaber.Core.Platform;
using ScoreSaber.Core.Api.UploadTrust;
using ScoreSaber.Features.Players.Domain;
using ScoreSaber.Core.Presentation;
using ScoreSaber.Core;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Players.Services {
    internal class GameSessionService {

        public LocalPlayerInfo LocalPlayerInfo { get; private set; }
        public GameSession GameSession { get; private set; }
        public LoginStatus Status { get; private set; }
        public string StatusText { get; private set; } = string.Empty;
        internal bool HasAuthenticatedSession => LocalPlayerInfo != null && GameSession != null && GameSession.IsAuthenticated;
        internal bool CanUseUploadProtocolV2 => _buildMetadata.IsOfficial || _buildMetadata.IsDevelopment;
        public event Action<LoginStatus, string> LoginStatusChanged;
        private readonly IScoreSaberApiClient _apiClient;
        private readonly PlatformUserService _platformUser;
        private readonly UploadTrustBuildMetadata _buildMetadata;
        private Task<bool> _signInTask;

        public enum LoginStatus {
            None = 0,
            InProgress = 1,
            Error = 2,
            Success = 3
        }

        public GameSessionService(IScoreSaberApiClient apiClient, PlatformUserService platformUser) {
            _apiClient = apiClient;
            _platformUser = platformUser;
            _buildMetadata = UploadTrustBuildMetadata.FromAssembly(typeof(Plugin).Assembly);
            Plugin.Log.Debug("GameSessionService Setup!");
        }

        public void ChangeLoginStatus(LoginStatus loginStatus, string status) => ChangeLoginStatus(loginStatus, status, true);

        private void ChangeLoginStatus(LoginStatus loginStatus, string status, bool notify) {
            Status = loginStatus;
            StatusText = status;
            if (notify) {
                LoginStatusChanged?.Invoke(Status, StatusText);
            }
        }

        public void EnsureAuthenticated() => EnsureAuthenticated(false, CancellationToken.None).RunTask();

        internal Task<bool> EnsureAuthenticated(bool forceRefresh, CancellationToken cancellationToken) => EnsureAuthenticated(forceRefresh, cancellationToken, true);

        internal Task<bool> EnsureAuthenticated(bool forceRefresh, CancellationToken cancellationToken, bool notifyStatus) {
            if (!forceRefresh && HasAuthenticatedSession) {
                return Task.FromResult(true);
            }

            if (_signInTask != null && !_signInTask.IsCompleted) {
                return _signInTask;
            }

            _signInTask = SignIn(cancellationToken, notifyStatus);
            return _signInTask;
        }

        internal Task<bool> RefreshUploadTrust(CancellationToken cancellationToken) {
            if (!HasAuthenticatedSession) {
                return EnsureAuthenticated(false, cancellationToken, false);
            }

            if (_signInTask != null && !_signInTask.IsCompleted) {
                return _signInTask;
            }

            _signInTask = RefreshUploadTrustSession(cancellationToken);
            return _signInTask;
        }

        private async Task<bool> SignIn(CancellationToken cancellationToken, bool notifyStatus) {

            if (Status == LoginStatus.InProgress) {
                return false;
            }

            bool updateLoginStatus = notifyStatus || !HasAuthenticatedSession;
            if (updateLoginStatus) {
                ChangeLoginStatus(LoginStatus.InProgress, "Signing into ScoreSaber...", notifyStatus);
            }

            var playerInfo = _buildMetadata.HasDevelopmentAuth
                ? await CreateDevelopmentPlayerInfo(cancellationToken)
                : await CreatePlatformPlayerInfo(cancellationToken);

            int attempts = 1;

            while (attempts < 4) {

                var authenticated = await AuthenticateWithScoreSaber(playerInfo);

                if (authenticated) {
                    LocalPlayerInfo = playerInfo;
                    string successText = PlayerPresentation.GetLoginSuccessText(LocalPlayerInfo.playerId);
                    if (updateLoginStatus) {
                        ChangeLoginStatus(LoginStatus.Success, successText, notifyStatus);
                    }
                    return true;
                }

                if (updateLoginStatus) {
                    ChangeLoginStatus(LoginStatus.InProgress, $"Failed, attempting again ({attempts} of 3 tries...)", notifyStatus);
                }
                attempts++;
                await Task.Delay(4000, cancellationToken);
            }

            if (updateLoginStatus && Status != LoginStatus.Success) {
                ChangeLoginStatus(LoginStatus.Error, "Failed to authenticate with ScoreSaber! Please restart your game", notifyStatus);
            }

            return false;
        }

        private Task<bool> RefreshUploadTrustSession(CancellationToken cancellationToken) => AuthenticateWithScoreSaber(LocalPlayerInfo);

        private async Task<LocalPlayerInfo> CreatePlatformPlayerInfo(CancellationToken cancellationToken) {
            var authToken = await _platformUser.GetAuthToken();
            var userInfo = await _platformUser.GetUserInfo(cancellationToken);

            var nonce = string.Empty;
            var platform = string.Empty;

            switch (userInfo.platform) {
                case UserInfo.Platform.Steam:
                    nonce = authToken;
                    platform = "0";
                    break;
                case UserInfo.Platform.Oculus:
                    nonce = authToken + "," + await _platformUser.GetXPlatformAccessToken(cancellationToken);
                    platform = "1";
                    break;
            }

            var playerId = userInfo.platformUserId;
            var playerName = userInfo.userName;
            var friendIds = await _platformUser.GetFriendsUserIds();
            var friends = string.Join(",", friendIds.Where(x => x != "0"));

            return new LocalPlayerInfo(playerId, playerName, friends, platform, nonce);
        }

        private async Task<LocalPlayerInfo> CreateDevelopmentPlayerInfo(CancellationToken cancellationToken) {
            string playerId = _buildMetadata.DevelopmentPlayerId;
            string playerName = _buildMetadata.DevelopmentPlayerName;
            string friends = string.Empty;

            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(playerName)) {
                var userInfo = await _platformUser.GetUserInfo(cancellationToken);
                if (string.IsNullOrEmpty(playerId)) {
                    playerId = userInfo.platformUserId;
                }
                if (string.IsNullOrEmpty(playerName)) {
                    playerName = userInfo.userName;
                }
            }

            return new LocalPlayerInfo(playerId, playerName, friends, "3", _buildMetadata.DevelopmentAuthNonce);
        }

        private async Task<bool> AuthenticateWithScoreSaber(LocalPlayerInfo playerInfo) {


            try {
                Plugin.Log.Debug($"Authenticating ScoreSaber player {playerInfo.playerId} with {CountFriendIds(playerInfo.playerFriends)} friends");
                var request = new GameAuthenticationRequest {
                    AuthType = Convert.ToInt32(playerInfo.authType),
                    PlayerId = playerInfo.playerId,
                    Nonce = playerInfo.playerNonce,
                    FriendIds = playerInfo.playerFriends,
                    PlayerName = playerInfo.playerName
                };

                using (var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(30))) {
                    GameAuthenticationResult authResult = await _apiClient.AuthenticateGame(request, cancellationSource.Token);

                    if (authResult.Status != GameSessionStatus.Success || authResult.Session == null || !authResult.Session.IsAuthenticated) {
                        Plugin.Log.Error($"Failed user authentication: {FormatAuthenticationError(authResult)}");
                        return false;
                    }

                    GameSession = authResult.Session;
                    playerInfo.playerKey = authResult.Session.SessionKey;
                    return true;
                }
            } catch (OperationCanceledException) {
                Plugin.Log.Error("Failed user authentication: timed out");
                return false;
            } catch (Exception ex) {
                Plugin.Log.Error($"Failed user authentication: {ex.Message}");
                return false;
            }
        }

        private static int CountFriendIds(string friendIds) => string.IsNullOrEmpty(friendIds) ? 0 : friendIds.Split(',').Length;

        private static string FormatAuthenticationError(GameAuthenticationResult authResult) {
            if (authResult.Error == null) {
                return authResult.Message;
            }

            string message = string.IsNullOrEmpty(authResult.Error.Message) ? authResult.Message : authResult.Error.Message;
            if (authResult.Error.StatusCode == 0) {
                return message;
            }

            return $"{message} (status {authResult.Error.StatusCode})";
        }
    }
}
