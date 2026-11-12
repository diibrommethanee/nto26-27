using _Project.Develop.Gameplay.Events;
using _Project.Develop.Gameplay.Player;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Gameplay.NPC
{
    public class TestNPC : MonoBehaviour, IInteractable
    {
        public string Id;
        public string Name;
        private IPublisher<NpcInteractedEvent> _publisher;

        [Inject]
        public void Construct(IPublisher<NpcInteractedEvent> publisher)
        {
            _publisher = publisher;
        }
        
        public void Interact()
        {
            _publisher.Publish(new NpcInteractedEvent(this.Id, this.Name));
            Debug.Log("interacted");
        }
    }
}
