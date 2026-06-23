using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace ScoreSaber.Core.Configuration {
    internal class Settings {
        public bool hideReplayUI = false;

        public int fileVersion { get; set; }
        public bool disableScoreSaber { get; set; }
        public bool showLocalPlayerRank { get; set; }
        public bool showScorePP { get; set; }
        public bool showStatusText { get; set; }
        public bool saveLocalReplays { get; set; }
        public bool enableCountryLeaderboards { get; set; }
        public string locationFilterMode { get; set; }
        public bool replayCameraSmoothing { get; set; }
        public bool replayOverrideHandedness { get; set; }
        public bool publicLivePresenceOptOut { get; set; } = false;
        public bool liveChatOverlayEnabled { get; set; } = true;
        public bool liveChatOverlayGameplayEnabled { get; set; } = true;
        public float liveChatOverlayScale { get; set; } = 1.15f;
        public float liveChatOverlayTextScale { get; set; } = 1.25f;
        public float replayCameraFOV { get; set; }
        public float replayCameraXOffset { get; set; }
        public float replayCameraYOffset { get; set; }
        public float replayCameraZOffset { get; set; }
        public float replayCameraXRotation { get; set; }
        public float replayCameraYRotation { get; set; }
        public float replayCameraZRotation { get; set; }
        public bool enableReplayFrameRenderer { get; set; }
        public string replayFramePath { get; set; }
        public bool hideNAScoresFromLeaderboard { get; set; }
        public bool hasClickedScoreSaberLogo { get; set; }
        public bool hasOpenedReplayUI { get; set; }
        public bool leftHandedReplayUI { get; set; }
        public bool lockedReplayUIMode { get; set; }
        public List<SpectatorPoseRoot> spectatorPositions { get; set; }

        public void SetDefaults() {

            disableScoreSaber = false;
            showLocalPlayerRank = true;
            showScorePP = true;
            showStatusText = true;
            saveLocalReplays = true;
            enableCountryLeaderboards = true;
            locationFilterMode = "Country";
            replayCameraSmoothing = true;
            replayOverrideHandedness = false;
            publicLivePresenceOptOut = false;
            liveChatOverlayEnabled = true;
            liveChatOverlayGameplayEnabled = true;
            liveChatOverlayScale = 1.15f;
            liveChatOverlayTextScale = 1.25f;
            replayCameraFOV = 70f;
            replayCameraXOffset = 0.0f;
            replayCameraYOffset = 0.0f;
            replayCameraZOffset = 0.0f;
            replayCameraXRotation = 0.0f;
            replayCameraYRotation = 0.0f;
            replayCameraZRotation = 0.0f;
            enableReplayFrameRenderer = false;
            replayFramePath = "Z:\\Example\\Directory\\";
            hideNAScoresFromLeaderboard = false;
            hasClickedScoreSaberLogo = false;
            hasOpenedReplayUI = false;
            leftHandedReplayUI = false;
            lockedReplayUIMode = false;
            SetDefaultSpectatorPositions();
        }

        public void SetDefaultSpectatorPositions() {

            spectatorPositions = new List<SpectatorPoseRoot> {
                new SpectatorPoseRoot(new SpectatorPose(new Vector3(0f, 0f, -2f)), "Main"),
                new SpectatorPoseRoot(new SpectatorPose(new Vector3(0f, 4f, 0f)), "Bird's Eye"),
                new SpectatorPoseRoot(new SpectatorPose(new Vector3(-3f, 0f, 0f)), "Left"),
                new SpectatorPoseRoot(new SpectatorPose(new Vector3(3f, 0f, 0f)), "Right")
            };
        }

        internal struct SpectatorPoseRoot {
            [JsonProperty("name")]
            internal string name { get; set; }
            [JsonProperty("spectatorPose")]
            internal SpectatorPose spectatorPose { get; set; }

            internal SpectatorPoseRoot(SpectatorPose spectatorPose, string name) {
                this.name = name;
                this.spectatorPose = spectatorPose;
            }
        }

        internal struct SpectatorPose {
            [JsonProperty("x")]
            internal float x { get; set; }
            [JsonProperty("y")]
            internal float y { get; set; }
            [JsonProperty("z")]
            internal float z { get; set; }

            internal SpectatorPose(Vector3 position) {
                x = position.x;
                y = position.y;
                z = position.z;
            }

            internal Vector3 ToVector3() {
                return new Vector3(x, y, z);
            }
        }
    }
}
