using System;
using System.Collections.Generic;

namespace CrossDrive.Gameplay
{
    public sealed class ControlAssignment
    {
        private readonly int[] controlledCarOwnerByDriver;
        private readonly int[] driverByCarOwner;

        public ControlAssignment(IReadOnlyList<int> controlledCarOwners)
        {
            if (!ControlAssignmentService.IsValidCompleteCycle(controlledCarOwners))
            {
                throw new ArgumentException("Assignments must form one complete cycle.", "controlledCarOwners");
            }

            controlledCarOwnerByDriver = new int[controlledCarOwners.Count];
            driverByCarOwner = new int[controlledCarOwners.Count];

            for (int driver = 0; driver < controlledCarOwners.Count; driver++)
            {
                int owner = controlledCarOwners[driver];
                controlledCarOwnerByDriver[driver] = owner;
                driverByCarOwner[owner] = driver;
            }
        }

        public int PlayerCount { get { return controlledCarOwnerByDriver.Length; } }

        public int GetControlledCarOwner(int driverPlayerIndex)
        {
            return controlledCarOwnerByDriver[driverPlayerIndex];
        }

        public int GetDriverForCarOwner(int carOwnerPlayerIndex)
        {
            return driverByCarOwner[carOwnerPlayerIndex];
        }

        public int[] CopyDriverToCarOwnerMap()
        {
            return (int[])controlledCarOwnerByDriver.Clone();
        }
    }

    public sealed class ControlAssignmentService
    {
        public ControlAssignment Create(int playerCount, Random random = null)
        {
            if (playerCount < 3)
            {
                throw new ArgumentOutOfRangeException("playerCount", "A complete CrossDrive cycle requires at least three players.");
            }

            if (random == null)
            {
                random = new Random();
            }
            int[] cycleOrder = new int[playerCount];

            for (int index = 0; index < playerCount; index++)
            {
                cycleOrder[index] = index;
            }

            for (int index = playerCount - 1; index > 0; index--)
            {
                int swapIndex = random.Next(index + 1);
                int swappedPlayer = cycleOrder[index];
                cycleOrder[index] = cycleOrder[swapIndex];
                cycleOrder[swapIndex] = swappedPlayer;
            }

            int[] controlledCarOwnerByDriver = new int[playerCount];
            for (int cycleIndex = 0; cycleIndex < playerCount; cycleIndex++)
            {
                int driver = cycleOrder[cycleIndex];
                int controlledOwner = cycleOrder[(cycleIndex + 1) % playerCount];
                controlledCarOwnerByDriver[driver] = controlledOwner;
            }

            return new ControlAssignment(controlledCarOwnerByDriver);
        }

        public static bool IsValidCompleteCycle(IReadOnlyList<int> assignments)
        {
            if (assignments == null || assignments.Count < 3)
            {
                return false;
            }

            int count = assignments.Count;
            bool[] targetedCars = new bool[count];

            for (int driver = 0; driver < count; driver++)
            {
                int target = assignments[driver];
                if (target < 0 || target >= count || target == driver || targetedCars[target])
                {
                    return false;
                }

                if (assignments[target] == driver)
                {
                    return false;
                }

                targetedCars[target] = true;
            }

            bool[] visited = new bool[count];
            int current = 0;
            for (int step = 0; step < count; step++)
            {
                if (visited[current])
                {
                    return false;
                }

                visited[current] = true;
                current = assignments[current];
            }

            return current == 0;
        }
    }
}
