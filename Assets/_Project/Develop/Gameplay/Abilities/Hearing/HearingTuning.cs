using UnityEngine;

namespace _Project.Develop.Gameplay.Abilities.Hearing
{
    public class HearingTuning
    {
        private readonly HearingSource _source;
        private float _frequency;
        private float _heldSeconds;

        public HearingTuning(HearingSource source) => _source = source;

        public float FrequencyDifference => _source.frequency - _frequency;
        public bool IsInTune => Mathf.Abs(FrequencyDifference) <= _source.tolerance;

        public void Adjust(float scroll)
        {
            if (scroll == 0f) return;

            var step = Mathf.Min(0.05f, _source.tolerance * 2f);
            _frequency = Mathf.Clamp01(_frequency + Mathf.Sign(scroll) * step);
            if (!IsInTune)
                _heldSeconds = 0f;
        }

        public float AdvanceHold(float deltaSeconds)
        {
            if (IsInTune)
                _heldSeconds += deltaSeconds;

            return _heldSeconds / _source.holdSeconds;
        }

        public bool IsDecoded => _heldSeconds >= _source.holdSeconds;
    }
}
