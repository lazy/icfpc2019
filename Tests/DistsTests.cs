namespace Icfpc2019.Tests
{
    using System.IO;

    using Icfpc2019.Solution;

    using Xunit;

    public class DistsTests
    {
        [Fact]
        public void CanCalcDists()
        {
            var map = MapParser.Parse(File.ReadAllText("../../../../Data/maps/prob-067.desc"));
            var dists = new DistsFromCenter(new State(map));
            var bmp = dists.SaveToBitmap();
            bmp.Save("../../../../prob.png");
        }
    }
}