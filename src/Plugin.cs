using BeatSaberMarkupLanguage;
using HarmonyLib;
using IPA;
using IPA.Loader;
using ScoreSaber.Core;
using ScoreSaber.Core.Api;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Core.Platform;
using ScoreSaber.Features.Replays;
using ScoreSaber.Features.Replays.Installers;
using ScoreSaber.Features.ScoreSubmission.Services;
using ScoreSaber.Features.Players.Profile;
using SiraUtil.Web;
using SiraUtil.Zenject;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;

namespace ScoreSaber {
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin {

        internal static IPALogger Log { get; private set; }
        internal static Plugin Instance { get; private set; }

        internal static SettingsService SettingsService { get; private set; }

        internal Http HttpInstance { get; private set; }
        internal ReplayState ReplayState { get; private set; }

        internal System.Version LibVersion;
        internal Harmony harmony;
        internal PluginMetadata Metadata;

        [Init]
        public Plugin(IPALogger logger, PluginMetadata metadata, Zenjector zenjector) {

            Log = logger;
            Instance = this;
            Metadata = metadata;
            SettingsService = new SettingsService();
            ReplayState = new ReplayState();
            ReplayStateRegistry.Use(ReplayState);

            zenjector.UseLogger(logger);
            zenjector.Expose<ComboUIController>("Environment");
            zenjector.Expose<GameEnergyUIPanel>("Environment");
            zenjector.Install<AppInstaller>(Location.App);
            zenjector.Install<MainInstaller>(Location.Menu);
            zenjector.Install<ImberInstaller>(Location.StandardPlayer);
            zenjector.Install<PlaybackInstaller>(Location.StandardPlayer);
            zenjector.Install<RecordInstaller, StandardGameplayInstaller>();
            zenjector.Install<RecordInstaller, MultiplayerLocalActivePlayerInstaller>();
            zenjector.UseHttpService(HttpServiceType.UnityWebRequests);
            zenjector.UseAutoBinder();

            LibVersion = Assembly.GetExecutingAssembly().GetName().Version;
            HttpInstance = new Http(new HttpOptions() { baseURL = ScoreSaberUrls.ApiBaseUrl, applicationName = "ScoreSaber-PC", version = LibVersion });
            OpenXRManager.Initialize();
            SteamSettings.Initialize();
        }

        [OnEnable]
        public void OnEnable() {
            BeatSaberMarkupLanguage.Util.MainMenuAwaiter.MainMenuInitializing += MainMenuInit;
            SettingsService.Load();
            ReplayState.Reset();
            if (!SettingsService.Current.disableScoreSaber) {
                harmony = new Harmony("com.umbranox.BeatSaber.ScoreSaber");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                PlayerPrefs.SetInt("lbPatched", 1);
            }
        }

        private void MainMenuInit() {
            BSMLParser.Instance.RegisterTypeHandler(new ProfileDetailViewTypeHandler());
            BSMLParser.Instance.RegisterTag(new ProfileDetailViewTag(Metadata.Assembly));
        }

        public static bool ScoreSubmission {
            get => ScoreSubmissionRegistry.IsEnabled;
            set {
                if (!IsScoreSubmissionCompatibilityCaller()) {
                    return;
                }

                if (ReplayStateRegistry.IsPlaybackEnabled) {
                    return;
                }

                ScoreSubmissionRegistry.SetEnabled(value);
            }
        }

        private static bool IsScoreSubmissionCompatibilityCaller() {
            var frames = new StackTrace().GetFrames();
            if (frames == null) {
                return false;
            }

            foreach (StackFrame frame in frames) {
                MethodBase method = frame.GetMethod();
                string typeName = method?.DeclaringType?.FullName;
                if (!string.IsNullOrEmpty(typeName) && (typeName.Contains("BS_Utils") || typeName.Contains("SiraUtil"))) {
                    return true;
                }
            }

            return false;
        }
    }
}
