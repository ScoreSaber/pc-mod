using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#if BEAT_SABER_1_42_0
using OculusStudios.Platform.Core;
#endif

namespace ScoreSaber.Core.Platform {
#if BEAT_SABER_1_42_0
    internal class PlatformUserService {
        private readonly IPlatform _platform;

        public PlatformUserService(IPlatform platform) {
            _platform = platform;
        }

        public Task<UserInfo> GetUserInfo(CancellationToken _) {
            UserInfo.Platform platform;
            switch (_platform.key) {
                case "steam":
                    platform = UserInfo.Platform.Steam;
                    break;
                case "oculus":
                case "oculus-mock":
                    platform = UserInfo.Platform.Oculus;
                    break;
                default:
                    platform = UserInfo.Platform.Test;
                    break;
            }
            return Task.FromResult(new UserInfo(platform, _platform.user.userId.ToString(), _platform.user.displayName));
        }

        public Task<string> GetAuthToken() => _platform.user.GetAccessTokenAsync();

        public Task<string> GetXPlatformAccessToken(CancellationToken _) => _platform.user.GetXPlatformAccessTokenAsync(false);

        public Task<IReadOnlyList<string>> GetFriendsUserIds() {
            return Task.FromResult(_platform.key == "steam" ? MakeSteamFriendsUserIds() : new string[0]);
        }

        // same friend lookup the old Steam model used
        private static IReadOnlyList<string> MakeSteamFriendsUserIds() {
            int friendCount = Steamworks.SteamFriends.GetFriendCount(Steamworks.EFriendFlags.k_EFriendFlagAll);
            var ids = new List<string>(friendCount);
            for (int i = 0; i < friendCount; i++) {
                ids.Add(Steamworks.SteamFriends.GetFriendByIndex(i, Steamworks.EFriendFlags.k_EFriendFlagImmediate).m_SteamID.ToString());
            }
            return ids;
        }
    }
#else
    internal class PlatformUserService {
        private readonly IPlatformUserModel _platformUserModel;

        public PlatformUserService(IPlatformUserModel platformUserModel) {
            _platformUserModel = platformUserModel;
        }

#if BEAT_SABER_1_29_0
        public Task<UserInfo> GetUserInfo(CancellationToken _) => _platformUserModel.GetUserInfo();

        // 1.29 can't request this token through IPlatformUserModel, so ask Oculus directly
        public Task<string> GetXPlatformAccessToken(CancellationToken _) {
            var tcs = new TaskCompletionSource<string>();
            Oculus.Platform.Users.GetAccessToken().OnComplete(message => tcs.TrySetResult(message.IsError ? string.Empty : message.Data));
            return tcs.Task;
        }
#else
        public Task<UserInfo> GetUserInfo(CancellationToken cancellationToken) => _platformUserModel.GetUserInfo(cancellationToken);

        public async Task<string> GetXPlatformAccessToken(CancellationToken cancellationToken) => (await _platformUserModel.RequestXPlatformAccessToken(cancellationToken)).token;
#endif

        public async Task<string> GetAuthToken() => (await _platformUserModel.GetUserAuthToken()).token;

        public Task<IReadOnlyList<string>> GetFriendsUserIds() => _platformUserModel.GetUserFriendsUserIds(false);
    }
#endif
}
