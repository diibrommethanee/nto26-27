using _Project.Develop.Data.Npc;
using _Project.Develop.Gameplay.Dialogue;
using _Project.Develop.Gameplay.Npc;
using _Project.Develop.Gameplay.Player;
using _Project.Develop.Gameplay.Timeline;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Gameplay.NPC
{
    public class NpcInteract : MonoBehaviour, IInteractable
    {
        [SerializeField] private NpcData npcData;
        
        [SerializeField] private Transform dialogueAnchor;

        public NpcData Data => npcData;
        public Transform DialogueAnchor => dialogueAnchor;

        private DialogueRunner _dialogueRunner;
        private DayService _dayService;
        private IPublisher<NpcInteractedEvent> _interactPublisher;

        [Inject]
        public void Construct(IPublisher<NpcInteractedEvent> interactPublisher, DialogueRunner dialogueRunner,
            DayService dayService)
        {
            _interactPublisher = interactPublisher;
            _dialogueRunner = dialogueRunner;
            _dayService = dayService;
            
            npcData.state = new NpcState
            {
                mood = npcData.initialMood,
                dialogue = npcData.dialogue
            };
        }
        
        internal void EndDialogueOnExit()
        {
            if (_dialogueRunner.Interlocutor == npcData)
                _dialogueRunner.Stop();
        }

        public void Interact()
        {
            if (_dialogueRunner.IsRunning)
                return;

            var dialogue = npcData.state.dialogue;
            if (dialogue != null && !_dayService.TrySpendMinutes(_dayService.DialogueCostMinutes))
                return;

            _interactPublisher.Publish(new NpcInteractedEvent(npcData, dialogueAnchor));

            if (dialogue != null)
                _dialogueRunner.Start(dialogue, npcData);
        }

    }
}
