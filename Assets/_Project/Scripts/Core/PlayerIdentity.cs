using UnityEngine;

namespace CrossDrive.Core
{
    public sealed class PlayerIdentity
    {
        public PlayerIdentity(int index, string displayName, string carColorName, Color carColor)
        {
            Index = index;
            DisplayName = displayName;
            CarColorName = carColorName;
            CarColor = carColor;
        }

        public int Index { get; }
        public string DisplayName { get; }
        public string CarColorName { get; }
        public Color CarColor { get; }
    }
}
