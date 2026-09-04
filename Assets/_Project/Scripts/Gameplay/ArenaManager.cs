using System.Collections.Generic;
using UnityEngine;

namespace CrossDrive.Gameplay
{
    public sealed class ArenaManager : MonoBehaviour
    {
        private readonly List<Rect> pits = new List<Rect>();
        private readonly Vector2[] spawnPoints =
        {
            new Vector2(-6.2f, 3.4f),
            new Vector2(6.2f, 3.4f),
            new Vector2(6.2f, -3.4f),
            new Vector2(-6.2f, -3.4f),
        };

        public Rect DriveableBounds { get; private set; }
        public IReadOnlyList<Rect> Pits => pits;

        public void Initialize()
        {
            DriveableBounds = new Rect(-8f, -5f, 16f, 10f);
            pits.Clear();
            pits.Add(new Rect(-4.2f, -1.4f, 2.2f, 2.8f));
            pits.Add(new Rect(2f, -1.4f, 2.2f, 2.8f));
            pits.Add(new Rect(-1f, -4.2f, 2f, 2.1f));
        }

        public bool IsDriveable(Vector2 position)
        {
            if (!DriveableBounds.Contains(position))
            {
                return false;
            }

            foreach (Rect pit in pits)
            {
                if (pit.Contains(position))
                {
                    return false;
                }
            }

            return true;
        }

        public Vector2 GetSpawnPoint(int ownerPlayerIndex) => spawnPoints[ownerPlayerIndex];

        public float GetSpawnRotation(int ownerPlayerIndex)
        {
            Vector2 towardCenter = -spawnPoints[ownerPlayerIndex];
            return Mathf.Atan2(-towardCenter.x, towardCenter.y) * Mathf.Rad2Deg;
        }
    }
}
