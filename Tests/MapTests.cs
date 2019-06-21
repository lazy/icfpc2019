﻿namespace Icfpc2019.Tests
{
    using Icfpc2019.Solution;

    using Xunit;

    public class MapTests
    {
        [Fact]
        public void FromAsciiWorks()
        {
            var map = Map.FromAscii(
                @"xxxxxxxxxxxxxxxxxxxxx",
                @"xxx.....v...........x",
                @"xxxxxx...###.....xxxx",
                @"xx.....BFLX##...xxxxx",
                @"xx................xxx",
                @"xxxxxxxxxxxxxxxxxxxxx");

            Assert.Equal(21, map.Width);
            Assert.Equal(6, map.Height);

            Assert.Equal(Map.Cell.Edge, map[0, 0]);
            Assert.Equal(Map.Cell.Edge, map[1, 1]);
            Assert.Equal(Map.Cell.Empty, map[2, 1]);

            Assert.Equal(Map.Cell.Obstacle, map[9, 3]);
            Assert.Equal(Map.Cell.Obstacle, map[11, 3]);
            Assert.Equal(Map.Cell.Obstacle, map[12, 2]);

            Assert.Equal(Map.Cell.ManipulatorExtension, map[7, 2]);
            Assert.Equal(Map.Cell.FastWheels, map[8, 2]);
            Assert.Equal(Map.Cell.Drill, map[9, 2]);
            Assert.Equal(Map.Cell.MysteriousPoint, map[10, 2]);
        }
    }
}