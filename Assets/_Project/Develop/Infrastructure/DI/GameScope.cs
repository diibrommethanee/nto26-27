using System.Linq;
using _Project.Develop.Data;
using _Project.Develop.UI;
using _Project.Develop.Data.Evidence;
using _Project.Develop.Data.Player;
using _Project.Develop.Data.Questing;
using _Project.Develop.Data.Train;
using _Project.Develop.Data.Tea;
using _Project.Develop.Data.Timeline;
using _Project.Develop.Gameplay.Dialogue;
using _Project.Develop.Gameplay.Evidence;
using _Project.Develop.Gameplay.Input;
using _Project.Develop.Gameplay.Npc;
using _Project.Develop.Gameplay.Player.Abilities;
using _Project.Develop.Gameplay.Timeline;
using _Project.Develop.Gameplay.Train;
using _Project.Develop.Gameplay.Quests;
using _Project.Develop.Gameplay.Tea;
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
        [SerializeField] private TeaRecipeBook npcTeaRecipes;
        [SerializeField] private DaySettings daySettings;
        [SerializeField] private QuestDatabase questDatabase;
        [SerializeField] private EvidenceDatabase evidenceDatabase;
        [SerializeField] private PlayerStats playerStats;
        
        protected override void Configure(IContainerBuilder builder)
        {
            var sceneRoots = gameObject.scene.GetRootGameObjects();
            builder.RegisterBuildCallback(container =>
            {
                foreach (var root in sceneRoots)
                    container.InjectGameObject(root);
            });

            var options = new MessagePipeOptions();
            RegisterPlayer(builder, options);
            RegisterDayAndTea(builder, options);
            RegisterDialogue(builder, options);
            RegisterInvestigation(builder, options);
            RegisterTrain(builder, options);
            RegisterCheckpoints(builder, sceneRoots);
        }

        private void RegisterDayAndTea(IContainerBuilder builder, MessagePipeOptions options)
        {
            builder.Register<NpcStates>(Lifetime.Scoped).AsSelf().As<ISavable>();
            builder.RegisterInstance(daySettings);
            builder.RegisterInstance(npcTeaRecipes);
            builder.Register<NpcTeaService>(Lifetime.Scoped);
            builder.Register<NextDayBuffService>(Lifetime.Scoped).AsSelf().As<ISavable>();
            builder.RegisterMessageBroker<ForceDayPhase>(options);
            builder.RegisterMessageBroker<DayEndingCutsceneFinished>(options);
            builder.RegisterMessageBroker<DayPhaseChanged>(options);
            builder.RegisterEntryPoint<DayService>().AsSelf();
            builder.Register<NextDayTransition>(Lifetime.Scoped);
        }

        private void RegisterPlayer(IContainerBuilder builder, MessagePipeOptions options)
        {
            builder.RegisterInstance(playerStats);
            builder.RegisterEntryPoint<InputController>().AsSelf();
            builder.RegisterMessageBroker<ZoomModeChanged>(options);
            builder.RegisterMessageBroker<SavingVisibilityChanged>(options);
            builder.RegisterMessageBroker<SmellVisibilityChanged>(options);
            builder.RegisterMessageBroker<HearingChanged>(options);
            builder.RegisterMessageBroker<SelectedSkillChanged>(options);
            builder.Register<PlayerHearingAbility>(Lifetime.Scoped).As<IPlayerAbility>();
            builder.Register<PlayerSmellAbility>(Lifetime.Scoped).As<IPlayerAbility>();
        }

        private static void RegisterDialogue(IContainerBuilder builder, MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<DialogueShown>(options);
            builder.RegisterMessageBroker<DialogueEnded>(options);
            builder.RegisterMessageBroker<DialogueChoicesShown>(options);
            builder.Register<DialogueNodeHandler>(Lifetime.Scoped).As<IDialogueNodeHandler>();
            builder.Register<EndNodeHandler>(Lifetime.Scoped).As<IDialogueNodeHandler>();
            builder.Register<ChoiceNodeHandler>(Lifetime.Scoped).As<IDialogueNodeHandler>();
            builder.Register<DialogueRunner>(Lifetime.Scoped);
            builder.RegisterMessageBroker<NpcInteractedEvent>(options);
            builder.RegisterMessageBroker<NpcTalkedEvent>(options);
        }

        private void RegisterInvestigation(IContainerBuilder builder, MessagePipeOptions options)
        {
            builder.Register<QuestRewards>(Lifetime.Scoped);
            builder.RegisterMessageBroker<QuestCompletedEvent>(options);
            builder.RegisterMessageBroker<QuestStepCompletedEvent>(options);
            builder.RegisterMessageBroker<StartQuestEvent>(options);
            builder.RegisterInstance(questDatabase);
            builder.Register<IQuestRegistry, QuestRegistry>(Lifetime.Singleton);
            builder.RegisterEntryPoint<QuestService>().AsSelf();
            builder.RegisterInstance(evidenceDatabase);
            builder.Register<EvidenceService>(Lifetime.Scoped).AsSelf().As<ISavable>();
        }

        private void RegisterTrain(IContainerBuilder builder, MessagePipeOptions options)
        {
            builder.RegisterMessageBroker<TrainStopped>(options);
            builder.RegisterMessageBroker<TrainDeparted>(options);
            builder.RegisterInstance(trainSettings);
            builder.RegisterEntryPoint<TrainMovement>().AsSelf();
        }

        private static void RegisterCheckpoints(IContainerBuilder builder, GameObject[] sceneRoots)
        {
            builder.Register<CheckpointService>(Lifetime.Scoped).AsSelf().As<ICheckpointService>();
            var indicator = sceneRoots.SelectMany(root => root.GetComponentsInChildren<CheckpointIndicator>(true)).Single();
            builder.RegisterInstance(indicator);
            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
