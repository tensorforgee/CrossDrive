using CrossDrive.Gameplay;
using UnityEngine;

namespace CrossDrive.Core
{
    public static class CrossDriveBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartPrototype()
        {
            if (Object.FindFirstObjectByType<CrossDriveGame>() != null)
            {
                return;
            }

            GameObject root = new GameObject("CrossDrive Phase 1");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<CrossDriveGame>();
        }
    }
}
