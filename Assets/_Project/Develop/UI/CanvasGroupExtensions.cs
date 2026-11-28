using UnityEngine;

namespace _Project.Develop.UI
{
    internal static class CanvasGroupExtensions
    {
        public static void SetInteractive(this CanvasGroup group, bool interactive)
        {
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }
    }
}
