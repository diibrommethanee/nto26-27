using _Project.Develop.Gameplay.Dialogue;
using System;
using MessagePipe;
using TMPEffects.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.Develop.UI.Dialogue
{
    public sealed class DialogueView : MonoBehaviour
    {
        [SerializeField] private TMP_Text characterText;
        [SerializeField] private TMP_Text contentText;
        
        private CanvasGroup _group;
        private Button _continueButton;

        private TMPWriter _contentWriter;

        private DialogueRunner _runner;
        private IDisposable _statementSubscription;
        private IDisposable _endedSubscription;

        [Inject]
        public void Construct(DialogueRunner runner, ISubscriber<DialogueStatementShown> statementSubscriber, ISubscriber<DialogueEnded> endedSubscriber)
        {
            _runner = runner;
            _statementSubscription = statementSubscriber.Subscribe(OnStatementShown);
            _endedSubscription = endedSubscriber.Subscribe(OnDialogueEnded);
        }

        private void Start()
        {
            _group = GetComponent<CanvasGroup>();
            _continueButton = GetComponent<Button>();
            
            _contentWriter = contentText.GetComponent<TMPWriter>();
            
            ClearText();
            SetVisible(false);
            
            _continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnStatementShown(DialogueStatementShown message)
        {
            _contentWriter.StopWriter();
            
            var statement = message.statement;
            characterText.text = !string.IsNullOrWhiteSpace(statement.overrideName)
                ? statement.overrideName
                : statement.character != null
                    ? statement.character.displayName
                    : string.Empty;

            SetVisible(true);
            
            _contentWriter.SetText(statement.text);
            _contentWriter.StartWriter();
        }

        private void OnDialogueEnded(DialogueEnded message)
        {
            SetVisible(false);
            ClearText();
        }

        private void OnContinueClicked()
        {
            if (!_runner.IsRunning)
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
            _statementSubscription?.Dispose();
            _endedSubscription?.Dispose();

            _continueButton.onClick.RemoveListener(OnContinueClicked);
        }
    }
}
