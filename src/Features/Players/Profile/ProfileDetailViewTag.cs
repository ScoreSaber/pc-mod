using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Tags;
using ScoreSaber.Core.Presentation;
using System.Reflection;
using UnityEngine;

namespace ScoreSaber.Features.Players.Profile {
    internal class ProfileDetailViewTag : BSMLTag {
        private const string ProfileResource = "ScoreSaber.Features.Players.Profile.ProfileDetailView.bsml";
        private const string ProfileLayout = "ProfileDetailView.bsml";
        private readonly Assembly _assembly;

        public override string[] Aliases => new[] { "ss-profile" };

        public ProfileDetailViewTag(Assembly asm) {
            _assembly = asm;
        }

        public override GameObject CreateObject(Transform parent) {
            GameObject gameObj = new GameObject("ScoreSaberProfileModal");
            gameObj.transform.SetParent(parent, false);

            ProfileDetailView host = gameObj.AddComponent<ProfileDetailView>();
            Parse(gameObj, host);
            host.UseHotReload(BSMLHotReload.Watch(ProfileLayout, () => Reload(gameObj, host)));
            return gameObj;
        }

        private void Reload(GameObject gameObj, ProfileDetailView host) {
            BSMLHotReload.ClearChildren(gameObj.transform);
            Parse(gameObj, host);
        }

        private void Parse(GameObject gameObj, ProfileDetailView host) {
            BsmlParser.Instance.Parse(BSMLHotReload.ResourceContent(_assembly, ProfileResource, ProfileLayout), gameObj, host);
            host.SetProfileBadges(null);
        }
    }
}
