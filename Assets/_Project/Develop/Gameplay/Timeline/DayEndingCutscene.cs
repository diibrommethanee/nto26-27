using System;
using System.Collections.Generic;
using _Project.Develop.Data.Npc;
using _Project.Develop.Gameplay.Dialogue;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Npc;
using _Project.Develop.Gameplay.NPC;
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
        [Header("Day ending")]
        [SerializeField] private GameObject teaStage;
        [SerializeField] private GameObject notebookGroup;
        [SerializeField] private GameObject teaCeremonyGroup;
        [SerializeField] private TeaBrewingView teaMiniGame;
        [SerializeField] private Canvas hud;
        [SerializeField] private TMPWriter notebookWriter;
        [SerializeField] private Button newDayButton;

        [Header("NPC tea (optional separate canvas)")]
        [SerializeField] private GameObject npcTeaStage;
        [SerializeField] private GameObject npcTeaCeremonyGroup;
        [SerializeField] private NpcTeaBrewingView npcTeaMiniGame;

        private InputController _input;
        private DialogueRunner _dialogueRunner;
        private NextDayBuffService _buffService;
        private EvidenceService _evidenceService;
        private IPublisher<DayEndingCutsceneFinished> _finished;
        private IDisposable _subscription;
        private PlayableDirector _director;
        private bool _playing;
        private bool _hudWasEnabled;
        private bool _ending;
        private bool _teaCompleted;
        private bool _waitingToStartTea;
        private bool _teaForNpc;

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

            if (npcTeaMiniGame != null)
            {
                npcTeaMiniGame.NpcCompleted += OnNpcTeaCompleted;
            }
        }

        [Inject]
        private void Construct(IBufferedSubscriber<DayPhaseChanged> phases, InputController input, DialogueRunner dialogueRunner,
            IPublisher<DayEndingCutsceneFinished> finished, NextDayBuffService buffService,
            EvidenceService evidenceService)
        {
            _input = input;
            _dialogueRunner = dialogueRunner;
            _finished = finished;
            _buffService = buffService;
            _evidenceService = evidenceService;
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
            _teaForNpc = false;
            _ending = false;

            if (_input != null)
                _input.SetCutsceneActive(false);

            if (teaStage != null) teaStage.SetActive(false);
            SetNotebookGroupActive(false);
            SetTeaCeremonyGroupActive(false);
            if (teaMiniGame != null) teaMiniGame.HideImmediate();

            HideNpcTeaCanvas();

            if (newDayButton != null) newDayButton.gameObject.SetActive(false);

            if (hud != null && _hudWasEnabled)
                hud.enabled = _hudWasEnabled;
        }

        public bool CanServeTea(NpcData npc)
        {
            return npc != null && npc.state != null && !npc.state.teaServed;
        }

        public void BeginNpcTea(NpcData npc)
        {
            var view = GetNpcTeaView();
            if (npc == null || view == null || _ending || _teaForNpc || !CanServeTea(npc))
                return;

            _teaForNpc = true;
            _hudWasEnabled = hud != null && hud.enabled;
            if (_dialogueRunner != null && _dialogueRunner.IsRunning)
                _dialogueRunner.Stop();

            _input.SetCutsceneActive(true);
            if (hud != null)
                hud.enabled = false;

            GetNpcTeaStage()?.SetActive(true);
            SetNpcTeaCeremonyGroupActive(true);
            view.BeginForNpc(npc);
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

        private void OnTeaCompleted(IReadOnlyList<TeaIngredientType> ingredients)
        {
            _teaCompleted = true;
            _input.SetCutsceneActive(false);

            if (_buffService != null)
                _buffService.SetPendingIngredients(ingredients);

            if (_ending) _finished.Publish(new DayEndingCutsceneFinished());

            // Keep tea view visible behind the next-day button; scene reload will clean up.

            // If notebook writer is active, let its finish event show the button; otherwise show now
            if (notebookWriter != null && notebookWriter.isActiveAndEnabled)
            {
                // Keep UI as-is; ShowNewDayButton will be called on writer finish
            }
            else
            {
                ShowNewDayButtonInternal("Следующий день");
            }
        }

        private void OnNpcTeaCompleted(NpcData npc, IReadOnlyList<TeaIngredientType> ingredients)
        {
            if (!_teaForNpc)
                return;

            if (npc != null)
            {
                if (npc.state == null)
                    npc.state = new Data.Npc.NpcState { mood = npc.initialMood, dialogue = npc.dialogue };

                npc.state.teaServed = true;

                if (_buffService != null)
                {
                    int delta = _buffService.MoodDeltaFor(npc, ingredients);
                    npc.state.mood += delta;
                    if (delta != 0)
                        Debug.Log($"[Tea] Applied mood {delta:+#;-#;0} to {npc.displayName} (new mood: {npc.state.mood})");
                }
            }

            _teaForNpc = false;
            if (hud != null)
                hud.enabled = _hudWasEnabled;
            _input.SetCutsceneActive(false);

            HideNpcTeaCanvas();
        }

        private NpcTeaBrewingView GetNpcTeaView() => npcTeaMiniGame;
        private GameObject GetNpcTeaStage() => npcTeaStage != null ? npcTeaStage : teaStage;
        private GameObject GetNpcTeaCeremonyGroup() => npcTeaCeremonyGroup != null ? npcTeaCeremonyGroup : teaCeremonyGroup;

        private void SetNpcTeaCeremonyGroupActive(bool active)
        {
            var group = GetNpcTeaCeremonyGroup();
            if (group != null)
                group.SetActive(active);
        }

        private void HideNpcTeaCanvas()
        {
            GetNpcTeaView()?.HideImmediate();
            SetNpcTeaCeremonyGroupActive(false);
            GetNpcTeaStage()?.SetActive(false);
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
            if (teaMiniGame != null)
                teaMiniGame.Completed -= OnTeaCompleted;

            if (npcTeaMiniGame != null)
                npcTeaMiniGame.NpcCompleted -= OnNpcTeaCompleted;

            if (notebookWriter != null) notebookWriter.OnFinishWriter.RemoveListener(ShowNewDayButton);
            if (newDayButton != null) newDayButton.onClick.RemoveListener(OnNewDayButtonClicked);
            if (_director != null) _director.stopped -= OnStopped;
        }
    }
}
