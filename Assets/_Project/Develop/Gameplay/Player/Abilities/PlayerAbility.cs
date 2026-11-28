using System.Collections.Generic;
using System.Linq;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Timeline;
using MessagePipe;
using Cysharp.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using UnityCamera = UnityEngine.Camera;

namespace _Project.Develop.Gameplay.Player.Abilities
{
    public class PlayerAbility : MonoBehaviour
    {
        private InputController _input;
        private DayService _dayService;
        private UnityCamera _camera;

        private IPlayerAbility[] _abilities;
        private int _selectedIndex;
        private IPlayerAbility _active;
        private IBufferedPublisher<SelectedSkillChanged> _selectedSkillPublisher;
        private bool _subscribed;

        public SkillType SelectedSkill { get; private set; } = SkillType.Hearing;
        public SkillType? ActiveSkill => _active?.Skill;

        [Inject]
        private void Construct(InputController input,
            IReadOnlyList<IPlayerAbility> abilities,
            IBufferedPublisher<SelectedSkillChanged> selectedSkillPublisher,
            DayService dayService)
        {
            _input = input;
            _dayService = dayService;
            _abilities = abilities.ToArray();
            _selectedIndex = System.Array.FindIndex(_abilities, ability => ability.Skill == SelectedSkill);
            if (_selectedIndex < 0)
                throw new System.InvalidOperationException("The initial hearing ability must be registered.");
            _selectedSkillPublisher = selectedSkillPublisher;
            if (isActiveAndEnabled) Subscribe();
        }

        private void Start()
        {
            _camera = UnityCamera.main;
            PublishSelectedSkill();
        }

        private void OnEnable()
        {
            if (_input != null) Subscribe();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _input.Actions.Player.SwitchSkill.performed += OnSwitchSkill;
            _input.Actions.Player.UseSkill.performed += OnUseSkill;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed) return;
            _input.Actions.Player.SwitchSkill.performed -= OnSwitchSkill;
            _input.Actions.Player.UseSkill.performed -= OnUseSkill;
            _subscribed = false;
            SetActiveSkill(null);
        }

        private void OnSwitchSkill(InputAction.CallbackContext _)
        {
            SetActiveSkill(null);
            _selectedIndex = (_selectedIndex + 1) % _abilities.Length;
            SelectedSkill = _abilities[_selectedIndex].Skill;
            PublishSelectedSkill();
        }

        private void PublishSelectedSkill() =>
            _selectedSkillPublisher.Publish(new SelectedSkillChanged(SelectedSkill, ActiveSkill == SelectedSkill));

        private void OnUseSkill(InputAction.CallbackContext _)
        {
            UseSkillAsync().Forget();
        }

        private async UniTask UseSkillAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.LastUpdate, destroyCancellationToken);
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (!isActiveAndEnabled || IsPointerOverCollectableClue()) return;

            var activating = ActiveSkill != SelectedSkill;
            if (activating && !_dayService.TrySpendMinutes(_dayService.AbilityCostMinutes)) return;

            SetActiveSkill(activating ? _abilities[_selectedIndex] : null);
        }

        public void StopHearing()
        {
            if (ActiveSkill == SkillType.Hearing) SetActiveSkill(null);
        }

        private void SetActiveSkill(IPlayerAbility ability)
        {
            if (_active == ability) return;

            var previous = _active;
            _active = ability;
            previous?.SetActive(false);
            ability?.SetActive(true);
            PublishSelectedSkill();
        }

        private bool IsPointerOverCollectableClue()
        {
            var mouse = _input.Actions.Player.MousePosition.ReadValue<Vector2>();
            var ray = _camera.ScreenPointToRay(mouse);
            foreach (var hit in Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity))
            {
                if (!hit.collider) continue;
                var clue = hit.collider.GetComponentInParent<IWorldClue>();
                if (clue != null && clue.CanCollect(ActiveSkill))
                    return true;
            }

            return false;
        }
    }
}
