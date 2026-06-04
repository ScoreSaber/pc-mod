using IPA.Utilities;
using ScoreSaber.Features.Replays.Format;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScoreSaber.Features.Replays.Playback {
    internal class ComboPlayer : TimeSynchronizer, IScroller {
        private ComboController _comboController;
        private ComboUIController _comboUIController;
        private readonly List<ComboEvent> _comboEvents;
        private readonly float[] _scoringNoteEventTimes;
        private readonly float[] _comboLossTimes;

        public ComboPlayer(ReplayFile file, ComboController comboController, ComboUIController comboUIController) {

            _comboController = comboController;
            _comboUIController = comboUIController;
            _comboEvents = file.comboKeyframes;
            _scoringNoteEventTimes = file.noteKeyframes
                .Where(ReplayTimeSearch.IsScoringNoteEvent)
                .Select(ne => ne.Time)
                .OrderBy(time => time)
                .ToArray();
            _comboLossTimes = file.comboKeyframes
                .Where(ce => ce.Combo == 0)
                .Select(ce => ce.Time)
                .OrderBy(time => time)
                .ToArray();
        }

        public void TimeUpdate(float newTime) {

            int nextIndex = ReplayTimeSearch.CountAtOrBefore(_comboEvents, newTime, comboEvent => comboEvent.Time);
            int combo = nextIndex > 0 ? _comboEvents[nextIndex - 1].Combo : 0;
            UpdateCombo(newTime, combo);
        }

        private void UpdateCombo(float time, int combo) {

            int cutOrMissRecorded = ReplayTimeSearch.CountBefore(_scoringNoteEventTimes, time);

            Accessors.Combo(ref _comboController) = combo;
            Accessors.MaxCombo(ref _comboController) = cutOrMissRecorded;
            FieldAccessor<ComboController, Action<int>>.Get(ref _comboController, "comboDidChangeEvent").Invoke(combo);

            bool didLoseCombo = ReplayTimeSearch.CountBefore(_comboLossTimes, time) > 0;
            if ((combo == 0 && cutOrMissRecorded == 0) || !didLoseCombo) {
                Accessors.ComboAnimator(ref _comboUIController).Rebind();
                Accessors.ComboWasLost(ref _comboUIController) = false;
            } else {
                Accessors.ComboAnimator(ref _comboUIController).SetTrigger(Accessors.TriggerID(ref _comboUIController));
                Accessors.ComboWasLost(ref _comboUIController) = true;
            }
        }
    }
}
