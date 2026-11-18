using _Project.Develop.Data.Questing.Quests;
using _Project.Develop.Gameplay.Quests;
using MessagePipe;
using NaughtyAttributes;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Gameplay
{
    public class TestQuestSystem : MonoBehaviour
    {
        [SerializeField] private Quest quest;
        
        private IPublisher<StartQuestEvent>  _publisher;
        
        [Inject]
        public void Construct(IPublisher<StartQuestEvent> publisher)
        {
            _publisher = publisher;
        }

        [Button]
        public void CreateNewQuest()
        {
            _publisher.Publish(new StartQuestEvent(quest));
        }
    }
}
