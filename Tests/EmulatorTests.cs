﻿namespace Icfpc2019.Tests
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
                15);

        [Fact]
        public void SampleSolution2IsValid() =>
            this.TestOnSampleMap(
                "WWDSFDDDDAQWWWWQAAAQSSSQDWWDDDWSSS",
                true,
                15);

        [Fact]
        public void SampleSolution3IsValid() =>
            this.TestOnSampleMap(
                "WDWAWSSFDDDDDQQWLAAAAAWEEDDDDDWQQAAAAAWEEDDDDD",
                true,
                15);

        private void TestOnSampleMap(string moves, bool isSuccessful, int? timeUnits)
        {
            var solution = Emulator.MakeExtendedSolution(this.sampleMap, "test", MovesSerializer.Parse(moves));
            Assert.Equal(isSuccessful, solution.IsSuccessful);
            Assert.Equal(timeUnits, solution.TimeUnits);
        }
    }
}