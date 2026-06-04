namespace ScoreSaber.Features.Players.Domain {
    internal class LocalPlayerPanelState {
        internal string GlobalRankingText { get; private set; }
        internal bool IsLoaded { get; private set; }
        internal bool HasPlayerProfile { get; private set; }
        internal bool UsesWilliumsPanel { get; private set; }
        internal bool UsesDenyahPanel { get; private set; }
        internal string PromptErrorText { get; private set; }
        internal float PromptDismissTime { get; private set; }

        private LocalPlayerPanelState() {
            GlobalRankingText = "<b><color=#FFDE1A>Global Ranking: </color></b> Loading...";
            IsLoaded = true;
            PromptErrorText = string.Empty;
            PromptDismissTime = -1f;
        }

        internal static LocalPlayerPanelState Initial() {
            return new LocalPlayerPanelState();
        }

        internal static LocalPlayerPanelState Loading(LocalPlayerPanelState previous) {
            return new LocalPlayerPanelState {
                GlobalRankingText = previous.GlobalRankingText,
                IsLoaded = false,
                HasPlayerProfile = previous.HasPlayerProfile,
                UsesWilliumsPanel = previous.UsesWilliumsPanel,
                UsesDenyahPanel = previous.UsesDenyahPanel
            };
        }

        internal static LocalPlayerPanelState Player(PlayerProfile player, bool usesWilliumsPanel, bool usesDenyahPanel, string globalRankingText) {
            return new LocalPlayerPanelState {
                GlobalRankingText = globalRankingText,
                IsLoaded = true,
                HasPlayerProfile = player != null,
                UsesWilliumsPanel = usesWilliumsPanel,
                UsesDenyahPanel = usesDenyahPanel
            };
        }

        internal static LocalPlayerPanelState Message(string text) {
            return new LocalPlayerPanelState {
                GlobalRankingText = text,
                IsLoaded = true
            };
        }

        internal static LocalPlayerPanelState Unavailable() {
            return Message("<b><color=#FFDE1A>Global Ranking: </color></b>Unavailable");
        }

        internal static LocalPlayerPanelState PromptError(LocalPlayerPanelState previous, string promptText, float dismissTime) {
            return new LocalPlayerPanelState {
                GlobalRankingText = string.Empty,
                IsLoaded = true,
                HasPlayerProfile = previous.HasPlayerProfile,
                UsesWilliumsPanel = previous.UsesWilliumsPanel,
                UsesDenyahPanel = previous.UsesDenyahPanel,
                PromptErrorText = promptText,
                PromptDismissTime = dismissTime
            };
        }
    }
}
