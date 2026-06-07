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

            _comboController._combo = combo;
            _comboController._maxCombo = cutOrMissRecorded;
            FieldAccessor<ComboController, Action<int>>.Get(ref _comboController, "comboDidChangeEvent").Invoke(combo);

            bool didLoseCombo = ReplayTimeSearch.CountBefore(_comboLossTimes, time) > 0;
            var animator = _comboUIController._animator;
            int comboLostId = _comboUIController._comboLostId;
            if ((combo == 0 && cutOrMissRecorded == 0) || !didLoseCombo) {
                animator.Rebind();
                _comboUIController._fullComboLost = false;
            } else {
                animator.ResetTrigger(comboLostId);
                animator.Play(comboLostId, 0, 1f);
                animator.Update(0f);
                _comboUIController._fullComboLost = true;
            }
        }
    }
}
