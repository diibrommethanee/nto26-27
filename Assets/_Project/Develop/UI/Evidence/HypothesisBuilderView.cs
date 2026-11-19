using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _Project.Develop.Data.Evidence;
using _Project.Develop.Gameplay.Evidence;
using TMPro;
using UnityEngine;

namespace _Project.Develop.UI.Evidence
{
    public class HypothesisBuilderView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text[] textParts;
        [SerializeField] private TMP_Text result;
        [SerializeField] private UnityEngine.UI.Button checkButton;
        [SerializeField] private TMP_Dropdown[] slots;
        [SerializeField] private Color successColor;
        [SerializeField] private Color failureColor;

        private readonly List<List<EvidenceData>> _options = new();
        private EvidenceService _evidence;
        private HypothesisData _hypothesis;

        public event Action<HypothesisData> Solved;

        private void Awake()
        {
            checkButton.onClick.AddListener(Check);
        }

        private void OnDestroy()
        {
            checkButton.onClick.RemoveListener(Check);
        }

        public void Initialize(EvidenceService evidence)
        {
            _evidence = evidence;
        }

        public void Show(HypothesisData hypothesis)
        {
            CloseChoices();
            _options.Clear();
            _hypothesis = hypothesis;
            gameObject.SetActive(hypothesis != null);
            if (hypothesis == null)
                return;

            var fillCount = hypothesis.nodes.Count(node => node.type == HypothesisNodeType.Fill);
            if (fillCount > slots.Length || textParts.Length != slots.Length + 1)
                throw new InvalidOperationException($"The scene layout does not support hypothesis '{hypothesis.name}'.");

            title.text = hypothesis.title;
            result.text = string.Empty;
            for (var index = 0; index < slots.Length; index++)
            {
                slots[index].ClearOptions();
                slots[index].gameObject.SetActive(index < fillCount);
            }

            foreach (var part in textParts)
                part.gameObject.SetActive(false);

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
                _options.Add(new List<EvidenceData>());
            }

            SetTextPart(fillCount, text.ToString());
            checkButton.interactable = fillCount > 0;
            RefreshEvidence();
        }

        private void SetTextPart(int index, string text)
        {
            var part = textParts[index];
            part.text = text.Trim();
            part.gameObject.SetActive(part.text.Length > 0);
        }

        public void RefreshEvidence()
        {
            for (var index = 0; index < _options.Count; index++)
            {
                var selected = GetSelection(index);
                var slot = slots[index];
                if (slot.IsExpanded)
                    slot.Hide();

                var options = _options[index];
                options.Clear();

                var labels = new List<string> { "______" };
                foreach (var evidence in _evidence.Collected)
                {
                    if (evidence.fillIndex != index || string.IsNullOrWhiteSpace(evidence.fillWord))
                        continue;

                    options.Add(evidence);
                    labels.Add(evidence.fillWord);
                }

                slot.ClearOptions();
                slot.AddOptions(labels);
                slot.SetValueWithoutNotify(options.IndexOf(selected) + 1);
                slot.RefreshShownValue();
                slot.interactable = options.Count > 0;
            }
        }

        public void CloseChoices()
        {
            foreach (var slot in slots)
            {
                if (slot.IsExpanded)
                    slot.Hide();
            }
        }

        private EvidenceData GetSelection(int index)
        {
            var choice = slots[index].value - 1;
            return choice >= 0 ? _options[index][choice] : null;
        }

        private void Check()
        {
            var answers = new EvidenceData[_options.Count];
            for (var index = 0; index < answers.Length; index++)
                answers[index] = GetSelection(index);

            var solved = _evidence.CheckHypothesis(_hypothesis, answers);
            result.text = solved ? "Гипотеза подтверждена." : "Гипотеза не подтверждена.";
            result.color = solved ? successColor : failureColor;
            checkButton.interactable = !solved;

            if (solved)
                Solved?.Invoke(_hypothesis);
        }
    }
}
