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
        private static readonly int? StrategiesLimit = 10;
        private static readonly bool LogImmediately = true;

        public static void Main(string[] args)
        {
            const int Iterations = 1;

            for (var i = 0; i < Iterations; ++i)
            {
                var baseDir = args.Length > 0 ? args[0] : FindSolutionDir();
                Directory.SetCurrentDirectory(baseDir);

                var strategies = StrategyFactory.GenerateStrategies().ToArray();
                var packFile = "Data/booster-pack.txt";
                var mapToPack = File.ReadAllLines(packFile).Select(line => line.Split(' ')).ToDictionary(tokens => tokens[0], tokens => tokens[1]);

                var totalTimeUnits = 0;
                var haveFailures = false;

                var outputLock = new object();

                Parallel.ForEach(
                    Directory.EnumerateFiles("Data/maps", "*.desc"),
                    new ParallelOptions { MaxDegreeOfParallelism = 10 },
                    mapFile =>
                    {
                        var log = new List<string>();
                        var mapName = Path.GetFileNameWithoutExtension(mapFile);

                        void Log(string msg)
                        {
                            if (LogImmediately)
                            {
                                Console.WriteLine($"{mapName}: {msg}");
                            }
                            else
                            {
                                log.Add(msg);
                            }
                        }

                        if (!mapToPack.ContainsKey(mapName))
                        {
                            return;
                        }

                        string packedBoosters = mapToPack.ContainsKey(mapName) ? mapToPack[mapName] : string.Empty;
                        Log($"Processing {mapName} with extra boosters: [{packedBoosters}]");
                        var map = MapParser.Parse(File.ReadAllText(mapFile), packedBoosters);
                        var solutionSuffix = packedBoosters != string.Empty ? "-packed" : string.Empty;

                        var extSolutionPath = $"Data/extended-solutions{solutionSuffix}/{mapName}.ext-sol";

                        // Delete broken solutions
                        if (File.Exists(extSolutionPath))
                        {
                            var oldSolution = ExtendedSolution.Load(extSolutionPath);
                            var oldCommands = CommandsSerializer.Parse(oldSolution.Commands);
                            if (!Emulator.MakeExtendedSolution(map, string.Empty, oldCommands, packedBoosters).IsSuccessful)
                            {
                                File.Delete(extSolutionPath);
                            }
                        }

                        var rng = new Random();
                        var currentStrategies = StrategiesLimit != null
                            ? strategies.OrderBy(s => rng.Next()).Take(StrategiesLimit.Value).ToArray()
                            : strategies;

                        var solutions = currentStrategies.AsParallel()
                            .Where(strategy => !(mapName.Contains("294") && strategy.Name.Contains("DumbBfs")))
                            .Select(strategy => (strategy, Emulator.MakeExtendedSolution(map, strategy, packedBoosters)));

                        var numSuccessful = 0;
                        foreach (var pair in solutions)
                        {
                            var (strategy, solution) = pair;
                            solution.SaveIfBetter(extSolutionPath);
                            if (solution.IsSuccessful)
                            {
                                Log($"  {strategy.Name}: {solution.TimeUnits}");
                                if (StrategiesLimit != null && ++numSuccessful >= StrategiesLimit)
                                {
                                    break;
                                }
                            }
                        }

                        var best = ExtendedSolution.Load(extSolutionPath);
                        File.WriteAllText($"Data/solutions{solutionSuffix}/{mapName}.sol", best.Commands);
                        if (solutionSuffix != string.Empty)
                        {
                            File.WriteAllText($"Data/solutions{solutionSuffix}/{mapName}.buy", packedBoosters);
                        }

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
            }
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