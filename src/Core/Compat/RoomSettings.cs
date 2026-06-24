using UnityEngine;
#if BEAT_SABER_1_37_1
using BeatSaber.GameSettings;
#endif

namespace ScoreSaber.Core.Compat {
    internal class RoomSettings {
#if BEAT_SABER_1_29_0
        private MainSettingsModelSO _mainSettingsModel;

        private MainSettingsModelSO MainSettingsModel => _mainSettingsModel != null ? _mainSettingsModel : _mainSettingsModel = Resources.FindObjectsOfTypeAll<MainSettingsModelSO>()[0];

        internal Vector3 Center => MainSettingsModel.roomCenter.value;
        internal float Rotation => MainSettingsModel.roomRotation.value;
#elif BEAT_SABER_1_37_1
        private readonly MainSettingsHandler _mainSettingsHandler;

        public RoomSettings(MainSettingsHandler mainSettingsHandler) {
            _mainSettingsHandler = mainSettingsHandler;
        }

        internal Vector3 Center => _mainSettingsHandler.instance.roomCenter;
        internal float Rotation => _mainSettingsHandler.instance.roomRotation;
#else
        private readonly SettingsManager _settingsManager;

        public RoomSettings(SettingsManager settingsManager) {
            _settingsManager = settingsManager;
        }

        internal Vector3 Center => _settingsManager.settings.room.center;
        internal float Rotation => _settingsManager.settings.room.rotation;
#endif
    }
}
