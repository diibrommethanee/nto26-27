using _Project.Develop.Gameplay.Npc;
using _Project.Develop.Gameplay.Player;
using _Project.Develop.Gameplay.Quests;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Gameplay.NPC
{
    public class NpcInteract : MonoBehaviour, IInteractable
    {
        [SerializeField] private Data.Npc.NpcData npcData;
        
        private IPublisher<NpcInteractedEvent> _interactPublisher;
        private IPublisher<StartQuestEvent> _questPublisher;

        [Inject]
        public void Construct(IPublisher<NpcInteractedEvent> publisher, IPublisher<StartQuestEvent> questPublisher)
        {
            _interactPublisher = publisher;
            _questPublisher = questPublisher;
        }
        
        public void Interact()
        {
            if (npcData.quest != null)
                _questPublisher.Publish(new StartQuestEvent(npcData.quest));
            
            _interactPublisher.Publish(new NpcInteractedEvent(npcData.id, npcData.displayName));
            print("Npc interacted");
        }
    }
}
