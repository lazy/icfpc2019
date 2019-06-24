namespace AssembleSubmission
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;

    using Icfpc2019.Solution;

    public class Program
    {
        public static readonly int InitialBudget = 275000;

        public static void Main(string[] args)
        {
            var baseDir = args.Length > 0 ? args[0] : FindSolutionDir();
            Directory.SetCurrentDirectory(baseDir);

            var mapFiles = Directory.EnumerateFiles("Data/maps", "*.desc").ToList();
            var maps = mapFiles.Select(
                    filename =>
                    {
                        var mapName = Path.GetFileNameWithoutExtension(filename);
                        var map = MapParser.Parse(File.ReadAllText(filename), string.Empty);

                        var originalSolution = ExtendedSolution.Load($"Data/extended-solutions/{mapName}.ext-sol");

                        var boostedSolution = File.Exists($"Data/extended-solutions-packed/{mapName}.ext-sol")
                            ? ExtendedSolution.Load($"Data/extended-solutions-packed/{mapName}.ext-sol")
                            : null;

                        return new MapData
                        {
                            Name = mapName,
                            OriginalSolution = originalSolution,
                            Width = map.Width,
                            Height = map.Height,
                            BoostedSolution = boostedSolution,
                        };
                    })
                .ToArray();

            var nameToSolution = maps.ToDictionary(
                data => data.Name,
                data => data.OriginalSolution);

            var budget = InitialBudget;
            foreach (var map in maps.OrderBy(data => (data.GetProfit() / (float)data.BoostCost)))
            {
                if (map.BoostedSolution != null && map.BoostCost < budget)
                {
                    nameToSolution[map.Name] = map.BoostedSolution;
                    budget -= map.BoostCost;
                }
            }

            if (Directory.Exists("Data/submission"))
            {
                Directory.Delete("Data/submission", true);
            }

            Directory.CreateDirectory("Data/submission");

            foreach (var kvp in nameToSolution)
            {
                var (name, solution) = kvp;
                File.WriteAllText($"Data/submission/{name}.sol", solution.Commands);
                if (!string.IsNullOrEmpty(solution.BoosterPack))
                {
                    File.WriteAllText($"Data/submission/{name}.buy", solution.BoosterPack);
                }
            }

            var submissionFile = $"Data/submission.zip";
            if (File.Exists(submissionFile))
            {
                File.Delete(submissionFile);
            }

            ZipFile.CreateFromDirectory($"Data/submission", submissionFile);
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

        private class MapData
        {
            public string Name { get; set; } = string.Empty;

            public int Width { get; set; }
            public int Height { get; set; }

            public ExtendedSolution? OriginalSolution { get; set; }
            public ExtendedSolution? BoostedSolution { get; set; }

            public int BoostCost => 2000 * (this.BoostedSolution?.BoosterPack?.Length ?? 0);

            public int MapCost => (int)(1000 * Math.Ceiling(Math.Log(this.Width * this.Height)));

            public int GetProfit()
            {
                if (this.OriginalSolution == null || this.BoostedSolution == null)
                {
                    return 0;
                }

                var origTime = (float)(this.OriginalSolution.TimeUnits ?? 0);
                var boostedTime = (float)(this.BoostedSolution.TimeUnits ?? 0);
                return (int)(this.MapCost * ((origTime / boostedTime) - 1));
            }
        }
    }
}