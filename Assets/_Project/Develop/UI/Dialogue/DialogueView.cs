using _Project.Develop.Data.Evidence;
using _Project.Develop.Data.Npc;
using _Project.Develop.Gameplay.NPC;
using PrimeTween;
using _Project.Develop.Gameplay.Dialogue;
using _Project.Develop.UI.Evidence;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        
        [SerializeField] private EvidenceView notebook;

        [Header("World dialogue")]
        [SerializeField] private Canvas worldCanvas;
        [SerializeField] private TMP_Text worldCharacter;
        [SerializeField] private TMP_Text worldContent;
        [SerializeField] private Button worldContinue;
        [SerializeField] private RectTransform worldBlob;
        [SerializeField] private NpcInteract[] speakers;
        [SerializeField] private Transform worldFallbackAnchor;

        private TMPWriter _worldWriter;
        private Tween _blobEntrance;
        private Vector3 _canvasWorldScale;
        private TMPWriter ActiveWriter => worldCanvas ? _worldWriter : _contentWriter;
        private Clue _lineClue;

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
            _choiceTemplate = choicesContainer.GetComponentInChildren<Button>(true);
            SetVisible(false);

            if (worldCanvas)
            {
                _canvasWorldScale = worldCanvas.transform.lossyScale;
                _worldWriter = worldContent.GetComponent<TMPWriter>();
                worldContinue.onClick.AddListener(OnContinueClicked);
                HideWorldLine();
            }
            else
            {
                _continueButton = GetComponent<Button>();
                _contentWriter = contentText.GetComponent<TMPWriter>();
                ClearText();
                _continueButton.onClick.AddListener(OnContinueClicked);
            }
        }

        private void OnDialogueShown(DialogueShown message)
        {
            ClearChoices();
            
            var dialogue = message.dialogue;
            _lineClue = dialogue.clue;
            var text = dialogue.text ?? string.Empty;
            if (_lineClue && notebook)
                text = TagCluePhrases(text);
            var speakerName = !string.IsNullOrWhiteSpace(dialogue.overrideName) ? dialogue.overrideName
                : dialogue.character != null ? dialogue.character.displayName
                    : string.Empty;

            text = FormatText(text);
            if (worldCanvas)
            {
                SetVisible(false);
                ShowWorldLine(dialogue.character, speakerName, text);
                return;
            }

            _contentWriter.StopWriter();
            characterText.text = speakerName;
            SetVisible(true);

            _contentWriter.SetText(text);
            _contentWriter.StartWriter();
        }

        private void ShowWorldLine(NpcData speaker, string speakerName, string text)
        {
            var anchor = FindAnchor(speaker) ?? FindAnchor(_runner.Interlocutor) ?? worldFallbackAnchor;

            var canvasTransform = worldCanvas.transform;
            canvasTransform.SetParent(null, false);
            canvasTransform.SetPositionAndRotation(anchor.position, Quaternion.identity);
            canvasTransform.localScale = _canvasWorldScale;
            canvasTransform.SetParent(anchor, true);

            var opensRight = anchor.position.x >= anchor.parent.position.x;
            worldBlob.pivot = new Vector2(opensRight ? 0f : 1f, 0.5f);
            worldCanvas.gameObject.SetActive(true);
            worldCharacter.text = speakerName;
            worldCharacter.gameObject.SetActive(!string.IsNullOrEmpty(speakerName));
            worldBlob.sizeDelta = new Vector2(
                Mathf.Clamp(worldContent.GetPreferredValues(text).x + 64f, 270f, 520f),
                120f);
            worldBlob.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(110f, worldContent.GetPreferredValues(text, worldBlob.rect.width - 64f, 0).y + 80f));

            _blobEntrance.Stop();
            worldBlob.localScale = Vector3.one * 0.94f;
            _blobEntrance = Tween.Scale(worldBlob, 1f, 0.19f, Ease.OutCubic);
            if (_worldWriter.IsWriting) _worldWriter.StopWriter();
            _worldWriter.SetText(text);
            _worldWriter.StartWriter();
        }

        private Transform FindAnchor(NpcData speaker)
        {
            if (!speaker) return null;
            foreach (var npc in speakers)
                if (npc && npc.gameObject.activeInHierarchy && npc.Data == speaker)
                    return npc.DialogueAnchor;
            return null;
        }

        private void HideWorldLine()
        {
            if (!worldCanvas) return;
            _blobEntrance.Stop();
            if (_worldWriter.IsWriting) _worldWriter.StopWriter();
            worldCanvas.gameObject.SetActive(false);
        }

        private static string TagCluePhrases(string text)
        {
            var index = 0;
            return Regex.Replace(text, @"<(?<tag>highlight|wave|h|w)>(?<phrase>.*?)</\k<tag>>",
                match => string.IsNullOrWhiteSpace(match.Groups["phrase"].Value)
                    ? match.Value
                    : $"<link=\"{EvidenceView.DialogueClueLinkPrefix}{index++}\">{match.Value}</link>",
                RegexOptions.Singleline);
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
            HideWorldLine();
            if (!worldCanvas)
            {
                _contentWriter.StopWriter();
                contentText.enabled = false;
            }

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
            if (!worldCanvas) contentText.enabled = true;
        }

        private void OnDialogueEnded(DialogueEnded message)
        {
            ClearChoices();
            SetVisible(false);
            HideWorldLine();
            if (!worldCanvas) ClearText();
        }

        private void OnContinueClicked()
        {
            if (!_runner.IsRunning || _runner.IsChoosing)
                return;

            var writer = ActiveWriter;
            if (writer.IsWriting)
            {
                writer.SkipWriter();
                return;
            }
            
            if (notebook && _lineClue)
            {
                notebook.ContinueWithClueText(_lineClue, worldCanvas ? worldContent : contentText, _runner.Continue);
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

            if (_continueButton) _continueButton.onClick.RemoveListener(OnContinueClicked);
            if (worldContinue) worldContinue.onClick.RemoveListener(OnContinueClicked);
            _blobEntrance.Stop();
        }
    }
}
