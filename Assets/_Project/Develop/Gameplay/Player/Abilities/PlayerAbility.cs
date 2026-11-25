using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.Input;
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
        private UnityCamera _camera;

        private PlayerSmellAbility _smell;
        private PlayerHearingAbility _hearing;
        private IBufferedPublisher<SelectedSkillChanged> _selectedSkillPublisher;
        private bool _subscribed;

        public SkillType SelectedSkill { get; private set; } = SkillType.Hearing;
        public SkillType? ActiveSkill { get; private set; }

        [Inject]
        private void Construct(InputController input,
            IBufferedPublisher<SmellVisibilityChanged> visibility,
            IBufferedPublisher<HearingChanged> hearing,
            IBufferedPublisher<SelectedSkillChanged> selectedSkillPublisher)
        {
            _input = input;
            _hearing = new PlayerHearingAbility(hearing);
            _smell = new PlayerSmellAbility(visibility);
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
            SelectedSkill = SelectedSkill == SkillType.Smell ? SkillType.Hearing : SkillType.Smell;
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
            if (isActiveAndEnabled && !IsPointerOverCollectableClue())
                SetActiveSkill(ActiveSkill == SelectedSkill ? null : SelectedSkill);
        }

        public void StopHearing()
        {
            if (ActiveSkill == SkillType.Hearing) SetActiveSkill(null);
        }

        private void SetActiveSkill(SkillType? skill)
        {
            if (ActiveSkill == skill) return;

            var wasSmelling = ActiveSkill == SkillType.Smell;
            var wasHearing = ActiveSkill == SkillType.Hearing;
            ActiveSkill = skill;
            var isSmelling = skill == SkillType.Smell;
            if (wasSmelling != isSmelling)
                _smell.SetActive(isSmelling);
            bool isHearing = skill == SkillType.Hearing;
            if (wasHearing != isHearing)
            {
                _input.SetHearingActive(isHearing);
                _hearing.SetActive(isHearing);
            }
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
                if (clue != null && !clue.IsCollected && clue.CanCollect(ActiveSkill))
                    return true;
            }

            return false;
        }
    }
}
