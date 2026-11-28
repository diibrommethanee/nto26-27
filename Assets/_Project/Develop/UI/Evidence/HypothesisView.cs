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

        private EvidenceService _evidence;
        private Hypothesis _hypothesis;
        private readonly List<HypothesisAnswerSlot> _slots = new();
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
            _hypothesis = hypothesis;
            gameObject.SetActive(hypothesis != null);
            if (hypothesis == null)
                return;

            title.text = hypothesis.title;

            BuildSentence(hypothesis);
            RefreshClues();

            LayoutRebuilder.ForceRebuildLayoutImmediate(sentence);
            var scroll = sentence.GetComponentInParent<ScrollRect>();
            if (scroll)
            {
                scroll.StopMovement();
                scroll.normalizedPosition = new Vector2(0f, 1f);
            }
        }

        private void BuildSentence(Hypothesis hypothesis)
        {
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
                _slots.Add(new HypothesisAnswerSlot(slot));
            }

            AddTextPart(text.ToString());
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
            for (var index = 0; index < _slots.Count; index++)
                _slots[index].Refresh(_hypothesis, index, _evidence.Collected, selections[index], usedClues, solved);
        }

        public void CloseChoices()
        {
            foreach (var slot in _slots)
                slot.Close();
        }

        private void OnSelectionChanged(int _)
        {
            if (_slots.Count == 0 || _evidence.HasSolvedHypothesis(_hypothesis))
                return;

            var answers = ReadSelections();
            var solved = _evidence.CheckHypothesis(_hypothesis, answers);
            RebuildOptions(answers, solved);
        }

        private Clue[] ReadSelections()
        {
            var selections = new Clue[_slots.Count];
            for (var index = 0; index < selections.Length; index++)
                selections[index] = _slots[index].Selection;

            return selections;
        }
    }
}
