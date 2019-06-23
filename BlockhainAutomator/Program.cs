namespace BlockhainAutomator
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Icfpc2019.Solution;
    using Icfpc2019.Solution.Strategies;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class Program
    {
        private const string BlockChainUrl = "https://lambdacoin.org/lambda";

        private static string GetJsonPayload(string methodName, Dictionary<string, string> @params)
        {
            return JsonConvert.SerializeObject(
                new
                {
                    jsonrpc = "2.0",
                    id = "2ivans",
                    method = methodName,
                    @params,
                });
        }

        private static async Task<JObject> FetchApiAsync(string methodName, string? arg)
        {
            var httpClient = new HttpClient();
            var suffix = arg == null ? string.Empty : $"/{arg}";
            var response = await httpClient.GetAsync($"{BlockChainUrl}/{methodName}{suffix}");
            var rawResponse = await response.Content.ReadAsStringAsync();
            return JObject.Parse(rawResponse);
        }

        private static async Task SolveCurrentBlockAsync()
        {
            var metaInfo = await FetchApiAsync("getblockchaininfo", null);
            var blockNum = metaInfo["block"].Value<int>();
            var blockInfo = await FetchApiAsync("getblockinfo", blockNum.ToString());
            var blockTs = DateTimeOffset.FromUnixTimeSeconds((int)blockInfo["block_ts"].Value<double>());
            var blockAge = DateTimeOffset.UtcNow - blockTs;
            var puzzleText = blockInfo["puzzle"].Value<string>();
            var taskText = blockInfo["task"].Value<string>();

            Console.WriteLine($"Current block: #{blockNum}, aged {blockAge}");

            var solutionDir = FindSolutionDir();
            var blockDir = Path.Combine(solutionDir, $@"Data\blocks\{blockNum}");

            if (!Directory.Exists(blockDir))
            {
                Console.WriteLine("This is a totally new block; creating a new directory");
                Directory.CreateDirectory(blockDir);
            }

            var puzzleFile = Path.Combine(blockDir, "puzzle.cond");
            if (!File.Exists(puzzleFile))
            {
                Console.WriteLine($"Writing the original puzzle: {puzzleText}");
                File.WriteAllText(puzzleFile, puzzleText);
            }

            var puzzleSolutionFile = $"{puzzleFile}.desc";
            if (!File.Exists(puzzleSolutionFile))
            {
                Console.WriteLine("Solving puzzle");
                var puzzle = new Puzzle(puzzleText);
                File.WriteAllText(puzzleSolutionFile, puzzle.SaveToMap());
                puzzle.SaveToBitmap().Save($"{puzzleFile}.png");
            }

            var taskFile = Path.Combine(blockDir, "task.desc");
            if (!File.Exists(taskFile))
            {
                Console.WriteLine($"Writing the original task: {taskText}");
                File.WriteAllText(taskFile, taskText);
            }

            var extSolutionFile = $"{taskFile}.ext-sol";
            var solutionFile = $"{taskFile}.sol";
            if (!File.Exists(extSolutionFile))
            {
                Console.WriteLine("Solving task");
                var strategies = LookAheadFactory.MakeStrategies().Concat(new[] { new DumbBfs(), }).ToArray();
                var map = MapParser.Parse(taskText);
                var solutions = strategies.AsParallel()
                    .Select(strategy => Emulator.MakeExtendedSolution(map, strategy))
                    .Take(20)
                    .ToArray();

                foreach (var sln in solutions)
                {
                    sln.SaveIfBetter(extSolutionFile);
                }

                var extSlnLines = File.ReadAllLines(extSolutionFile);
                Trace.Assert(extSlnLines.Length > 0);
                File.WriteAllText(solutionFile, extSlnLines.Last());
            }

            Console.WriteLine($"Now run: lambda-cli.py submit {blockNum} {Path.GetFullPath(solutionFile)} {Path.GetFullPath(puzzleSolutionFile)}");
        }

        private static void Main()
        {
            while (true)
            {
                Console.WriteLine("Solving the current block");
                try
                {
                    SolveCurrentBlockAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Something is wrong: {ex}!");
                    Console.Error.WriteLine($"Things went south at: {ex.StackTrace}");
                }

                Console.WriteLine("Sleeping before attempting again");
                Thread.Sleep(TimeSpan.FromSeconds(25));
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