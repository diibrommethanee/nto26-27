using _Project.Develop.Data.Npc;
using _Project.Develop.Gameplay.Dialogue;
using _Project.Develop.Gameplay.Npc;
using _Project.Develop.Gameplay.Player;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Gameplay.NPC
{
    public class NpcInteract : MonoBehaviour, IInteractable
    {
        [SerializeField] private NpcData npcData;
        
        private DialogueRunner _dialogueRunner;
        private IPublisher<NpcInteractedEvent> _interactPublisher;

        [Inject]
        public void Construct(IPublisher<NpcInteractedEvent> interactPublisher, DialogueRunner dialogueRunner)
        {
            _interactPublisher = interactPublisher;
            _dialogueRunner = dialogueRunner;
        }

        
        public void Interact()
        {
            if (npcData == null)
            {
                Debug.LogError("NpcInteract: assign NPC data.", this);
                return;
            }

            if (_dialogueRunner.IsRunning)
                return;

            if (npcData.dialogue != null)
                _dialogueRunner.Start(npcData.dialogue);
            
            _interactPublisher.Publish(new NpcInteractedEvent(npcData));
            print("Npc interacted");
        }
    }
}
