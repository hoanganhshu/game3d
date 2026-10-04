using System.Collections.Generic;
using UnityEngine;

namespace FPSGame
{
    /// <summary>Something that reacts to the Android Back button (Escape in the Editor).</summary>
    public interface IBackHandler
    {
        /// <returns>true if the press was consumed.</returns>
        bool OnBack();
    }

    /// <summary>
    /// Stack of back handlers. Screens register when enabled; popups opened later sit on top,
    /// so Back closes the newest popup first, then falls through to the screen handler
    /// (pause in game, quit in the main menu, return in level select).
    /// </summary>
    public static class BackButtonManager
    {
        private static readonly List<IBackHandler> Handlers = new List<IBackHandler>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Handlers.Clear();
        }

        public static void Push(IBackHandler handler)
        {
            if (handler == null) return;
            Handlers.Remove(handler);
            Handlers.Add(handler);
        }

        public static void Remove(IBackHandler handler)
        {
            Handlers.Remove(handler);
        }

        public static void HandleBack()
        {
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsLoading) return;

            for (int i = Handlers.Count - 1; i >= 0; i--)
            {
                IBackHandler handler = Handlers[i];
                if (handler == null || (handler is Object unityObject && unityObject == null))
                {
                    Handlers.RemoveAt(i);
                    continue;
                }
                if (handler.OnBack()) return;
            }
        }
    }
}
