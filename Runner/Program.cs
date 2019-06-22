namespace Icfpc2019.Runner
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;

    using Icfpc2019.Solution;
    using Icfpc2019.Solution.Strategies;

    public class Program
    {
        public static void Main(string[] args)
        {
            var baseDir = args.Length > 0 ? args[0] : FindSolutionDir();
            Directory.SetCurrentDirectory(baseDir);

            /*
            var strategies =
                typeof(DumbBfs).Assembly.DefinedTypes
                    .Where(type => type.IsClass && !type.IsAbstract && typeof(IStrategy).IsAssignableFrom(type))
                    .Select(type => (IStrategy)Activator.CreateInstance(type))
                    .ToArray();
            */

            var strategies = LookAheadFactory.MakeStrategies().Concat(new[] { new DumbBfs(), }).ToArray();

            var totalTimeUnits = 0;
            var haveFailures = false;

            var outputLock = new object();

            Parallel.ForEach(
                Directory.EnumerateFiles("Data/maps", "*.desc").Reverse(),
                new ParallelOptions { MaxDegreeOfParallelism = -1 },
                mapFile =>
                {
                    var log = new List<string>();

                    var mapName = Path.GetFileNameWithoutExtension(mapFile);
                    log.Add($"Processing {mapName}");
                    var map = MapParser.Parse(File.ReadAllText(mapFile));

                    var extSolutionPath = $"Data/extended-solutions/{mapName}.ext-sol";

                    /*
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
                    */

                    // Generate new solutions
                    foreach (var strategy in strategies)
                    {
                        if (mapName.Contains("294") && strategy.Name.Contains("DumbBfs"))
                        {
                            continue;
                        }

                        var solution = Emulator.MakeExtendedSolution(map, strategy);
                        solution.SaveIfBetter(extSolutionPath);
                        log.Add($"  {strategy.Name}: {solution.IsSuccessful}/{solution.TimeUnits}");
                    }

                    var best = ExtendedSolution.Load(extSolutionPath);
                    File.WriteAllText($"Data/solutions/{mapName}.sol", best.Commands);
                    log.Add($"  BEST ({best.StrategyName}): {best.IsSuccessful}/{best.TimeUnits}");

                    lock (outputLock)
                    {
                        if (best.TimeUnits.HasValue)
                        {
                            totalTimeUnits += best.TimeUnits.Value;
                        }
                        else
                        {
                            haveFailures = true;
                        }

                        foreach (var line in log)
                        {
                            Console.WriteLine(line);
                        }
                    }
                });

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