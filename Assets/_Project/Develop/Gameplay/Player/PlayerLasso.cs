using System;
using System.Collections.Generic;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Player.Abilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Player
{
    internal sealed class PlayerLasso : IDisposable
    {
        private const float Radius = 3f;
        private const float CloseDistance = 1.25f;
        private const float EdgeTolerance = 0.9f;
        private const float StartMovePixels = 12f;

        private readonly InputController _input;
        private readonly EvidenceService _evidence;
        private readonly UnityCamera _camera;
        private readonly PlayerAbility _ability;
        private readonly Action _onClick;
        private readonly List<Vector2> _points = new();
        private readonly LineRenderer _line;
        private readonly Material _material;
        private Vector2 _startPosition;
        private float _pressedAt;
        private bool _held;
        private bool _drawing;

        public PlayerLasso(InputController input, EvidenceService evidence, UnityCamera camera,
            PlayerAbility ability, Action onClick, GameObject owner)
        {
            _input = input;
            _evidence = evidence;
            _camera = camera;
            _ability = ability;
            _onClick = onClick;

            _line = owner.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.startWidth = _line.endWidth = 0.04f;
            _line.sortingOrder = 1000;
            _material = new Material(Shader.Find("Sprites/Default")) { color = Color.yellow };
            _line.material = _material;
            _line.startColor = _line.endColor = Color.yellow;
            _line.enabled = false;

            _input.Actions.Camera.Mode.started += OnPressed;
            _input.Actions.Camera.Mode.canceled += OnReleased;
            _input.Actions.Player.MousePosition.performed += OnMouseMoved;
        }

        private void OnPressed(InputAction.CallbackContext _)
        {
            _held = true;
            _drawing = false;
            _points.Clear();
            _pressedAt = Time.unscaledTime;
            if (_ability.ActiveSkill != null)
                return;

            _startPosition = _input.Actions.Player.MousePosition.ReadValue<Vector2>();
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            _points.Add(ToWorld(_startPosition));
            _line.enabled = true;
            DrawLine();
        }

        private void OnMouseMoved(InputAction.CallbackContext context)
        {
            if (!_held || _points.Count == 0)
                return;

            var position = context.ReadValue<Vector2>();
            if (!_drawing && Vector2.Distance(_startPosition, position) < StartMovePixels)
                return;

            _drawing = true;
            _points.Add(ToWorld(position));
            DrawLine();
        }

        private void OnReleased(InputAction.CallbackContext _)
        {
            if (!_held)
                return;

            _held = false;
            if (_drawing)
                CollectEnclosedClue();
            else if (Time.unscaledTime - _pressedAt < 0.2f)
                _onClick?.Invoke();

            _points.Clear();
            _line.positionCount = 0;
            _line.enabled = false;
        }

        private void CollectEnclosedClue()
        {
            if (_points.Count < 3 || Vector2.Distance(_points[0], _points[_points.Count - 1]) > CloseDistance)
                return;

            var min = _points[0];
            var max = min;
            foreach (var point in _points)
            {
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            foreach (var hit in Physics2D.OverlapAreaAll(min, max))
            {
                var clue = hit != null ? hit.GetComponentInParent<WorldClue>() : null;
                if (clue == null || clue.IsCollected || clue.RequiredSkill != ClueSkillRequirement.None)
                    continue;

                var position = (Vector2)clue.transform.position;
                if (!IsWithinRadius(position) || !Contains(position))
                    continue;

                clue.Collect(_evidence);
                return;
            }
        }

        private bool Contains(Vector2 point)
        {
            var inside = false;
            for (int i = 0, j = _points.Count - 1; i < _points.Count; j = i++)
            {
                var a = _points[i];
                var b = _points[j];
                var edge = b - a;
                var t = edge.sqrMagnitude == 0f ? 0f : Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude);
                if ((point - (a + edge * t)).sqrMagnitude <= EdgeTolerance * EdgeTolerance)
                    return true;

                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }

            return inside;
        }

        private bool IsWithinRadius(Vector2 position)
        {
            foreach (var point in _points)
                if ((point - position).sqrMagnitude > Radius * Radius)
                    return false;

            return true;
        }

        private void DrawLine()
        {
            _line.positionCount = _points.Count;
            for (var i = 0; i < _points.Count; i++)
                _line.SetPosition(i, _points[i]);
        }

        private Vector2 ToWorld(Vector2 position)
        {
            return _camera.ScreenToWorldPoint(new Vector3(position.x, position.y, -_camera.transform.position.z));
        }

        public void Dispose()
        {
            _input.Actions.Camera.Mode.started -= OnPressed;
            _input.Actions.Camera.Mode.canceled -= OnReleased;
            _input.Actions.Player.MousePosition.performed -= OnMouseMoved;
            UnityEngine.Object.Destroy(_line);
            UnityEngine.Object.Destroy(_material);
        }
    }
}
