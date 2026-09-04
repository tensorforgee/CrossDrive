using UnityEngine;

namespace CrossDrive.Input
{
    public readonly struct LocalControlScheme
    {
        public LocalControlScheme(KeyCode left, KeyCode right, KeyCode boost)
        {
            Left = left;
            Right = right;
            Boost = boost;
        }

        public KeyCode Left { get; }
        public KeyCode Right { get; }
        public KeyCode Boost { get; }

        public string Summary => $"{Left}/{Right} steer, {Boost} boost";
    }
}
