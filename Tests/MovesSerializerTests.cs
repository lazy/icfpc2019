﻿namespace Icfpc2019.Tests
{
    using System.Linq;

    using Icfpc2019.Solution;

    using Xunit;

    public class MovesSerializerTests
    {
        [Fact]
        public void AllMovesWorkExceptUseBoosterB() =>
            TestSerialization(
                "WSADQEFL",
                Move.MoveUp,
                Move.MoveDown,
                Move.MoveLeft,
                Move.MoveRight,
                Move.TurnLeft,
                Move.TurnRight,
                Move.UseBoosterF,
                Move.UseBoosterL);

        [Fact]
        public void UseBoostBWorks() =>
            TestSerialization(
                "B(-1,15)B(1,1)",
                Move.UseBoosterB,
                Move.UseBoosterB);

        private static void TestSerialization(string serializedMoves, params Move[] moves)
        {
            var parsedMoves = MovesSerializer.Parse(serializedMoves).ToArray();
            var serializedBackMoves = MovesSerializer.Serialize(moves);

            Assert.Equal(moves, parsedMoves);
            Assert.Equal(serializedMoves, serializedBackMoves);
        }
    }
}