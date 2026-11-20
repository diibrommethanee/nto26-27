using System;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private TMP_Text[] textParts;
        [SerializeField] private TMP_Text result;
        [SerializeField] private Color successColor;
        [SerializeField] private Color failureColor;

        private readonly List<List<Clue>> _options = new();
        private EvidenceService _evidence;
        private Hypothesis _hypothesis;
        private Button _checkButton;
        private TMP_Dropdown[] _slots;

        private void OnDestroy()
        {
            if (_checkButton != null)
                _checkButton.onClick.RemoveListener(Check);
        }

        public void Initialize(EvidenceService evidence)
        {
            _evidence = evidence;
            _slots = GetComponentsInChildren<TMP_Dropdown>(true);
            _checkButton = GetComponentInChildren<Button>(true);
            _checkButton.onClick.AddListener(Check);
        }

        public void Show(Hypothesis hypothesis)
        {
            CloseChoices();
            _options.Clear();
            _hypothesis = hypothesis;
            gameObject.SetActive(hypothesis != null);
            if (hypothesis == null)
                return;

            var fillCount = hypothesis.nodes.Count(node => node.type == HypothesisNodeType.Fill);
            if (fillCount > _slots.Length || textParts.Length != _slots.Length + 1)
                throw new InvalidOperationException($"The scene layout does not support hypothesis '{hypothesis.name}'.");

            title.text = hypothesis.title;
            result.text = string.Empty;
            for (var index = 0; index < _slots.Length; index++)
            {
                _slots[index].SetValueWithoutNotify(0);
                _slots[index].gameObject.SetActive(index < fillCount);
            }

            var text = new StringBuilder();
            foreach (var node in hypothesis.nodes)
            {
                if (node.type == HypothesisNodeType.Text)
                {
                    text.Append(node.text);
                    continue;
                }

                SetTextPart(_options.Count, text.ToString());
                text.Clear();
                _options.Add(new List<Clue>());
            }

            SetTextPart(fillCount, text.ToString());
            for (var index = fillCount + 1; index < textParts.Length; index++)
                textParts[index].gameObject.SetActive(false);

            _checkButton.interactable = fillCount > 0;
            RefreshClues();
        }

        private void SetTextPart(int index, string text)
        {
            var part = textParts[index];
            part.text = text.Trim();
            part.gameObject.SetActive(part.text.Length > 0);
        }

        public void RefreshClues()
        {
            for (var index = 0; index < _options.Count; index++)
            {
                var slot = _slots[index];
                var selectedIndex = slot.value;
                if (slot.IsExpanded)
                    slot.Hide();

                var options = _options[index];
                options.Clear();

                var labels = new List<string> { "______" };
                foreach (var clue in _evidence.Collected)
                {
                    if (clue.fillIndex != index || string.IsNullOrWhiteSpace(clue.fillWord))
                        continue;

                    options.Add(clue);
                    labels.Add(clue.fillWord);
                }

                slot.ClearOptions();
                slot.AddOptions(labels);
                slot.SetValueWithoutNotify(selectedIndex);
                slot.RefreshShownValue();
                slot.interactable = options.Count > 0;
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

        private Clue GetSelection(int index)
        {
            var choice = _slots[index].value - 1;
            return choice >= 0 ? _options[index][choice] : null;
        }

        private void Check()
        {
            var answers = new Clue[_options.Count];
            for (var index = 0; index < answers.Length; index++)
                answers[index] = GetSelection(index);

            var solved = _evidence.CheckHypothesis(_hypothesis, answers);
            result.text = solved ? "Гипотеза подтверждена." : "Гипотеза не подтверждена.";
            result.color = solved ? successColor : failureColor;
            _checkButton.interactable = !solved;
        }
    }
}
