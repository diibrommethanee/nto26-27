using _Project.Develop.Data.Evidence;
using _Project.Develop.Data.Player;
using _Project.Develop.Data.Questing;
using _Project.Develop.Data.Train;
using _Project.Develop.Gameplay.Dialogue;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Player;
using _Project.Develop.Gameplay.Npc;
using _Project.Develop.Gameplay.Player.Abilities;
using _Project.Develop.Gameplay.Timeline;
using _Project.Develop.Gameplay.Train;
using _Project.Develop.Gameplay.Quests;
using _Project.Develop.Infrastructure.EntryPoints;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Infrastructure.DI
{
    public class GameScope : LifetimeScope
    {
        [SerializeField, Space(5)] private TrainSettings trainSettings;
        [SerializeField] private QuestDatabase questDatabase;
        [SerializeField] private EvidenceDatabase evidenceDatabase;
        [SerializeField] private PlayerStats playerStats;
        
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<InputController>().AsSelf();
            
            var options = new MessagePipeOptions();
            
            builder.RegisterMessageBroker<ForceDayPhase>(options);
            builder.RegisterMessageBroker<DayEndingCutsceneFinished>(options);
            builder.RegisterMessageBroker<DayPhaseChanged>(options);
            builder.RegisterEntryPoint<DayService>().AsSelf();

            builder.RegisterMessageBroker<ZoomModeChanged>(options);
            builder.RegisterMessageBroker<SmellVisibilityChanged>(options);
            builder.RegisterMessageBroker<HearingChanged>(options);
            builder.RegisterMessageBroker<SelectedSkillChanged>(options);
            builder.RegisterMessageBroker<DialogueShown>(options);
            builder.RegisterMessageBroker<DialogueEnded>(options);
            builder.RegisterMessageBroker<DialogueChoicesShown>(options);

            builder.Register<DialogueNodeHandler>(Lifetime.Scoped).As<IDialogueNodeHandler>();
            builder.Register<EndNodeHandler>(Lifetime.Scoped).As<IDialogueNodeHandler>();
            builder.Register<ChoiceNodeHandler>(Lifetime.Scoped).As<IDialogueNodeHandler>();
            builder.Register<DialogueRunner>(Lifetime.Scoped);
            
            builder.RegisterMessageBroker<NpcInteractedEvent>(options);
            builder.RegisterMessageBroker<NpcTalkedEvent>(options);
            builder.RegisterMessageBroker<QuestCompletedEvent>(options);
            builder.RegisterMessageBroker<QuestStepCompletedEvent>(options);
            builder.RegisterMessageBroker<StartQuestEvent>(options);
            
            builder.RegisterInstance(questDatabase);
            builder.Register<IQuestRegistry, QuestRegistry>(Lifetime.Singleton);
            
            builder.RegisterEntryPoint<QuestService>();
            
            builder.RegisterMessageBroker<TrainStopped>(options);
            builder.RegisterMessageBroker<TrainDeparted>(options);
            
            builder.RegisterInstance(trainSettings);
            builder.RegisterEntryPoint<TrainMovement>().AsSelf();
            
            builder.RegisterInstance(playerStats);
            builder.RegisterInstance(evidenceDatabase);
            builder.Register<EvidenceService>(Lifetime.Scoped);
            
            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
