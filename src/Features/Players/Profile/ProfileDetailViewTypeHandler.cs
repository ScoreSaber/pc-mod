using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.TypeHandlers;
using ScoreSaber.Core;
using System;
using System.Collections.Generic;

namespace ScoreSaber.Features.Players.Profile {
    [ComponentHandler(typeof(ProfileDetailView))]
    internal class ProfileDetailViewTypeHandler : TypeHandler<ProfileDetailView> {
        public override Dictionary<string, string[]> Props => new Dictionary<string, string[]>()
        {
            { "name", new [] { "name" } },
            { "profileImageSource", new [] { "src" } },
            { "performancePoints", new [] { "pp" } },
            { "rankedAccuracy", new [] { "acc" } },
            { "totalScore", new [] { "score" } },
            { "loading", new [] { "loading" } },
            { "rank", new [] { "rank" } },

            { "showEvent", new [] { "show-event" } },
            { "hideEvent", new [] { "hide-event" } },
        };

        public override Dictionary<string, Action<ProfileDetailView, string>> Setters => new Dictionary<string, Action<ProfileDetailView, string>>()
        {
            { "name", new Action<ProfileDetailView, string>((profile, value) => profile.playerNameText.text = value) },
            { "profileImageSource", new Action<ProfileDetailView, string>((profile, value) => profile.profilePicture.SetImageAsync(value).RunTask()) },
            { "rankedAccuracy", new Action<ProfileDetailView, string>((profile, value) => profile.rankedAccText.text = value) },
            { "performancePoints", new Action<ProfileDetailView, string>((profile, value) => profile.ppText.text  = value) },
            { "totalScore", new Action<ProfileDetailView, string>((profile, value) => profile.totalScoreText.text = value) },
            { "loading", new Action<ProfileDetailView, string>((profile, value) => profile.SetLoadingState(bool.Parse(value))) },
            { "rank", new Action<ProfileDetailView, string>((profile, value) => profile.rankText.text = value) }
        };

        public override void HandleTypeAfterParse(BSMLParser.ComponentTypeWithData componentType, BSMLParserParams parserParams) {
            base.HandleTypeAfterParse(componentType, parserParams);
            try {
                var profile = componentType.Component as ProfileDetailView;
                var parent = profile.profileModalRoot.transform.parent;
                void Reparent() { profile.profileModalRoot.transform.SetParent(parent, true); }

                if (componentType.Data.TryGetValue("showEvent", out string showEvent)) {
                    parserParams.AddEvent(showEvent, delegate {
                        profile.profileModalRoot.Show(true, true); // by Design™
                    });
                }
                if (componentType.Data.TryGetValue("hideEvent", out string hideEvent)) {
                    parserParams.AddEvent(hideEvent, delegate {
                        profile.profileModalRoot.Hide(true, Reparent);
                    });
                }
            } catch (Exception ex) {
                Plugin.Log.Error(ex);
            }
        }
    }
}
