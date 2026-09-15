using CrossDrive.Gameplay;
using CrossDrive.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrossDrive.Core
{
    public static class CrossDriveBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartPrototype()
        {
            if (Object.FindFirstObjectByType<CrossDriveGame>() != null ||
                Object.FindFirstObjectByType<NetworkBootstrap>() != null)
            {
                return;
            }

            bool multiplayerScene = SceneManager.GetActiveScene().name == "Phase2Multiplayer";
            GameObject root = new GameObject(multiplayerScene ? "CrossDrive Phase 2" : "CrossDrive Phase 1");
            Object.DontDestroyOnLoad(root);
            if (multiplayerScene)
            {
                root.AddComponent<NetworkBootstrap>();
            }
            else
            {
                root.AddComponent<CrossDriveGame>();
            }
        }
    }
}
