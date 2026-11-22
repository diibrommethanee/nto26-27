using System;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.Hearing;
using _Project.Develop.Gameplay.Player.Abilities;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.Develop.UI.Hearing
{
    public class HearingView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private HearingSource[] sources;
        [SerializeField] private PlayerAbility player;
        [SerializeField] private HearingWave wave;
        [SerializeField] private Slider dial;
        [SerializeField] private Image progress;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Button close;

        private IDisposable _subscription;
        private EvidenceService _evidence;
        private HearingSource _source;
        private float _held;
        private bool _decoded;

        [Inject]
        private void Construct(IBufferedSubscriber<HearingChanged> hearing, EvidenceService evidence)
        {
            _evidence = evidence;
            close.onClick.AddListener(player.StopHearing);
            dial.onValueChanged.AddListener(Tune);
            _subscription = hearing.Subscribe(OnHearingChanged);
        }

        private void OnHearingChanged(HearingChanged message)
        {
            _source = null;
            _held = 0;
            _decoded = false;
            panel.SetActive(message.IsActive);
            if (!message.IsActive) return;
            float nearest = float.PositiveInfinity;
            foreach (var source in sources)
            {
                float distance = Vector2.Distance(player.transform.position, source.transform.position);
                if (!source.isActiveAndEnabled || distance > source.radius || distance >= nearest) continue;
                _source = source;
                nearest = distance;
            }
            progress.fillAmount = 0;
            wave.enabled = dial.interactable = _source != null;
            if (_source == null)
            {
                status.text = "Поблизости ничего не слышно";
                return;
            }
            _decoded = _evidence.HasClue(_source.clue);
            dial.SetValueWithoutNotify(_decoded ? _source.frequency : 0);
            Tune(dial.value);
            if (_decoded) ShowTranscript();
        }

        private void Tune(float value)
        {
            wave.Tune(value, _source.frequency);
            float difference = _source.frequency - value;
            if (Mathf.Abs(difference) <= _source.tolerance)
            {
                status.text = "Частота поймана · удерживайте настройку";
                return;
            }
            _held = 0;
            progress.fillAmount = 0;
            status.text = difference > 0 ? "Повысьте частоту →" : "← Понизьте частоту";
        }

        private void Update()
        {
            if (_source == null || _decoded || Mathf.Abs(dial.value - _source.frequency) > _source.tolerance) return;
            _held += Time.unscaledDeltaTime;
            progress.fillAmount = _held / _source.holdSeconds;
            if (_held < _source.holdSeconds) return;
            _decoded = true;
            _evidence.Collect(_source.clue, _source.transform.position);
            ShowTranscript();
        }

        private void ShowTranscript()
        {
            dial.interactable = false;
            progress.fillAmount = 1;
            status.text = _source.transcript;
        }

        private void OnDestroy() => _subscription?.Dispose();
    }
}
