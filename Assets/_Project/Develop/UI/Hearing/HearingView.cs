using System;
using _Project.Develop.Gameplay.Abilities.Hearing;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Player.Abilities;
using MessagePipe;
using PrimeTween;
using TMPEffects.Components;
using TMPro;
using UnityEngine;
using VContainer;

namespace _Project.Develop.UI.Hearing
{
    public class HearingView : MonoBehaviour
    {
        private const float NoSignalRadius = 2f;
        private const float EnterSeconds = 0.30f;
        private const float ExitSeconds = 0.24f;
        private const float TranscriptReadSeconds = 1.2f;
        private const float FadeSeconds = 0.75f;

        [SerializeField] private GameObject panel;
        [SerializeField] private HearingSource[] sources;
        [SerializeField] private PlayerAbility player;
        [SerializeField] private HearingWave wave;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Vector2 offsetAbovePlayer = new(0f, 3.65f);

        private IDisposable _subscription;
        private EvidenceService _evidence;
        private InputController _input;
        private TMPWriter _writer;
        private Canvas _canvas;
        private CanvasGroup _visibility;
        private Tween _visibilityTween;
        private float _statusAlpha;
        private HearingSource _source;
        private Vector3? _lastNoSignalOrigin;
        private Vector3 _lastNoSignalPosition;
        private float _frequency;
        private float _held;
        private bool _decoded;
        private float _caughtElapsed = -1f;

        [Inject]
        private void Construct(IBufferedSubscriber<HearingChanged> hearing, EvidenceService evidence, InputController input)
        {
            _evidence = evidence;
            _input = input;
            _writer = status.GetComponent<TMPWriter>();
            _writer.OnFinishWriter.AddListener(OnTranscriptFinished);
            _canvas = GetComponent<Canvas>();
            if (!panel.TryGetComponent(out _visibility))
                _visibility = panel.AddComponent<CanvasGroup>();
            _visibility.alpha = 0f;
            _statusAlpha = status.alpha;
            wave.raycastTarget = false;
            _subscription = hearing.Subscribe(OnHearingChanged);
        }

        private void OnHearingChanged(HearingChanged message)
        {
            _visibilityTween.Stop();
            _source = null;
            _held = 0;
            _decoded = false;
            _caughtElapsed = -1f;
            if (!message.IsActive)
            {
                if (panel.activeSelf && _visibility.alpha > 0f)
                {
                    if (_writer.isActiveAndEnabled)
                        _writer.StopWriter();

                    _visibilityTween = Tween.Alpha(_visibility, 0f, ExitSeconds,
                        Ease.InOutSine, useUnscaledTime: true);
                    _visibilityTween.OnComplete(this, view => view.HidePanel());
                }
                else
                {
                    HidePanel();
                }

                return;
            }

            _writer.enabled = false;
            Vector3 playerPosition = player.transform.position;

            float nearest = float.PositiveInfinity;
            foreach (var source in sources)
            {
                float distance = Vector2.Distance(playerPosition, source.transform.position);
                if (!source.isActiveAndEnabled || distance > source.radius || distance >= nearest
                    || _evidence.HasClue(source.clue)) continue;
                _source = source;
                nearest = distance;
            }

            bool repeatNoSignal = false;
            transform.position = _source != null
                ? _source.transform.position
                : GetNoSignalPosition(playerPosition, out repeatNoSignal);
            wave.gameObject.SetActive(_source != null);
            _canvas.enabled = !_input.IsZoomMode;
            status.text = string.Empty;
            status.alpha = _statusAlpha;
            panel.SetActive(true);
            if (_source == null)
            {
                const string messageText = "Поблизости ничего не слышно";
                if (repeatNoSignal)
                {
                    status.text = messageText;
                    FadeIn();
                }
                else
                {
                    _visibility.alpha = 1f;
                    StartWriting(messageText);
                }

                return;
            }

            FadeIn();
            wave.ResetSignal();
            wave.FacePlayer(playerPosition - _source.transform.position);
            Tune(0f);
        }

