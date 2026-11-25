using System;
using _Project.Develop.Gameplay.Input;
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
        [SerializeField] private Canvas hud;
        [SerializeField] private TMPWriter notebookWriter;
        [SerializeField] private Button newDayButton;

        private InputController _input;
        private IPublisher<DayEndingCutsceneFinished> _finished;
        private IDisposable _subscription;
        private PlayableDirector _director;
        private bool _playing;
        private bool _hudWasEnabled;
        private bool _ending;

        private void Awake()
        {
            newDayButton.gameObject.SetActive(false);
            newDayButton.onClick.AddListener(StartNewDay);
            notebookWriter.OnFinishWriter.AddListener(ShowNewDayButton);
        }

        [Inject]
        private void Construct(IBufferedSubscriber<DayPhaseChanged> phases, InputController input,
            IPublisher<DayEndingCutsceneFinished> finished)
        {
            _input = input;
            _finished = finished;
            _director = GetComponent<PlayableDirector>();
            _director.stopped += OnStopped;
            _subscription = phases.Subscribe(OnPhaseChanged);
        }

        private void OnPhaseChanged(DayPhaseChanged message)
        {
            var ending = message.Phase == DayPhase.Ending;
            if (ending == _ending) return;
            _ending = ending;
            if (ending)
            {
                _playing = true;
                _hudWasEnabled = hud.enabled;
                hud.enabled = false;
                _input.SetCutsceneActive(true);
                teaStage.SetActive(true);
                _director.time = 0;
                _director.Play();
            }
            else
            {
                _director.Stop();
                _playing = false;
                _input.SetCutsceneActive(false);
                teaStage.SetActive(false);
                hud.enabled = _hudWasEnabled;
            }
        }

        private void OnStopped(PlayableDirector director)
        {
            if (!_playing) return;
            _playing = false;
            _input.SetCutsceneActive(false);
            if (_ending) _finished.Publish(new DayEndingCutsceneFinished());
        }

        private void ShowNewDayButton(TMPWriter writer)
        {
            if (!_ending) return;

            writer.enabled = false;
            newDayButton.gameObject.SetActive(true);
        }

        private void StartNewDay()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            if (notebookWriter != null) notebookWriter.OnFinishWriter.RemoveListener(ShowNewDayButton);
            if (newDayButton != null) newDayButton.onClick.RemoveListener(StartNewDay);
            if (_director != null) _director.stopped -= OnStopped;
        }
    }
}
