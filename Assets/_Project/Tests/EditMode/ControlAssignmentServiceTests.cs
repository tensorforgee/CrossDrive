using System;
using System.Collections.Generic;
using CrossDrive.Gameplay;
using NUnit.Framework;

namespace CrossDrive.Tests
{
    public sealed class ControlAssignmentServiceTests
    {
        [Test]
        public void GeneratedAssignmentsAreCompleteFourPlayerCycles()
        {
            ControlAssignmentService service = new ControlAssignmentService();

            for (int seed = 0; seed < 200; seed++)
            {
                ControlAssignment assignment = service.Create(4, new Random(seed));
                Assert.That(ControlAssignmentService.IsValidCompleteCycle(assignment.CopyDriverToCarOwnerMap()), Is.True);
            }
        }

        [Test]
        public void GeneratedAssignmentsHaveNoSelfAssignments()
        {
            ControlAssignment assignment = new ControlAssignmentService().Create(4, new Random(19));

            for (int player = 0; player < assignment.PlayerCount; player++)
            {
                Assert.That(assignment.GetControlledCarOwner(player), Is.Not.EqualTo(player));
            }
        }

        [Test]
        public void GeneratedAssignmentsGiveEveryCarExactlyOneDriver()
        {
            ControlAssignment assignment = new ControlAssignmentService().Create(4, new Random(27));
            HashSet<int> controlledCars = new HashSet<int>(assignment.CopyDriverToCarOwnerMap());

            Assert.That(controlledCars.Count, Is.EqualTo(4));
        }

        [Test]
        public void GeneratedAssignmentsHaveNoMutualTwoPlayerSwaps()
        {
            ControlAssignment assignment = new ControlAssignmentService().Create(4, new Random(42));

            for (int player = 0; player < assignment.PlayerCount; player++)
            {
                int target = assignment.GetControlledCarOwner(player);
                Assert.That(assignment.GetControlledCarOwner(target), Is.Not.EqualTo(player));
            }
        }

        [TestCase(new int[] { 1, 0, 3, 2 })]
        [TestCase(new int[] { 1, 0, 3, 4, 2 })]
        [TestCase(new int[] { 0, 2, 3, 1 })]
        [TestCase(new int[] { 1, 2, 0, 4, 5, 3 })]
        public void InvalidCyclesAreRejected(int[] map)
        {
            Assert.That(ControlAssignmentService.IsValidCompleteCycle(map), Is.False);
        }
    }
}
