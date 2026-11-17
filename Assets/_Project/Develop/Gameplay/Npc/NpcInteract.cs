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
            
            npcData.state = new NpcState
            {
                mood = npcData.initialMood,
                dialogue = npcData.dialogue
            };
        }
        
        public void Interact()
        {
            if (_dialogueRunner.IsRunning)
                return;

            var dialogue = npcData.state.dialogue;
            if (dialogue != null)
                _dialogueRunner.Start(dialogue, npcData);
            
            _interactPublisher.Publish(new NpcInteractedEvent(npcData));
        }

    }
}
