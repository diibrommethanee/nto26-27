using System.Threading.Tasks;
using _Project.Develop.Data;

namespace _Project.Develop.Gameplay.Timeline
{
    public class NextDayTransition
    {
        private readonly DayService _day;
        private readonly ICheckpointService _checkpoints;

        public NextDayTransition(DayService day, ICheckpointService checkpoints)
        {
            _day = day;
            _checkpoints = checkpoints;
        }

        public async Task<bool> StartAsync()
        {
            if (!_day.TryStartNextDay()) return false;
            await _checkpoints.SaveAsync();
            return true;
        }
    }
}
