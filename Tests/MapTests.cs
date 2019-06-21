namespace Icfpc2019.Tests
{
    using Icfpc2019.Solution;

    using Xunit;

    public class MapTests
    {
        [Fact]
        public void FromAsciiWorks()
        {
            var map = Map.FromAscii(
                8,
                4,
                @"xxxxxxxxxxxxxxxxxxxxx",
                @"xxx.................x",
                @"xxxxxx...###.....xxxx",
                @"xx.....BFDX##...xxxxx",
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

            Assert.Equal(Map.Cell.BoosterB, map[7, 2]);
            Assert.Equal(Map.Cell.BoosterF, map[8, 2]);
            Assert.Equal(Map.Cell.BoosterD, map[9, 2]);
            Assert.Equal(Map.Cell.BoosterX, map[10, 2]);
        }
    }
}
