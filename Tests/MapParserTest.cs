namespace Icfpc2019.Tests
{
    using System;
    using System.IO;

    using Icfpc2019.Solution;

    using Xunit;
    using Xunit.Abstractions;

    public class MapParserTests
    {
        private readonly ITestOutputHelper testOutputHelper;

        public MapParserTests(ITestOutputHelper testOutputHelper)
        {
            // Use testOutputHelper.WriteLine() instead of Console.WriteLine()
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void TestParseExampleMap()
        {
            const string ExampleMap = "(0,0),(10,0),(10,10),(0,10)#(0,0)#(4,2),(6,2),(6,7),(4,7);(5,8),(6,8),(6,9),(5,9)#B(0,1);B(1,1);F(0,2);F(1,2);L(0,3);X(0,9)";
            var map = MapParser.Parse(ExampleMap);

            var expectedMap = @"
.xxxxxxxxxx.
xX.........x
x.....#....x
x..........x
x....##....x
x....##....x
x....##....x
xL...##....x
xFF..##....x
xBB........x
xv.........x
.xxxxxxxxxx.
".Trim();
            Assert.Equal(expectedMap.Replace("\r\n", "\n"), map.ToString());
        }

        [Fact]
        public void TestMap001()
        {
            var mapContent = File.ReadAllText(@"..\..\..\..\Data\maps\prob-001.desc");
            var map = MapParser.Parse(mapContent);
            var expectedMap = @"
.xxxxxx...
x......xx.
x........x
xv.....xx.
.xxxxxx...
".Trim();
            Assert.Equal(expectedMap.Replace("\r\n", "\n"), map.ToString());
        }

        [Fact]
        public void ParsesAllRealMaps()
        {
            foreach (var mapFile in Directory.EnumerateFiles(@"..\..\..\..\Data\maps", "*.desc"))
            {
                MapParser.Parse(File.ReadAllText(mapFile));
            }
        }
    }
}