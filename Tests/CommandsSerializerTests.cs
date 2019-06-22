﻿namespace Icfpc2019.Tests
{
    using System.Linq;

    using Icfpc2019.Solution;

    using Xunit;

    public class CommandsSerializerTests
    {
        [Fact]
        public void AllMovesWorkExceptUseBoosterB() =>
            TestSerialization(
                "WSADQEFL",
                new Command[]
                {
                    Move.Up,
                    Move.Down,
                    Move.Left,
                    Move.Right,
                    Turn.Left,
                    Turn.Right,
                    UseFastWheels.Instance,
                    UseDrill.Instance
                });

        [Fact]
        public void UseBoostBWorks() =>
            TestSerialization(
                "B(-1,15)B(1,1)",
                new Command[]
                {
                    new UseManipulatorExtension(-1, 15),
                    new UseManipulatorExtension(1, 1)
                });

        [Fact]
        public void ClonningParses() =>
            TestSerialization(
                "WWCA#D",
                new Command[]
                {
                    Move.Up,
                    Move.Up,
                    Clone.Instance,
                    Move.Left,
                },
                new Command[]
                {
                    Move.Right
                });

        private static void TestSerialization(string serializedMoves, params Command[][] commands)
        {
            var parsedMoves = CommandsSerializer.Parse(serializedMoves);
            var serializedBackMoves = CommandsSerializer.Serialize(commands);

            Assert.Equal(commands, parsedMoves);
            Assert.Equal(serializedMoves, serializedBackMoves);
        }
    }
}