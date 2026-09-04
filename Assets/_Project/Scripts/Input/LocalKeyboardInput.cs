using UnityEngine;

namespace CrossDrive.Input
{
    public sealed class LocalKeyboardInput
    {
        private readonly LocalControlScheme[] schemes =
        {
            new LocalControlScheme(KeyCode.A, KeyCode.D, KeyCode.W),
            new LocalControlScheme(KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow),
            new LocalControlScheme(KeyCode.J, KeyCode.L, KeyCode.I),
            new LocalControlScheme(KeyCode.C, KeyCode.V, KeyCode.F),
        };

        public int PlayerCount => schemes.Length;

        public LocalControlScheme GetScheme(int playerIndex) => schemes[playerIndex];

        public float ReadSteering(int playerIndex)
        {
            LocalControlScheme scheme = schemes[playerIndex];
            float steering = 0f;

            if (UnityEngine.Input.GetKey(scheme.Left))
            {
                steering += 1f;
            }

            if (UnityEngine.Input.GetKey(scheme.Right))
            {
                steering -= 1f;
            }

            return steering;
        }

        public bool ReadBoostDown(int playerIndex)
        {
            return UnityEngine.Input.GetKeyDown(schemes[playerIndex].Boost);
        }
    }
}
