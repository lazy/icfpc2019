﻿namespace Icfpc2019.Runner
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;

    using Icfpc2019.Solution;

    public class Program
    {
        // For debugging
        private static readonly bool LogImmediately = false;

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

            var strategies = StrategyFactory.GenerateStrategies().ToArray();

            var totalTimeUnits = 0;
            var haveFailures = false;

            var outputLock = new object();

            Parallel.ForEach(
                Directory.EnumerateFiles("Data/maps", "*.desc"),
                new ParallelOptions { MaxDegreeOfParallelism = -1 },
                mapFile =>
                {
                    var log = new List<string>();

                    void Log(string msg)
                    {
                        if (LogImmediately)
                        {
                            Console.WriteLine($"{msg}");
                        }
                        else
                        {
                            log.Add(msg);
                        }
                    }

                    var mapName = Path.GetFileNameWithoutExtension(mapFile);

                    Log($"Processing {mapName}");
                    var map = MapParser.Parse(File.ReadAllText(mapFile));

                    /*
                    // temporary for cloning debugging
                    if (map.NumCloneBoosts == 0 || map.NumSpawnPoints == 0)
                    {
                        return;
                    }
                    */

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

                    var solutions = strategies /*.AsParallel()*/
                        .Where(strategy => !(mapName.Contains("294") && strategy.Name.Contains("DumbBfs")))
                        .Select(strategy => (strategy, Emulator.MakeExtendedSolution(map, strategy)))
                        .ToArray();

                    foreach (var pair in solutions)
                    {
                        var (strategy, solution) = pair;
                        solution.SaveIfBetter(extSolutionPath);
<<<<<<< HEAD
                        if (solution.IsSuccessful)
                        {
                            log.Add($"  {strategy.Name}: {solution.TimeUnits}");
                        }
=======
                        Log($"  {strategy.Name}: {solution.IsSuccessful}/{solution.TimeUnits}");
>>>>>>> Working strategy composition
                    }

                    var best = ExtendedSolution.Load(extSolutionPath);
                    File.WriteAllText($"Data/solutions/{mapName}.sol", best.Commands);
                    Log($"  BEST ({best.StrategyName}): {best.IsSuccessful}/{best.TimeUnits}");

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