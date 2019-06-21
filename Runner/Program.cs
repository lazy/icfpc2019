namespace Icfpc2019.Runner
{
    using System;
    using System.IO;

    using Icfpc2019.Solution;
    using Icfpc2019.Solution.Strategies;

    public class Program
    {
        public static void Main(string[] args)
        {
            var mapDir = args[0];
            var solutionDir = args[1];

            foreach (var mapFile in Directory.EnumerateFiles(mapDir, "*.desc"))
            {
                Console.WriteLine("Processing {0}", mapFile);
                var map = MapParser.Parse(File.ReadAllText(mapFile));
                var dumbBfs = new DumbBfs();
                var solution = MovesSerializer.Serialize(dumbBfs.Solve(map));
                var solutionFile = Path.Combine(solutionDir, Path.GetFileNameWithoutExtension(mapFile) + ".sol");
                File.WriteAllText(solutionFile, solution);
            }
        }
    }
}