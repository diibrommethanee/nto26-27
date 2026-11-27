using System;
using _Project.Develop.Gameplay.Input;
using MessagePipe;

namespace _Project.Develop.UI
{
    internal class HudVisibilitySubscription : IDisposable
    {
        private readonly IDisposable _zoom;
        private readonly IDisposable _saving;

        public HudVisibilitySubscription(ISubscriber<ZoomModeChanged> zoom,
            IBufferedSubscriber<SavingVisibilityChanged> saving, bool zoomActive, Action<bool, bool> setHidden)
        {
            bool savingActive = false;
            bool hidden = zoomActive;

            void Refresh(bool saveTransition)
            {
                bool next = zoomActive || savingActive;
                if (next == hidden && !(saveTransition && savingActive)) return;
                hidden = next;
                setHidden(next, saveTransition);
            }

            _zoom = zoom.Subscribe(message => { zoomActive = message.IsActive; Refresh(false); });
            _saving = saving.Subscribe(message => { savingActive = message.IsActive; Refresh(true); });
        }

        public void Dispose()
        {
            _zoom.Dispose();
            _saving.Dispose();
        }
    }
}
