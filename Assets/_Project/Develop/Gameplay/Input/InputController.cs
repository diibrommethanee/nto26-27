using System;
using _Project.Develop.Gameplay.Evidence;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace _Project.Develop.Gameplay.Input
{
    public enum SkillType
    {
        Smell,
        Hearing
    }

    public class InputController : IStartable, ITickable, IDisposable
    {
        public PlayerInputActions Actions { get; } = new();
        private float _modePressedAt = float.PositiveInfinity;
        private UnityEngine.Camera _camera;

        public SkillType SelectedSkill { get; private set; } = SkillType.Smell;
        public SkillType? ActiveSkill { get; private set; }

        public void Start()
        {
            _camera = UnityEngine.Camera.main;
            Actions.Enable();
            Actions.Camera.Zoom.Disable();

            Actions.Camera.Mode.started += OnZoomPressed;
            Actions.Camera.Mode.canceled += OnZoomReleased;

            Actions.Player.SwitchSkill.performed += OnSwitchSkill;
            Actions.Player.UseSkill.performed += OnUseSkill;
        }

        private void OnZoomPressed(InputAction.CallbackContext _)
        {
            _modePressedAt = Time.unscaledTime;
            Actions.Player.CameraPan.Disable();
        }

        public void Tick()
        {
            if (!Actions.Camera.Mode.IsPressed())
            {
                if (!float.IsPositiveInfinity(_modePressedAt))
                    OnZoomReleased(default);
                return;
            }

            if (Time.unscaledTime - _modePressedAt < 0.2f || Actions.Camera.Zoom.enabled)
                return;

            Actions.Player.Disable();
            Actions.Camera.Zoom.Enable();
        }

        private void OnZoomReleased(InputAction.CallbackContext _)
        {
            _modePressedAt = float.PositiveInfinity;
            Actions.Camera.Zoom.Disable();
            Actions.Player.Enable();
        }

        private void OnSwitchSkill(InputAction.CallbackContext _)
        {
            SelectedSkill = SelectedSkill == SkillType.Smell
                ? SkillType.Hearing
                : SkillType.Smell;
        }

        private void OnUseSkill(InputAction.CallbackContext _)
        {
            if (IsPointerOverWorldClue())
                return;

            UseSelectedSkill();
        }

        private bool IsPointerOverWorldClue()
        {
            if (_camera == null)
                return false;

            var mousePosition = Actions.Player.MousePosition.ReadValue<Vector2>();
            var ray = _camera.ScreenPointToRay(mousePosition);
            var hits = Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity);

            foreach (var hit in hits)
            {
                if (hit.collider == null)
                    continue;

                var clue = hit.collider.GetComponentInParent<IWorldClue>();
                if (clue != null && !clue.IsCollected && clue.CanCollect(ActiveSkill))
                    return true;
            }

            return false;
        }

        private void UseSelectedSkill()
        {
            ActiveSkill = ActiveSkill == SelectedSkill
                ? null
                : SelectedSkill;
        }

        public void Dispose()
        {
            Actions.Camera.Mode.started -= OnZoomPressed;
            Actions.Camera.Mode.canceled -= OnZoomReleased;
            Actions.Player.SwitchSkill.performed -= OnSwitchSkill;
            Actions.Player.UseSkill.performed -= OnUseSkill;

            Actions.Disable();
            Actions.Dispose();
        }
    }
}
