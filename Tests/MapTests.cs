namespace Icfpc2019.Tests
{
    using System;

    using Icfpc2019.Solution;

    using Xunit;
    using Xunit.Abstractions;

    public class MapTests
    {
        private readonly ITestOutputHelper testOutputHelper;

        public MapTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

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

        [Fact]
        public void TestVisibility()
        {
            var map = Map.FromAscii(
                @"xxxxxxx",
                @"x..#..x",
                @"x...v.x",
                @"x.#...x",
                @"x...#.x",
                @"x.....x",
                @"xxxxxxx");

            var x0 = 4;
            var y0 = 4;

            Assert.True(map.AreVisible(x0, y0, 1, 1));
            Assert.True(map.AreVisible(x0, y0, 2, 1));
            Assert.True(map.AreVisible(x0, y0, 3, 1));
            Assert.False(map.AreVisible(x0, y0, 4, 1));
            Assert.True(map.AreVisible(x0, y0, 5, 1));

            Assert.False(map.AreVisible(x0, y0, 1, 2));
            Assert.True(map.AreVisible(x0, y0, 2, 2));
            Assert.True(map.AreVisible(x0, y0, 3, 2));
            Assert.False(map.AreVisible(x0, y0, 4, 2));
            Assert.True(map.AreVisible(x0, y0, 5, 2));

            Assert.False(map.AreVisible(x0, y0, 1, 3));
            Assert.False(map.AreVisible(x0, y0, 2, 3));
            Assert.True(map.AreVisible(x0, y0, 3, 3));
            Assert.True(map.AreVisible(x0, y0, 4, 3));
            Assert.True(map.AreVisible(x0, y0, 5, 3));

            Assert.True(map.AreVisible(x0, y0, 1, 4));
            Assert.True(map.AreVisible(x0, y0, 2, 4));
            Assert.True(map.AreVisible(x0, y0, 3, 4));
            Assert.True(map.AreVisible(x0, y0, 4, 4));
            Assert.True(map.AreVisible(x0, y0, 5, 4));

            Assert.True(map.AreVisible(x0, y0, 1, 5));
            Assert.False(map.AreVisible(x0, y0, 2, 5));
            Assert.False(map.AreVisible(x0, y0, 3, 5));
            Assert.True(map.AreVisible(x0, y0, 4, 5));
            Assert.True(map.AreVisible(x0, y0, 5, 5));
        }
    }
}