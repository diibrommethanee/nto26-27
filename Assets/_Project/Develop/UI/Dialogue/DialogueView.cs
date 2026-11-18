using _Project.Develop.Gameplay.Dialogue;
using System;
using System.Collections.Generic;
using MessagePipe;
using TMPEffects.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.Develop.UI.Dialogue
{
    public class DialogueView : MonoBehaviour
    {
        [SerializeField] private TMP_Text characterText;
        [SerializeField] private TMP_Text contentText;
        [SerializeField] private RectTransform choicesContainer;
        
        private CanvasGroup _group;
        private Button _continueButton;

        private TMPWriter _contentWriter;
        private Button _choiceTemplate;

        private DialogueRunner _runner;
        private IDisposable _dialogueSubscription;
        private IDisposable _endedSubscription;
        
        private readonly List<Button> _choiceButtons = new();
        private IDisposable _choicesSubscription;

        [Inject]
        public void Construct(DialogueRunner runner, ISubscriber<DialogueShown> dialogueSubscriber,
            ISubscriber<DialogueEnded> endedSubscriber, ISubscriber<DialogueChoicesShown> choicesSubscriber)
        {
            _runner = runner;
            _dialogueSubscription = dialogueSubscriber.Subscribe(OnDialogueShown);
            _endedSubscription = endedSubscriber.Subscribe(OnDialogueEnded);
            _choicesSubscription = choicesSubscriber.Subscribe(OnChoicesShown);
        }

        private void Start()
        {
            _group = GetComponent<CanvasGroup>();
            _continueButton = GetComponent<Button>();
            
            _contentWriter = contentText.GetComponent<TMPWriter>();
            _choiceTemplate = choicesContainer.GetComponentInChildren<Button>(true);
            
            ClearText();
            SetVisible(false);
            
            _continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnDialogueShown(DialogueShown message)
        {
            ClearChoices();
            _contentWriter.StopWriter();
            
            var dialogue = message.dialogue;
            characterText.text = !string.IsNullOrWhiteSpace(dialogue.overrideName) ? dialogue.overrideName
                : dialogue.character != null ? dialogue.character.displayName
                    : string.Empty;

            SetVisible(true);
            
            _contentWriter.SetText(FormatText(dialogue.text));
            _contentWriter.StartWriter();
        }

        private static string FormatText(string text)
        {
            return (text ?? string.Empty)
                .Replace("<highlight>", "<color=#FFD54F>")
                .Replace("</highlight>", "</color>")
                .Replace("<h>", "<color=#FFD54F>")
                .Replace("</h>", "</color>")
                .Replace("<w>", "<wave>")
                .Replace("</w>", "</wave>");
        }

        private void OnChoicesShown(DialogueChoicesShown message)
        {
            ClearChoices();
            _contentWriter.StopWriter();
            contentText.enabled = false;

            for (var i = 0; i < message.choices.Count; i++)
            {
                var index = i;
                var button = Instantiate(_choiceTemplate, choicesContainer);
                button.GetComponentInChildren<TMP_Text>(true).text = message.choices[index].text;
                button.onClick.AddListener(() => _runner.Choose(index));
                _choiceButtons.Add(button);
                button.gameObject.SetActive(true);
            }

            SetVisible(true);
        }

        private void ClearChoices()
        {
            foreach (var button in _choiceButtons)
            {
                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }

            _choiceButtons.Clear();
            contentText.enabled = true;
        }

        private void OnDialogueEnded(DialogueEnded message)
        {
            ClearChoices();
            SetVisible(false);
            ClearText();
        }

        private void OnContinueClicked()
        {
            if (!_runner.IsRunning || _runner.IsChoosing)
                return;

            if (_contentWriter.IsWriting)
            {
                _contentWriter.SkipWriter();
                return;
            }
            
            _runner.Continue();
        }

        private void SetVisible(bool visible)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }
        
        private void ClearText()
        {
            characterText.text = string.Empty;
            
            _contentWriter.StopWriter();
            _contentWriter.SetText(string.Empty);
        }

        private void OnDestroy()
        {
            _dialogueSubscription?.Dispose();
            _endedSubscription?.Dispose();
            _choicesSubscription?.Dispose();

            _continueButton.onClick.RemoveListener(OnContinueClicked);
        }
    }
}
