using System.Threading.Tasks;
using _Project.Develop.Data;
using _Project.Develop.Gameplay.Npc;

namespace _Project.Develop.Gameplay.Timeline
{
    public class NextDayTransition
    {
        private readonly DayService _day;
        private readonly ICheckpointService _checkpoints;

        private readonly NpcStates _npcs;

        public NextDayTransition(DayService day, ICheckpointService checkpoints, NpcStates npcs)
        {
            _npcs = npcs;
            _day = day;
            _checkpoints = checkpoints;
        }

        public async Task<bool> StartAsync()
        {
            if (!_day.TryStartNextDay()) return false;
            _npcs.StartNewDay();
            await _checkpoints.SaveAsync();
            return true;
        }
    }
}
