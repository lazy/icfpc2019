namespace Icfpc2019.Runner
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;

    using Icfpc2019.Solution;
    using Icfpc2019.Solution.Strategies;

    public class Program
    {
        public static void Main(string[] args)
        {
            var baseDir = args.Length > 0 ? args[0] : FindSolutionDir();
            Directory.SetCurrentDirectory(baseDir);

            var strategies =
                typeof(DumbBfs).Assembly.DefinedTypes
                    .Where(type => type.IsClass && typeof(IStrategy).IsAssignableFrom(type))
                    .Select(type => (IStrategy)Activator.CreateInstance(type))
                    .ToArray();

            var totalTimeUnits = 0;
            var haveFailures = false;
            foreach (var mapFile in Directory.EnumerateFiles("Data/maps", "*.desc"))
            {
                var mapName = Path.GetFileNameWithoutExtension(mapFile);
                Console.WriteLine("Processing {0}", mapName);
                var map = MapParser.Parse(File.ReadAllText(mapFile));

                var extSolutionPath = $"Data/extended-solutions/{mapName}.ext-sol";

                // Delete broken solutions
                if (File.Exists(extSolutionPath))
                {
                    var oldSolution = ExtendedSolution.Load(extSolutionPath);
                    var oldCommands = CommandsSerializer.Parse(oldSolution.Commands);
                    if (!Emulator.MakeExtendedSolution(map, string.Empty, oldCommands).IsSuccessful)
                    {
                        File.Delete(extSolutionPath);
                    }
                }

                // Generate new solutions
                foreach (var strategy in strategies)
                {
                    var solution = Emulator.MakeExtendedSolution(map, strategy);
                    solution.SaveIfBetter(extSolutionPath);
                    Console.WriteLine($"  {strategy.Name}: {solution.IsSuccessful}/{solution.TimeUnits}");
                }

                var best = ExtendedSolution.Load(extSolutionPath);
                File.WriteAllText($"Data/solutions/{mapName}.sol", best.Commands);
                Console.WriteLine($"  BEST ({best.StrategyName}): {best.IsSuccessful}/{best.TimeUnits}");

                if (best.TimeUnits.HasValue)
                {
                    totalTimeUnits += best.TimeUnits.Value;
                }
                else
                {
                    haveFailures = true;
                }
            }

            if (haveFailures)
            {
                Console.WriteLine("Not printing the time unit sum some solutions were invalid");
            }
            else
            {
                Console.WriteLine($"TIME UNIT SUM = {totalTimeUnits}");
            }

            var submissionFile = $"Data/submission.zip";
            if (File.Exists(submissionFile))
            {
                File.Delete(submissionFile);
            }

            ZipFile.CreateFromDirectory($"Data/solutions", submissionFile);
        }

        private static string FindSolutionDir()
        {
            var dir = Directory.GetCurrentDirectory();
            while (!File.Exists($"{dir}/icfpc2019.sln"))
            {
                dir = Path.GetDirectoryName(dir);
            }

            return dir;
        }
    }
}