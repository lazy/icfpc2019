namespace OptimizeBoosters
{
    using System;
    using System.IO;
    using System.Linq;

    using Icfpc2019.Solution;

    public class Program
    {
        public static readonly int OriginalBudget = 350000;

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
                        var solution = ExtendedSolution.Load($"Data/extended-solutions/{mapName}.ext-sol");

                        return new MapState
                        {
                            MapName = mapName,
                            OriginalScore = solution.TimeUnits ?? 0,
                            Width = map.Width,
                            Height = map.Height,
                            HasSpawns = map.NumSpawnPoints > 0,
                            NumOriginalClones = map.NumCloneBoosts,
                            NumNewClones = 0,
                        };
                    })
                .ToArray();

            var budget = OriginalBudget;
            while (budget >= 2000)
            {
                budget -= 2000;

                var best = maps[0];
                var bestProfit = 0;

                foreach (var map in maps)
                {
                    var curScore = map.ProfitWithExtra(map.NumNewClones);
                    var newScore = map.ProfitWithExtra(map.NumNewClones + 1);
                    var profit = newScore - curScore;

                    if (profit > bestProfit)
                    {
                        best = map;
                        bestProfit = profit;
                    }
                }

                best.NumNewClones += 1;
                best.Boosters += 'C';
            }

            foreach (var map in maps)
            {
                if (map.NumNewClones > 0)
                {
                    Console.WriteLine($"{map.MapName} {map.Boosters}");
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

        public class MapState
        {
            public string MapName { get; set; } = string.Empty;
            public int OriginalScore { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public bool HasSpawns { get; set; }
            public int NumOriginalClones { get; set; }

            public int NumNewClones { get; set; }
            public string Boosters { get; set; } = string.Empty;

            public int Cost => (int)(1000 * Math.Ceiling(Math.Log(this.Width * this.Height)));

            public int ProfitWithExtra(int c)
            {
                if (!this.HasSpawns)
                {
                    return 0;
                }

                var oldBots = 1 + this.NumOriginalClones;
                var newBotCost = 0.8;
                var newBots = 0.0;
                for (var i = 0; i < c; ++i)
                {
                    newBots += newBotCost;
                    newBotCost *= 0.8;
                }

                var speedup = (float)(oldBots + newBots) / (float)oldBots;

                return (int)(this.Cost * speedup);
            }
        }
    }
}