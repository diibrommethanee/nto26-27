using System;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Tea;
using MessagePipe;
using TMPEffects.Components;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace _Project.Develop.Gameplay.Timeline
{
    public readonly struct DayEndingCutsceneFinished { }

    [RequireComponent(typeof(PlayableDirector))]
    public class DayEndingCutscene : MonoBehaviour
    {
        [SerializeField] private GameObject teaStage;
        [SerializeField] private GameObject notebookGroup;
        [SerializeField] private GameObject teaCeremonyGroup;
        [SerializeField] private TeaBrewingView teaMiniGame;
        [SerializeField] private Canvas hud;
        [SerializeField] private TMPWriter notebookWriter;
        [SerializeField] private Button newDayButton;

        private InputController _input;
        private NextDayBuffService _buffService;
        private IPublisher<DayEndingCutsceneFinished> _finished;
        private IDisposable _subscription;
        private PlayableDirector _director;
        private bool _playing;
        private bool _hudWasEnabled;
        private bool _ending;
        private bool _teaCompleted;
        private bool _waitingToStartTea;

        private void Awake()
        {
            _director = GetComponent<PlayableDirector>();
            _director.playOnAwake = false;
            _director.Stop();

            newDayButton.gameObject.SetActive(false);
            newDayButton.onClick.AddListener(OnNewDayButtonClicked);
            if (notebookWriter != null)
                notebookWriter.OnFinishWriter.AddListener(ShowNewDayButton);

            if (teaMiniGame != null)
                teaMiniGame.Completed += OnTeaCompleted;
        }

        [Inject]
        private void Construct(IBufferedSubscriber<DayPhaseChanged> phases, InputController input,
            IPublisher<DayEndingCutsceneFinished> finished, NextDayBuffService buffService)
        {
            _input = input;
            _finished = finished;
            _buffService = buffService;
            _director = GetComponent<PlayableDirector>();
            _director.playOnAwake = false;
            _director.stopped += OnStopped;
            _subscription = phases.Subscribe(OnPhaseChanged);

            ResetToIdleState();
        }

        private void OnPhaseChanged(DayPhaseChanged message)
        {
            var ending = message.Phase == DayPhase.Ending;
            if (!ending)
            {
                if (_ending)
                    ResetToIdleState();
                return;
            }

            if (_ending || _playing)
                return;

            _ending = true;
            _playing = true;
            _teaCompleted = false;
            _waitingToStartTea = false;
            _hudWasEnabled = hud.enabled;
            hud.enabled = false;
            _input.SetCutsceneActive(true);

            if (teaStage != null) teaStage.SetActive(true);
            SetNotebookGroupActive(true);
            SetTeaCeremonyGroupActive(false);
            if (teaMiniGame != null) teaMiniGame.HideImmediate();

            _director.time = 0;
            _director.Play();
        }

        private void ResetToIdleState()
        {
            if (_director != null && _director.state == PlayState.Playing)
                _director.Stop();

            _playing = false;
            _waitingToStartTea = false;
            _teaCompleted = false;
            _ending = false;

            if (_input != null)
                _input.SetCutsceneActive(false);

            if (teaStage != null) teaStage.SetActive(false);
            SetNotebookGroupActive(false);
            SetTeaCeremonyGroupActive(false);
            if (teaMiniGame != null) teaMiniGame.HideImmediate();
            if (newDayButton != null) newDayButton.gameObject.SetActive(false);

            if (hud != null && _hudWasEnabled)
                hud.enabled = _hudWasEnabled;
        }

        private void OnStopped(PlayableDirector director)
        {
            if (!_playing) return;
            _playing = false;

            if (_ending && teaMiniGame != null)
            {
                _waitingToStartTea = true;
                ShowNewDayButtonInternal("Продолжить");
                return;
            }

            _input.SetCutsceneActive(false);
            if (_ending) _finished.Publish(new DayEndingCutsceneFinished());
        }

        private void OnTeaCompleted(TeaIngredientOption option)
        {
            _teaCompleted = true;
            _input.SetCutsceneActive(false);
            if (option != null && _buffService != null)
            {
                _buffService.SetPendingBuff(option.type);
            }
            if (_ending) _finished.Publish(new DayEndingCutsceneFinished());

            // If notebook writer is not used, show button immediately
            if (notebookWriter == null || !notebookWriter.isActiveAndEnabled)
                ShowNewDayButtonInternal("Следующий день");
        }

        private void ShowNewDayButton(TMPWriter writer)
        {
            if (!_ending) return;
            if (!_teaCompleted && teaMiniGame != null) return;

            ShowNewDayButtonInternal("Следующий день");
        }

        private void ShowNewDayButtonInternal(string label = null)
        {
            if (notebookWriter != null) notebookWriter.enabled = false;
            if (newDayButton != null)
            {
                newDayButton.gameObject.SetActive(true);
                if (!string.IsNullOrEmpty(label))
                    SetButtonLabel(newDayButton, label);
            }
        }

        private void OnNewDayButtonClicked()
        {
            if (_waitingToStartTea)
            {
                _waitingToStartTea = false;
                if (newDayButton != null) newDayButton.gameObject.SetActive(false);

                SetNotebookGroupActive(false);
                SetTeaCeremonyGroupActive(true);
                if (teaMiniGame != null) teaMiniGame.Begin();
                return;
            }

            StartNewDay();
        }

        private void SetNotebookGroupActive(bool active)
        {
            if (notebookGroup != null)
                notebookGroup.SetActive(active);
        }

        private void SetTeaCeremonyGroupActive(bool active)
        {
            if (teaCeremonyGroup != null)
                teaCeremonyGroup.SetActive(active);
        }

        private static void SetButtonLabel(Button button, string text)
        {
            var tmp = button.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = text;
                return;
            }

            var uiText = button.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (uiText != null)
                uiText.text = text;
        }

        private void StartNewDay()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            if (teaMiniGame != null) teaMiniGame.Completed -= OnTeaCompleted;
            if (notebookWriter != null) notebookWriter.OnFinishWriter.RemoveListener(ShowNewDayButton);
            if (newDayButton != null) newDayButton.onClick.RemoveListener(OnNewDayButtonClicked);
            if (_director != null) _director.stopped -= OnStopped;
        }
    }
}
