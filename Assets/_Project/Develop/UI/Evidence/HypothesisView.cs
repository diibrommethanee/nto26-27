using System.Collections.Generic;
using System.Text;
using _Project.Develop.Data.Evidence;
using _Project.Develop.Gameplay.Evidence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Develop.UI.Evidence
{
    public class HypothesisView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private RectTransform sentence;
        [SerializeField] private TMP_Text textTemplate;
        [SerializeField] private TMP_Dropdown slotTemplate;
        [SerializeField, HideInInspector] private TMP_Text[] textParts;

        private readonly List<List<Clue>> _options = new();
        private EvidenceService _evidence;
        private Hypothesis _hypothesis;
        private readonly List<TMP_Dropdown> _slots = new();
        private readonly List<GameObject> _sentenceItems = new();

        public void Initialize(EvidenceService evidence)
        {
            _evidence = evidence;
            if (!sentence)
            {
                // Older scenes keep the sample elements directly inside the sentence.
                textTemplate = textParts[0];
                sentence = (RectTransform)textTemplate.transform.parent;
                slotTemplate = sentence.GetComponentInChildren<TMP_Dropdown>(true);
                foreach (Transform item in sentence)
                    item.gameObject.SetActive(false);
            }
            // Scenes saved before automatic checking may still contain these controls.
            var legacyCheck = transform.Find("Check");
            if (legacyCheck) legacyCheck.gameObject.SetActive(false);
            var legacyResult = transform.Find("Result");
            if (legacyResult) legacyResult.gameObject.SetActive(false);
        }

        public void Show(Hypothesis hypothesis)
        {
            CloseChoices();
            foreach (var item in _sentenceItems)
            {
                item.SetActive(false);
                Destroy(item);
            }

            _sentenceItems.Clear();
            _slots.Clear();
            _options.Clear();
            _hypothesis = hypothesis;
            gameObject.SetActive(hypothesis != null);
            if (hypothesis == null)
                return;

            title.text = hypothesis.title;

            var text = new StringBuilder();
            foreach (var node in hypothesis.nodes)
            {
                if (node.type == HypothesisNodeType.Text)
                {
                    text.Append(node.text);
                    continue;
                }

                AddTextPart(text.ToString());
                text.Clear();

                var slot = Instantiate(slotTemplate, sentence);
                slot.gameObject.SetActive(true);
                slot.SetValueWithoutNotify(0);
                slot.onValueChanged.AddListener(OnSelectionChanged);
                _sentenceItems.Add(slot.gameObject);
                _slots.Add(slot);
                _options.Add(new List<Clue>());
            }

            AddTextPart(text.ToString());
            RefreshClues();

            LayoutRebuilder.ForceRebuildLayoutImmediate(sentence);
            var scroll = sentence.GetComponentInParent<ScrollRect>();
            if (scroll)
            {
                scroll.StopMovement();
                scroll.normalizedPosition = new Vector2(0f, 1f);
            }
        }

        private void AddTextPart(string text)
        {
            text = text.Trim();
            if (text.Length == 0) return;

            var part = Instantiate(textTemplate, sentence);
            part.text = text;
            part.textWrappingMode = TextWrappingModes.Normal;
            part.gameObject.SetActive(true);
            _sentenceItems.Add(part.gameObject);
        }

        public void RefreshClues()
        {
            if (_slots.Count == 0) return;
            var solved = _evidence.TryGetHypothesisAnswers(_hypothesis, out var answers);
            RebuildOptions(solved ? answers : ReadSelections(), solved);
        }

        private void RebuildOptions(IReadOnlyList<Clue> selections, bool solved)
        {
            var usedClues = new HashSet<Clue>(selections);
            for (var index = 0; index < _options.Count; index++)
            {
                var slot = _slots[index];
                var selected = selections[index];
                if (slot.IsExpanded)
                    slot.Hide();

                var options = _options[index];
                options.Clear();

                var labels = new List<string> { "______" };
                foreach (var clue in _evidence.Collected)
                {
                    if (clue.hypothesis != _hypothesis || string.IsNullOrWhiteSpace(clue.fillWord) ||
                        (clue != selected && usedClues.Contains(clue)))
                        continue;

                    options.Add(clue);
                    labels.Add(clue.fillWord);
                }

                slot.ClearOptions();
                slot.AddOptions(labels);
                slot.SetValueWithoutNotify(options.IndexOf(selected) + 1);
                slot.RefreshShownValue();
                slot.interactable = !solved && options.Count > 0;
            }
        }

        public void CloseChoices()
        {
            foreach (var slot in _slots)
            {
                if (slot.IsExpanded)
                    slot.Hide();
            }
        }

        private void OnSelectionChanged(int _)
        {
            if (_slots.Count == 0 || _evidence.HasSolvedHypothesis(_hypothesis))
                return;

            var answers = ReadSelections();
            var solved = System.Array.TrueForAll(answers, clue => clue != null) &&
                         _evidence.CheckHypothesis(_hypothesis, answers);
            RebuildOptions(answers, solved);
        }

        private Clue[] ReadSelections()
        {
            var selections = new Clue[_slots.Count];
            for (var index = 0; index < selections.Length; index++)
            {
                var choice = _slots[index].value;
                if (choice > 0)
                    selections[index] = _options[index][choice - 1];
            }

            return selections;
        }
    }
}
