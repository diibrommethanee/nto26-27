using System;
using _Project.Develop.Gameplay.Abilities.Hearing;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Player.Abilities;
using MessagePipe;
using TMPEffects.Components;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.Develop.UI.Hearing
{
    public class HearingView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private HearingSource[] sources;
        [SerializeField] private PlayerAbility player;
        [SerializeField] private HearingWave wave;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Vector2 offsetAbovePlayer = new(0f, 1.75f);

        private IDisposable _subscription;
        private EvidenceService _evidence;
        private InputController _input;
        private TMPWriter _writer;
        private Canvas _canvas;
        private HearingSource _source;
        private float _frequency;
        private float _held;
        private bool _decoded;

        [Inject]
        private void Construct(IBufferedSubscriber<HearingChanged> hearing, EvidenceService evidence, InputController input)
        {
            _evidence = evidence;
            _input = input;
            _writer = status.GetComponent<TMPWriter>();
            _canvas = GetComponent<Canvas>();
            wave.raycastTarget = false;
            _subscription = hearing.Subscribe(OnHearingChanged);
        }

        private void OnHearingChanged(HearingChanged message)
        {
            _source = null;
            _held = 0;
            _decoded = false;
            if (!message.IsActive)
            {
                panel.SetActive(false);
                _writer.enabled = false;
                return;
            }

            Vector3 playerPosition = player.transform.position;

            float nearest = float.PositiveInfinity;
            foreach (var source in sources)
            {
                float distance = Vector2.Distance(playerPosition, source.transform.position);
                if (!source.isActiveAndEnabled || distance > source.radius || distance >= nearest) continue;
                _source = source;
                nearest = distance;
            }

            transform.position = _source != null
                ? _source.transform.position
                : playerPosition + (Vector3)offsetAbovePlayer;
            wave.gameObject.SetActive(_source != null);
            _canvas.enabled = !_input.IsZoomMode;
            panel.SetActive(true);
            if (_source == null)
            {
                status.text = "Поблизости ничего не слышно";
                return;
            }

            wave.ResetSignal();
            wave.AimAtSource(_source.transform.position - playerPosition);
            _decoded = _evidence.HasClue(_source.clue);
            Tune(_decoded ? _source.frequency : 0f);
            if (_decoded) ShowTranscript();
        }

        private void Tune(float value)
        {
            _frequency = value;
            float difference = _source.frequency - value;
            float distance = Mathf.Abs(difference);
            wave.Tune(distance, _source.tolerance);
            if (distance <= _source.tolerance)
            {
                status.text = string.Empty;
                return;
            }

            _held = 0;
            wave.SetProgress(0f);
            status.text = difference > 0 ? "Крути колёсико вверх" : "Крути колёсико вниз";
        }

        private void Update()
        {
            if (player.ActiveSkill != SkillType.Hearing) return;

            bool zooming = _input.IsZoomMode;
            _canvas.enabled = !zooming;
            if (_source == null) return;
            if (Vector2.Distance(player.transform.position, _source.transform.position) > _source.radius)
            {
                player.StopHearing();
                return;
            }

            if (zooming || _decoded) return;

            float scroll = _input.Actions.UI.ScrollWheel.ReadValue<Vector2>().y;
            if (scroll != 0f)
            {
                float step = Mathf.Min(0.05f, _source.tolerance * 2f);
                Tune(Mathf.Clamp01(_frequency + Mathf.Sign(scroll) * step));
            }

            if (Mathf.Abs(_frequency - _source.frequency) > _source.tolerance) return;
            _held += Time.unscaledDeltaTime;
            wave.SetProgress(_held / _source.holdSeconds);
            if (_held < _source.holdSeconds) return;
            _decoded = true;
            _evidence.Collect(_source.clue, _source.transform.position);
            wave.Flash();
            ShowTranscript();
        }

        private void ShowTranscript()
        {
            wave.SetProgress(1f);
            _writer.enabled = true;
            _writer.SetText(_source.transcript);
            _writer.StartWriter();
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }
    }
}
