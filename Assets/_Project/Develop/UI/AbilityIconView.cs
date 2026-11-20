using System;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Player.Abilities;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.Develop.UI
{
    [RequireComponent(typeof(Image))]
    public class AbilityIconView : MonoBehaviour
    {
        [SerializeField] private Sprite smellSprite;
        [SerializeField] private Sprite hearingSprite;

        private Image _image;
        private IBufferedSubscriber<SelectedSkillChanged> _selectedSkillSubscriber;
        private IDisposable _subscription;

        [Inject]
        private void Construct(IBufferedSubscriber<SelectedSkillChanged> selectedSkillSubscriber)
        {
            _selectedSkillSubscriber = selectedSkillSubscriber;
        }

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void Start()
        {
            _subscription = _selectedSkillSubscriber.Subscribe(OnSelectedSkillChanged);
        }

        private void OnSelectedSkillChanged(SelectedSkillChanged message)
        {
            _image.sprite = message.Skill switch
            {
                SkillType.Smell => smellSprite,
                SkillType.Hearing => hearingSprite,
                _ => _image.sprite
            };
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
        }
    }
}
