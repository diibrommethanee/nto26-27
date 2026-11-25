using _Project.Develop.Gameplay.Timeline;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Gameplay.Player
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class BedDayEndInteract : MonoBehaviour, IInteractable
    {
        private DayService _dayService;

        [Inject]
        private void Construct(DayService dayService)
        {
            _dayService = dayService;
        }

        public void Interact() => _dayService.TryEndDay();
    }
}