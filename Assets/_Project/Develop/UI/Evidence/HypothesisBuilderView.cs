using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _Project.Develop.Data.Evidence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Develop.UI.Evidence
{
    /// <summary>Writes a hypothesis directly onto the notebook board.</summary>
    public sealed class HypothesisBuilderView : MonoBehaviour
    {
        private const int PlaceholderLength = 14;
        private const float PlaceholderWidth = 170f;
        private const float DropdownItemHeight = 48f;

        private sealed class FillChoice
        {
            public HypothesisNode Node;
            public TMP_Dropdown Dropdown;
            public List<EvidenceData> Evidence;
        }

        private readonly List<FillChoice> _fills = new();
        private RectTransform _board;
        private TMP_Text _title;
        private TMP_Text _hypothesisText;
        private TMP_Text _result;
        private Button _checkButton;
        private HypothesisData _hypothesis;
        private IReadOnlyList<EvidenceData> _evidence = Array.Empty<EvidenceData>();
        private Action<HypothesisData> _onSolved;

        /// <summary>Создаёт и настраивает отображение гипотезы как дочерний элемент Board.</summary>
        public static HypothesisBuilderView Create(RectTransform board)
        {
            var root = new GameObject("HypothesisTextOnBoard", typeof(RectTransform));
            root.transform.SetParent(board, false);

            var view = root.AddComponent<HypothesisBuilderView>();
            view._board = board;
            view.Build();
            root.SetActive(false);
            return view;
        }

        /// <summary>Создаёт текст гипотезы, область результата и кнопку проверки на листе.</summary>
        private void Build()
        {
            var area = (RectTransform)transform;
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = area.offsetMax = Vector2.zero;

            _title = CreateText("HypothesisTitle", transform, 30, FontStyles.Bold);
            Place(_title.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.68f, 0.95f));
            _title.color = InkColor;
            _title.alignment = TextAlignmentOptions.MidlineLeft;

            _hypothesisText = CreateText("HypothesisText", transform, 25, FontStyles.Normal);
            Place(_hypothesisText.rectTransform, new Vector2(0.08f, 0.43f), new Vector2(0.92f, 0.85f));
            _hypothesisText.color = InkColor;
            _hypothesisText.alignment = TextAlignmentOptions.TopLeft;
            _hypothesisText.textWrappingMode = TextWrappingModes.Normal;

            _result = CreateText("HypothesisResult", transform, 18, FontStyles.Normal);
            Place(_result.rectTransform, new Vector2(0.08f, 0.36f), new Vector2(0.70f, 0.42f));
            _result.color = InkColor;
            _result.alignment = TextAlignmentOptions.MidlineLeft;

            _checkButton = CreateCheckButton(transform);
            _checkButton.onClick.AddListener(CheckHypothesis);
        }

        private static readonly Color InkColor = new(0.16f, 0.14f, 0.12f, 1f);

        /// <summary>Показывает гипотезу и строит Dropdown в позициях её Fill-нод.</summary>
        public void Show(HypothesisData hypothesis, IReadOnlyList<EvidenceData> evidence,
            Action<HypothesisData> onSolved)
        {
            _hypothesis = hypothesis;
            _evidence = evidence ?? Array.Empty<EvidenceData>();
            _onSolved = onSolved;
            _result.text = string.Empty;
            _checkButton.interactable = true;

            if (hypothesis == null)
            {
                ClearSlots();
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            _title.text = hypothesis.title;
            RebuildHypothesisText(null);
            transform.SetAsLastSibling();
        }

        /// <summary>Обновляет варианты Dropdown после получения новых улик, сохраняя выбор игрока.</summary>
        public void RefreshEvidence(IReadOnlyList<EvidenceData> evidence)
        {
            var previousSelections = _fills.Select(fill =>
            {
                var selectedIndex = fill.Dropdown.value - 1;
                return selectedIndex >= 0 && selectedIndex < fill.Evidence.Count
                    ? fill.Evidence[selectedIndex]
                    : null;
            }).ToList();

            _evidence = evidence ?? Array.Empty<EvidenceData>();
            RebuildHypothesisText(previousSelections);
        }

        /// <summary>Собирает текст нод и размещает элементы выбора поверх символов пропуска.</summary>
        private void RebuildHypothesisText(IReadOnlyList<EvidenceData> previousSelections)
        {
            ClearSlots();
            var source = new StringBuilder();
            var slotRanges = new List<(FillChoice fill, int start, int length)>();

            foreach (var node in _hypothesis.nodes)
            {
                if (node.type == HypothesisNodeType.Text)
                {
                    source.Append(node.text);
                    continue;
                }

                var start = source.Length;
                source.Append('_', PlaceholderLength);
                var fill = new FillChoice
                {
                    Node = node,
                    Evidence = GetTextEvidence()
                };
                _fills.Add(fill);
                slotRanges.Add((fill, start, PlaceholderLength));
            }

            _hypothesisText.text = source.ToString();
            Canvas.ForceUpdateCanvases();
            _hypothesisText.ForceMeshUpdate();

            for (var i = 0; i < slotRanges.Count; i++)
            {
                var slot = slotRanges[i];
                var rect = GetSlotRect(slot.start, slot.length);
                var choices = CreateDropdown(transform, rect);
                slot.fill.Dropdown = choices;
                var previous = previousSelections != null && i < previousSelections.Count
                    ? previousSelections[i]
                    : null;
                PopulateDropdown(slot.fill, previous);
            }
        }

        /// <summary>Находит координаты символов пропуска в TMP-тексте и переводит их в координаты Board.</summary>
        private RectTransform GetSlotRect(int sourceStart, int sourceLength)
        {
            var info = _hypothesisText.textInfo;
            var found = false;
            var left = float.MaxValue;
            var right = float.MinValue;
            var top = float.MinValue;
            var bottom = float.MaxValue;
            var sourceEnd = sourceStart + sourceLength;

            for (var i = 0; i < info.characterCount; i++)
            {
                var character = info.characterInfo[i];
                if (!character.isVisible || character.index < sourceStart || character.index >= sourceEnd)
                    continue;

                found = true;
                left = Mathf.Min(left, character.bottomLeft.x);
                right = Mathf.Max(right, character.topRight.x);
                top = Mathf.Max(top, character.topRight.y);
                bottom = Mathf.Min(bottom, character.bottomLeft.y);
            }

            if (!found)
                return PlaceAtBoardPoint(Vector2.zero, PlaceholderWidth, 42f);

            var center = new Vector3((left + right) * 0.5f, (top + bottom) * 0.5f, 0f);
            var boardPoint = _board.InverseTransformPoint(_hypothesisText.rectTransform.TransformPoint(center));
            var height = Mathf.Max(40f, top - bottom + 4f);
            return PlaceAtBoardPoint(boardPoint, Mathf.Max(PlaceholderWidth, right - left), height);
        }

        /// <summary>Создаёт Dropdown заданного размера и ставит его в указанную точку листа.</summary>
        private RectTransform PlaceAtBoardPoint(Vector2 position, float width, float height)
        {
            var dropdown = CreateDropdown(transform, width, height);
            var dropdownRect = (RectTransform)dropdown.transform;
            dropdownRect.anchoredPosition = position;
            return dropdownRect;
        }

        /// <summary>Растягивает RectTransform между заданными якорями без дополнительных отступов.</summary>
        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>Заполняет Dropdown текстами улик и восстанавливает ранее выбранную улику.</summary>
        private void PopulateDropdown(FillChoice fill, EvidenceData selectedEvidence)
        {
            var options = new List<TMP_Dropdown.OptionData>
            {
                new("__________")
            };

            options.AddRange(fill.Evidence.Select(item => new TMP_Dropdown.OptionData(item.fillWord)));

            fill.Dropdown.onValueChanged.RemoveAllListeners();
            fill.Dropdown.ClearOptions();
            fill.Dropdown.AddOptions(options);
            fill.Dropdown.SetValueWithoutNotify(0);
            fill.Dropdown.RefreshShownValue();
            fill.Dropdown.captionText.text = "__________";

            fill.Dropdown.onValueChanged.AddListener(value =>
            {
                var evidenceIndex = value - 1;
                fill.Dropdown.captionText.text = evidenceIndex >= 0 && evidenceIndex < fill.Evidence.Count
                    ? GetEvidenceText(fill.Evidence[evidenceIndex])
                    : "__________";
                fill.Dropdown.captionText.color = InkColor;
            });

            if (selectedEvidence != null)
            {
                var index = fill.Evidence.IndexOf(selectedEvidence);
                if (index >= 0)
                {
                    fill.Dropdown.SetValueWithoutNotify(index + 1);
                    fill.Dropdown.RefreshShownValue();
                    fill.Dropdown.captionText.text = GetEvidenceText(fill.Evidence[index]);
                }
            }

            fill.Dropdown.captionText.color = InkColor;
            fill.Dropdown.interactable = fill.Evidence.Count > 0;
        }

        /// <summary>Возвращает уникальные улики с заполненным текстом для подстановки.</summary>
        private List<EvidenceData> GetTextEvidence()
        {
            return _evidence
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.fillWord))
                .Distinct()
                .ToList();
        }

        /// <summary>Проверяет все Fill-ноды и сообщает, верно ли игрок собрал гипотезу.</summary>
        private void CheckHypothesis()
        {
            foreach (var fill in _fills)
            {
                var index = fill.Dropdown.value - 1;
                if (index < 0 || index >= fill.Evidence.Count || !IsCorrectEvidence(fill, fill.Evidence[index]))
                {
                    _result.text = "Гипотеза не подтверждена.";
                    _result.color = new Color(0.72f, 0.16f, 0.12f, 1f);
                    return;
                }
            }

            _result.text = "Гипотеза подтверждена.";
            _result.color = new Color(0.12f, 0.46f, 0.22f, 1f);
            _checkButton.interactable = false;
            _onSolved?.Invoke(_hypothesis);
        }

        /// <summary>Сравнивает выбранную улику с правильной уликой или ожидаемым текстом ноды.</summary>
        private static bool IsCorrectEvidence(FillChoice fill, EvidenceData selected)
        {
            if (fill.Node.correctEvidence != null)
                return selected == fill.Node.correctEvidence;

            return !string.IsNullOrWhiteSpace(fill.Node.correctAnswerFill) &&
                   string.Equals(selected.fillWord, fill.Node.correctAnswerFill, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Возвращает слово для гипотезы из данных улики.</summary>
        private static string GetEvidenceText(EvidenceData evidence)
        {
            return string.IsNullOrWhiteSpace(evidence.fillWord) ? evidence.title : evidence.fillWord;
        }

        /// <summary>Удаляет созданные Dropdown и очищает список Fill-нод перед перестроением.</summary>
        private void ClearSlots()
        {
            _fills.Clear();
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("EvidenceChoice", StringComparison.Ordinal))
                    Destroy(child.gameObject);
            }
        }

        /// <summary>Получает компонент TMP_Dropdown с уже подготовленного RectTransform.</summary>
        private static TMP_Dropdown CreateDropdown(Transform parent, RectTransform position)
        {
            return position.GetComponent<TMP_Dropdown>();
        }

        /// <summary>Создаёт и настраивает TMP_Dropdown, его шаблон списка и строки вариантов.</summary>
        private static TMP_Dropdown CreateDropdown(Transform parent, float width, float height)
        {
            var host = new GameObject("EvidenceChoice", typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
            host.transform.SetParent(parent, false);
            var rect = (RectTransform)host.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            var hostImage = host.GetComponent<Image>();
            hostImage.color = new Color(1f, 1f, 1f, 0.96f);

            var underline = new GameObject("Underline", typeof(RectTransform), typeof(Image));
            underline.transform.SetParent(host.transform, false);
            var underlineRect = (RectTransform)underline.transform;
            underlineRect.anchorMin = new Vector2(0, 0);
            underlineRect.anchorMax = new Vector2(1, 0);
            underlineRect.pivot = new Vector2(0.5f, 0);
            underlineRect.sizeDelta = new Vector2(0, 2);
            underline.GetComponent<Image>().color = new Color(0.25f, 0.36f, 0.49f, 0.85f);
            underline.GetComponent<Image>().raycastTarget = false;

            var caption = CreateText("Caption", host.transform, 23, FontStyles.Normal);
            var captionRect = caption.rectTransform;
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(2, 2);
            captionRect.offsetMax = new Vector2(-22, 0);
            caption.alignment = TextAlignmentOptions.Center;
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            caption.enableAutoSizing = true;
            caption.fontSizeMin = 13;
            caption.fontSizeMax = 23;
            caption.overflowMode = TextOverflowModes.Ellipsis;
            caption.raycastTarget = false;

            var arrow = CreateText("Arrow", host.transform, 18, FontStyles.Bold);
            arrow.text = "v";
            arrow.alignment = TextAlignmentOptions.Center;
            var arrowRect = arrow.rectTransform;
            arrowRect.anchorMin = new Vector2(1, 0);
            arrowRect.anchorMax = Vector2.one;
            arrowRect.pivot = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(22, height);
            arrowRect.anchoredPosition = Vector2.zero;
            arrow.raycastTarget = false;

            var templateObject = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateObject.transform.SetParent(host.transform, false);
            var template = (RectTransform)templateObject.transform;
            template.anchorMin = new Vector2(0, 0);
            template.anchorMax = new Vector2(1, 0);
            template.pivot = new Vector2(0.5f, 1);
            template.anchoredPosition = new Vector2(0, -2);
            template.sizeDelta = new Vector2(0, 420);
            templateObject.GetComponent<Image>().color = new Color(0.17f, 0.16f, 0.14f, 1);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(template, false);
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1);
            // TMP_Dropdown computes its item offsets from the template's initial
            // content rect. Match that rect to the template item height so the
            // generated rows start at the top and the final row stays in bounds.
            contentRect.sizeDelta = new Vector2(0, DropdownItemHeight);

            var item = new GameObject("Item", typeof(RectTransform), typeof(Image), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            var itemRect = (RectTransform)item.transform;
            itemRect.anchorMin = new Vector2(0, 1);
            itemRect.anchorMax = new Vector2(1, 1);
            itemRect.pivot = new Vector2(0.5f, 1);
            itemRect.sizeDelta = new Vector2(0, DropdownItemHeight);
            var itemImage = item.GetComponent<Image>();
            itemImage.color = new Color(0, 0, 0, 0);

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(item.transform, false);
            var checkRect = (RectTransform)check.transform;
            checkRect.anchorMin = new Vector2(1, 0.5f);
            checkRect.anchorMax = new Vector2(1, 0.5f);
            checkRect.sizeDelta = new Vector2(16, 16);
            checkRect.anchoredPosition = new Vector2(-8, 0);
            check.GetComponent<Image>().color = Color.white;

            var itemLabel = CreateText("Item Label", item.transform, 18, FontStyles.Normal);
            itemLabel.rectTransform.anchorMin = Vector2.zero;
            itemLabel.rectTransform.anchorMax = Vector2.one;
            itemLabel.rectTransform.offsetMin = itemLabel.rectTransform.offsetMax = Vector2.zero;
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabel.color = Color.white;
            itemLabel.margin = new Vector4(12, 0, 28, 0);
            itemLabel.raycastTarget = false;
            item.GetComponent<Toggle>().targetGraphic = itemImage;
            item.GetComponent<Toggle>().graphic = check.GetComponent<Image>();
            item.GetComponent<Toggle>().navigation = new Navigation { mode = Navigation.Mode.None };

            var scroll = templateObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 48;
            templateObject.SetActive(false);

            var dropdown = host.GetComponent<TMP_Dropdown>();
            dropdown.targetGraphic = hostImage;
            dropdown.captionText = caption;
            dropdown.template = template;
            dropdown.itemText = itemLabel;
            dropdown.navigation = new Navigation { mode = Navigation.Mode.None };
            return dropdown;
        }

        /// <summary>Создаёт TMP-текст с общими настройками шрифта и переносов.</summary>
        private static TMP_Text CreateText(string name, Transform parent, float size, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = InkColor;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Создаёт кнопку, запускающую проверку гипотезы.</summary>
        private static Button CreateCheckButton(Transform parent)
        {
            var go = new GameObject("CheckHypothesis", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.72f, 0.86f);
            rect.anchorMax = new Vector2(0.94f, 0.95f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.25f, 0.36f, 0.49f, 0.92f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var text = CreateText("Label", go.transform, 16, FontStyles.Bold);
            text.text = "Проверить";
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }
    }
}
