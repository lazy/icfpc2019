namespace BlockhainAutomator
{
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Icfpc2019.Solution;
    using Icfpc2019.Solution.Strategies;

    public class Program
    {
        private static void Main(string[] args)
        {
            var blockDir = args[0];

            var puzzleFile = Path.Combine(blockDir, "puzzle.cond");
            var puzzle = new Puzzle(File.ReadAllText(puzzleFile));
            File.WriteAllText($"{puzzleFile}.desc", puzzle.SaveToMap());
            puzzle.SaveToBitmap().Save($"{puzzleFile}.png");

            var taskFile = Path.Combine(blockDir, "task.desc");
            var strategies = LookAheadFactory.MakeStrategies().Concat(new[] { new DumbBfs(), }).ToArray();
            var map = MapParser.Parse(File.ReadAllText(taskFile));
            var solutions = strategies.AsParallel()
                .Select(strategy => Emulator.MakeExtendedSolution(map, strategy))
                .ToArray();

            var extSolutionFile = Path.Combine($"{taskFile}.ext-sol");
            foreach (var sln in solutions)
            {
                sln.SaveIfBetter(extSolutionFile);
            }

            var extSlnLines = File.ReadAllLines(extSolutionFile);
            Trace.Assert(extSlnLines.Length > 0);
            File.WriteAllText($"{taskFile}.sol", extSlnLines.Last());
        }
    }
}