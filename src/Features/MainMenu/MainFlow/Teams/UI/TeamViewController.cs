using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using Newtonsoft.Json;
using ScoreSaber.Core.Presentation;
using ScoreSaber.Features.MainMenu.MainFlow.Teams;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zenject;

namespace ScoreSaber.Features.MainMenu.MainFlow.Teams.UI {
    [HotReload]
    internal class TeamViewController : BSMLAutomaticViewController {
        private const string TeamUrl = "https://raw.githubusercontent.com/Umbranoxio/ScoreSaber-Team/main/team.json";

        [UIComponent("tab-selector")]
        protected readonly TabSelector _tabSelector = null;

        [UIValue("team-hosts")]
        protected readonly List<object> _teamHosts = new List<object>();

        private Http _http = null;
        private ScoreSaberUIMaterials _materials = null;

        [Inject]
        internal void Construct(Http http, ScoreSaberUIMaterials materials) {
            _http = http;
            _materials = materials;
        }

        [UIAction("#post-parse")]
        protected void Parsed() {

            _tabSelector.transform.localScale *= 0.75f;
        }

        protected override async void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling) {

            if (firstActivation) {

                _teamHosts.Clear();
                var team = await GetTeam();

                foreach (KeyValuePair<TeamType, List<TeamMember>> member in team.TeamMembers) {
                    string teamName = member.Key.ToString();
                    if (teamName == "RT") {
                        teamName = "Ranking Team";
                    }
                    TeamHost host = TeamToProfileHost(member.Value, teamName);
                    _teamHosts.Add(host);
                }
            }

            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            _tabSelector.GetTextSegmentedControl().didSelectCellEvent += DidSelect;
            if (_teamHosts.Count > 0) {
                TeamHost host = (TeamHost)_teamHosts[0];
                host.Init();
                foreach (TeamUserInfo profile in host.profiles) {
                    profile.LoadImage();
                }
            }
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling) {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            _tabSelector.GetTextSegmentedControl().didSelectCellEvent -= DidSelect;
        }

        private void DidSelect(SegmentedControl segmentedControl, int pos) {

            var teamHost = _teamHosts[pos] as TeamHost;
            teamHost.Init();
            foreach (TeamUserInfo profile in teamHost.profiles) {
                profile.LoadImage();
            }
        }

        private TeamHost TeamToProfileHost(List<TeamMember> team, string teamName) {

            List<TeamUserInfo> host = new List<TeamUserInfo>();
            foreach (TeamMember member in team) {
                host.Add(new TeamUserInfo(_materials, member.ProfilePicture, member.Name, member.Discord, member.GitHub, member.Twitch, member.Twitter, member.YouTube));
            }
            return new TeamHost(teamName, host);
        }

        private async Task<ScoreSaberTeam> GetTeam() {
            string response = await _http.GetRawAsync(TeamUrl);
            return JsonConvert.DeserializeObject<ScoreSaberTeam>(response);
        }
    }
}
