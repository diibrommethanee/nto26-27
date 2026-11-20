using System;
using System.Collections.Generic;
using _Project.Develop.Data.Npc;
using _Project.Develop.Gameplay.NPC;
using _Project.Develop.Gameplay.Player;
using _Project.Develop.UI.Surface;
using TMPro;
using TMPEffects.Components;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Develop.UI.Dialogue
{
    public class DialogueBubbles : MonoBehaviour
    {
        [SerializeField] private SurfaceStyle style;
        [SerializeField] private TMP_Text template;
        [SerializeField, Min(1)] private int visibleLines = 4;

        private readonly List<Bubble> _bubbles = new();
        private RectTransform _layer;
        private Canvas _canvas;
        private Camera _camera;
        private NpcInteract[] _speakers;
        private Transform _player;

        public TMP_Text CurrentLabel => CurrentWriter ? CurrentWriter.GetComponent<TMP_Text>() : null;

        public TMPWriter CurrentWriter => _bubbles.Count == 0 ? null : _bubbles[^1].Writer;

        private void Awake()
        {
            _layer = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>().rootCanvas;
            _camera = Camera.main;
            _speakers = FindObjectsByType<NpcInteract>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var player = FindFirstObjectByType<PlayerController>();
            if (player) _player = player.transform;
        }

        public void Show(NpcData speaker, NpcData interlocutor, string text, Action advance)
        {
            if (_bubbles.Count > 0)
            {
                var previous = _bubbles[^1];
                if (previous.Writer.IsWriting) previous.Writer.SkipWriter();
                previous.Opacity.alpha = 0.82f;
                previous.Opacity.blocksRaycasts = false;
            }

            if (_bubbles.Count >= visibleLines)
            {
                Destroy(_bubbles[0].Rect.gameObject);
                _bubbles.RemoveAt(0);
            }

            var go = new GameObject("Dialogue bubble", typeof(RectTransform), typeof(CanvasGroup));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_layer, false);
            rect.pivot = new Vector2(0.5f, 0);
            var background = go.AddComponent<SurfaceGraphic>();
            background.style = style;
            var button = go.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => advance());

            var label = Instantiate(template, rect);
            label.gameObject.SetActive(true);
            label.enabled = true;
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.Center;
            label.margin = Vector4.zero;
            label.fontSize = 25;
            var size = label.GetPreferredValues(text, 440, 0);
            rect.sizeDelta = new Vector2(Mathf.Clamp(size.x + 44, 130, 484), size.y + 28);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(20, 10);
            label.rectTransform.offsetMax = new Vector2(-20, -10);

            var writer = label.GetComponent<TMPWriter>();
            var anchor = ResolveAnchor(speaker, interlocutor);
            _bubbles.Add(new Bubble(rect, go.GetComponent<CanvasGroup>(), writer, anchor,
                anchor ? anchor.GetComponentsInChildren<SpriteRenderer>() : Array.Empty<SpriteRenderer>()));
            writer.SetText(text);
            writer.StartWriter();
            PositionBubbles();
        }

        private Transform ResolveAnchor(NpcData speaker, NpcData interlocutor)
        {
            var target = speaker ? speaker : interlocutor;
            if (target)
                foreach (var npc in _speakers)
                    if (npc && npc.Data == target)
                        return npc.transform;

            return _player;
        }

        private void LateUpdate() => PositionBubbles();

        private void PositionBubbles()
        {
            if (_bubbles.Count == 0) return;
            var bounds = _layer.rect;
            var eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            for (var i = _bubbles.Count - 1; i >= 0; i--)
            {
                var bubble = _bubbles[i];
                var position = new Vector2(bounds.center.x, bounds.center.y + 70);
                if (_camera && bubble.Anchor)
                {
                    var world = bubble.Anchor.position;
                    foreach (var sprite in bubble.Sprites)
                        if (sprite && sprite.enabled && sprite.gameObject.activeInHierarchy)
                            world.y = Mathf.Max(world.y, sprite.bounds.max.y);
                    var screen = _camera.WorldToScreenPoint(world);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, screen, eventCamera, out position);
                    position.y += 14;
                }

                var halfWidth = bubble.Rect.rect.width / 2;
                position.x = Mathf.Clamp(position.x, bounds.xMin + halfWidth + 12, bounds.xMax - halfWidth - 12);
                position.y = Mathf.Clamp(position.y, bounds.yMin + 100, bounds.yMax - bubble.Rect.rect.height - 12);
                for (var j = _bubbles.Count - 1; j > i; j--)
                {
                    var newer = _bubbles[j].Rect;
                    var newerPosition = newer.anchoredPosition;
                    if (Mathf.Abs(position.x - newerPosition.x) < halfWidth + newer.rect.width / 2 + 10 &&
                        position.y < newerPosition.y + newer.rect.height + 8 &&
                        position.y + bubble.Rect.rect.height > newerPosition.y - 8)
                        position.y = newerPosition.y + newer.rect.height + 8;
                }

                bubble.Rect.anchoredPosition = position;
                bubble.Rect.gameObject.SetActive(position.y + bubble.Rect.rect.height <= bounds.yMax);
            }
        }

        public void Clear()
        {
            foreach (var bubble in _bubbles)
            {
                bubble.Rect.gameObject.SetActive(false);
                Destroy(bubble.Rect.gameObject);
            }
            _bubbles.Clear();
        }

        private class Bubble
        {
            public readonly RectTransform Rect;
            public readonly CanvasGroup Opacity;
            public readonly TMPWriter Writer;
            public readonly Transform Anchor;
            public readonly SpriteRenderer[] Sprites;

            public Bubble(RectTransform rect, CanvasGroup opacity, TMPWriter writer, Transform anchor,
                SpriteRenderer[] sprites)
            {
                Rect = rect;
                Opacity = opacity;
                Writer = writer;
                Anchor = anchor;
                Sprites = sprites;
            }
        }
    }
}
