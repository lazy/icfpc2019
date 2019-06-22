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
            Assert.Equal(Map.Cell.SpawnPoint, map[10, 2]);
        }

        [Fact]
        public void TestVisibility()
        {
            var map = Map.FromAscii(
                @"xxxx",
                @"x..x",
                @"x..x",
                @"x..x",
                @"x.#x",
                @"xv.x",
                @"xxxx");

            var x0 = 1;
            var y0 = 1;

            Assert.True(map.AreVisible(x0, y0, 2, 1));
            Assert.False(map.AreVisible(x0, y0, 2, 2));
            Assert.False(map.AreVisible(x0, y0, 2, 3));
            Assert.True(map.AreVisible(x0, y0, 2, 4));
            Assert.True(map.AreVisible(x0, y0, 2, 5));
        }
    }
}