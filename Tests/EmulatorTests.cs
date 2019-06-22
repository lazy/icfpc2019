namespace Icfpc2019.Tests
{
    using Icfpc2019.Solution;

    using Xunit;

    public class EmulatorTests
    {
        private readonly Map sampleMap = Map.FromAscii(
            @"xxxxxxxxxxxx",
            @"x..........x",
            @"x.....#....x",
            @"x..........x",
            @"x....##....x",
            @"x....##....x",
            @"x....##....x",
            @"xL...##....x",
            @"xFF..##....x",
            @"xBB........x",
            @"xv.........x",
            @"xxxxxxxxxxxx");

        [Fact]
        public void SampleSolution1IsValid() =>
            this.TestOnSampleMap(
                "WDWB(1,2)DSQDB(-3,1)DDDWWWWWWWSSEDSSDWWESSSSSAAAAAQQWWWWWWW",
                true,
                48);

        [Fact]
        public void SampleSolution2IsValid() =>
            this.TestOnSampleMap(
                "WWDSFDDDDAQWWWWQAAAQSSSQDWWDDDWSSS",
                true,
                34);

        [Fact]
        public void SampleSolution3IsValid() =>
            this.TestOnSampleMap(
                "WDWAWSSFDDDDDQQWLAAAAAWEEDDDDDWQQAAAAAWEEDDDDD",
                true,
                46);

        [Fact]
        public void CloningSampleSolutionIsValid()
        {
            var map = MapParser.Parse("(0,0),(10,0),(10,10),(0,10)#(0,0)#(4,2),(6,2),(6,7),(4,7);(5,8),(6,8),(6,9),(5,9)#B(0,1);F(0,2);L(0,3);R(0,4);C(0,5);C(0,6);C(0,7);X(0,9)");
            var commands = CommandsSerializer.Parse("WWWWWWWWWCDDDDDDESSSSSS#CDDDDDDDDESSSSSSSS#CSSDDDESSSSS#ESSSSSSSSSQDDDDD");
            var solution = Emulator.MakeExtendedSolution(map, "test", commands);
            Assert.True(solution.IsSuccessful);
            Assert.Equal(28, solution.TimeUnits);
        }

        [Fact]
        public void EmptySolutionIsInvalid()
        {
            this.TestOnSampleMap(string.Empty, false, null);
        }

        [Fact]
        public void StupidSolutionIsInvalid()
        {
            this.TestOnSampleMap("ASDFFAS", false, null);
        }

        [Fact]
        public void MovingIntoWallsIsInvalid()
        {
            this.TestOnSampleMap("WDWAWSSFDDDDDQQWLAAAAAWEEDDDDDWQQAAAAAWEEDDDDDDDDD", false, null);
        }

        private void TestOnSampleMap(string moves, bool isSuccessful, int? timeUnits)
        {
            var solution = Emulator.MakeExtendedSolution(this.sampleMap, "test", CommandsSerializer.Parse(moves));
            Assert.Equal(isSuccessful, solution.IsSuccessful);
            Assert.Equal(timeUnits, solution.TimeUnits);
        }
    }
}