namespace Icfpc2019.Tests
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
                new Command[] { Move.Up, },
                new Command[] { Move.Down },
                new Command[] { Move.Left },
                new Command[] { Move.Right },
                new Command[] { Turn.Left },
                new Command[] { Turn.Right },
                new Command[] { UseFastWheels.Instance },
                new Command[] { UseDrill.Instance });

        [Fact]
        public void UseBoostBWorks() =>
            TestSerialization(
                "B(-1,15)B(1,1)",
                new Command[] { new UseManipulatorExtension(-1, 15) },
                new Command[] { new UseManipulatorExtension(1, 1) });

        [Fact]
        public void ClonningParses() =>
            TestSerialization(
                "WWCA#D",
                new Command[] { Move.Up },
                new Command[] { Move.Up },
                new Command[] { Clone.Instance },
                new Command[] { Move.Left, Move.Right });

        private static void TestSerialization(
            string serializedMoves,
            params Command[]
                [] commands)
        {
            var parsedMoves = CommandsSerializer.Parse(serializedMoves);
            var serializedBackMoves = CommandsSerializer.Serialize(commands);

            Assert.Equal(commands, parsedMoves);

            Assert.Equal(serializedMoves, serializedBackMoves);
        }
    }
}