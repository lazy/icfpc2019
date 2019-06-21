namespace Icfpc2019.Tests
{
    using Icfpc2019.Solution;

    using Xunit;

    public class MapParserTests
    {
        [Fact]
        public void TestParseExampleMap()
        {
            const string ExampleMap = "(0,0),(10,0),(10,10),(0,10)#(0,0)#(4,2),(6,2),(6,7),(4,7);(5,8),(6,8),(6,9),(5,9)#B(0,1);B(1,1);F(0,2);F(1,2);L(0,3);X(0,9)";
            var map = MapParser.Parse(ExampleMap);

            var expectedMap = @"
xxxxxxxxxx.
X.........x
.....#....x
..........x
....##....x
....##....x
....##....x
L...##....x
FF..##....x
BB........x
v.........x
".Trim();
            Assert.Equal(expectedMap.Replace("\r\n", "\n"), map.ToString());
        }
    }
}