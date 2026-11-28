using System.Collections.Generic;
using _Project.Develop.Data.Evidence;
using TMPro;

namespace _Project.Develop.UI.Evidence
{
    internal class HypothesisAnswerSlot
    {
        private readonly TMP_Dropdown _dropdown;
        private readonly List<Clue> _options = new();

        public Clue Selection => _dropdown.value > 0 ? _options[_dropdown.value - 1] : null;

        public HypothesisAnswerSlot(TMP_Dropdown dropdown) => _dropdown = dropdown;

        public void Refresh(Hypothesis hypothesis, int index, IReadOnlyList<Clue> collected,
            Clue selected, HashSet<Clue> usedClues, bool solved)
        {
            Close();
            _options.Clear();
            var labels = new List<string> { "______" };
            foreach (var clue in collected)
            {
                if (!clue.FitsSlot(hypothesis, index) || (clue != selected && usedClues.Contains(clue)))
                    continue;

                _options.Add(clue);
                labels.Add(clue.fillWord);
            }

            _dropdown.ClearOptions();
            _dropdown.AddOptions(labels);
            _dropdown.SetValueWithoutNotify(_options.IndexOf(selected) + 1);
            _dropdown.RefreshShownValue();
            _dropdown.interactable = !solved && _options.Count > 0;
        }

        public void Close()
        {
            if (_dropdown.IsExpanded)
                _dropdown.Hide();
        }
    }
}
