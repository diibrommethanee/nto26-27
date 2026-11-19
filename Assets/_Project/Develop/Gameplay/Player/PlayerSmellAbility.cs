using _Project.Develop.Gameplay.Input;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Gameplay.Player
{
    public class SmellAbility : MonoBehaviour
    {
        public static bool IsActive { get; private set; }

        private InputController _input;
        private bool? _lastActiveState;

        [Inject]
        private void Construct(InputController input)
        {
            _input = input;
        }

        private void Update()
        {
            var isActive = _input.ActiveSkill == SkillType.Smell;

            if (_lastActiveState == isActive)
                return;

            _lastActiveState = isActive;
            IsActive = isActive;

            foreach (var trail in FindObjectsOfType<ScentTrail>(true))
                trail.SetVisible(isActive);
        }
    }
}