        private void FadeIn()
        {
            _visibilityTween = Tween.Alpha(_visibility, 1f, EnterSeconds,
                Ease.InOutSine, useUnscaledTime: true);
        }

        private Vector3 GetNoSignalPosition(Vector3 playerPosition, out bool repeat)
        {
            repeat = _lastNoSignalOrigin is Vector3 origin
                && ((Vector2)(playerPosition - origin)).sqrMagnitude < 0.0025f;
            if (repeat)
                return _lastNoSignalPosition;

            _lastNoSignalOrigin = playerPosition;
            _lastNoSignalPosition = playerPosition + (Vector3)offsetAbovePlayer
                + new Vector3(UnityEngine.Random.Range(-0.9f, 0.9f), UnityEngine.Random.Range(0f, 0.35f), 0f);
            return _lastNoSignalPosition;
        }

        private void Tune(float value)
        {
            _frequency = value;
            float difference = _source.frequency - value;
            float distance = Mathf.Abs(difference);
            wave.Tune(distance, _source.tolerance);
            if (distance <= _source.tolerance) return;

            _held = 0;
            status.alpha = _statusAlpha;
            wave.SetProgress(0f);
            status.text = difference > 0 ? "Крути колёсико вверх" : "Крути колёсико вниз";
        }

        private void Update()
        {
            if (player.ActiveSkill != SkillType.Hearing) return;

            bool zooming = _input.IsZoomMode;
            _canvas.enabled = !zooming;
            if (_source == null)
            {
                Vector2 displacement = player.transform.position - _lastNoSignalOrigin.Value;
                if (displacement.sqrMagnitude > NoSignalRadius * NoSignalRadius)
                    player.StopHearing();
                return;
            }
            Vector2 directionToPlayer = player.transform.position - _source.transform.position;
            if (directionToPlayer.sqrMagnitude > _source.radius * _source.radius)
            {
                player.StopHearing();
                return;
            }

            wave.FacePlayer(directionToPlayer);
            if (zooming) return;
            if (_caughtElapsed >= 0f)
            {
                UpdateCaught();
                return;
            }

            if (_decoded) return;

            float scroll = _input.Actions.UI.ScrollWheel.ReadValue<Vector2>().y;
            if (scroll != 0f)
            {
                float step = Mathf.Min(0.05f, _source.tolerance * 2f);
                Tune(Mathf.Clamp01(_frequency + Mathf.Sign(scroll) * step));
            }

            if (Mathf.Abs(_frequency - _source.frequency) > _source.tolerance) return;
            _held += Time.unscaledDeltaTime;
            float progress = _held / _source.holdSeconds;
            wave.SetProgress(progress);
            status.alpha = _statusAlpha * (1f - Mathf.SmoothStep(0f, 1f, progress));
            if (_held < _source.holdSeconds) return;
            _decoded = true;
            wave.Flash();
            ShowTranscript();
        }

        private void ShowTranscript()
        {
            wave.SetProgress(1f);
            StartWriting(_source.transcript);
        }

        private void StartWriting(string text)
        {
            _writer.enabled = true;
            _writer.SetText(text);
            status.alpha = _statusAlpha;
            _writer.StartWriter();
        }

        private void OnTranscriptFinished(TMPWriter writer)
        {
            if (!_decoded || _source == null) return;

            _caughtElapsed = 0f;
            _evidence.Collect(_source.clue, _source.transform.position);
        }

        private void UpdateCaught()
        {
            _caughtElapsed += Time.unscaledDeltaTime;
            if (_caughtElapsed < TranscriptReadSeconds) return;

            float fade = (_caughtElapsed - TranscriptReadSeconds) / FadeSeconds;
            _visibility.alpha = 1f - Mathf.SmoothStep(0f, 1f, fade);
            if (fade >= 1f)
                player.StopHearing();
        }

        private void HidePanel()
        {
            _visibility.alpha = 0f;
            panel.SetActive(false);
            _writer.enabled = false;
        }

        private void OnDestroy()
        {
            _visibilityTween.Stop();
            _writer?.OnFinishWriter.RemoveListener(OnTranscriptFinished);
            _subscription?.Dispose();
        }
    }
}
