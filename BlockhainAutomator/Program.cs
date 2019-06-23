namespace BlockhainAutomator
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Icfpc2019.Solution;
    using Icfpc2019.Solution.Strategies;

    using Newtonsoft.Json.Linq;

    public class Program
    {
        private const string BlockChainUrl = "https://lambdacoin.org/lambda";

        private static async Task<string> FetchApiAsync(string methodName, string? arg)
        {
            var httpClient = new HttpClient();
            var suffix = arg == null ? string.Empty : $"/{arg}";
            var response = await httpClient.GetAsync($"{BlockChainUrl}/{methodName}{suffix}");
            return await response.Content.ReadAsStringAsync();
        }

        private static async Task SolveCurrentBlockAsync()
        {
            var balanceInfo = int.Parse(await FetchApiAsync("getbalance", "83"));
            var metaInfo = JObject.Parse(await FetchApiAsync("getblockchaininfo", null));
            var blockNum = metaInfo["block"].Value<int>();
            var blockSubs = metaInfo["block_subs"].Value<int>();
            var blockInfo = JObject.Parse(await FetchApiAsync("getblockinfo", blockNum.ToString()));
            var blockTs = DateTimeOffset.FromUnixTimeSeconds((int)blockInfo["block_ts"].Value<double>());
            var blockAge = DateTimeOffset.UtcNow - blockTs;
            var puzzleText = blockInfo["puzzle"].Value<string>();
            var taskText = blockInfo["task"].Value<string>();

            Console.WriteLine($"Current block: #{blockNum}, aged {blockAge}, {blockSubs} submissions, our balance: {balanceInfo}");

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
                var seed = 3133337;
                while (true)
                {
                    Console.WriteLine($"Solving puzzle with seed {seed}");
                    var puzzle = new Puzzle(puzzleText, seed);
                    File.WriteAllText(puzzleSolutionFile, puzzle.SaveToMap());
                    puzzle.SaveToBitmap().Save($"{puzzleFile}.png");
                    try
                    {
                        puzzle.EnsureMapIsValid(puzzleSolutionFile);
                        Console.WriteLine("Have a valid solution");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Puzzle solution is invalid: {ex.Message}");
                        ++seed;
                    }
                }
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
                    .ToArray();

                foreach (var sln in solutions)
                {
                    sln.SaveIfBetter(extSolutionFile);
                }

                var extSlnLines = File.ReadAllLines(extSolutionFile);
                Trace.Assert(extSlnLines.Length > 0);
                File.WriteAllText(solutionFile, extSlnLines.Last());
            }

            var submissionResultFile = Path.Combine(blockDir, "submit.txt");

            if (!File.Exists(submissionResultFile))
            {
                Console.WriteLine("SUBMITTING!");

                var payload = new MultipartFormDataContent();
                payload.Add(new StringContent("2a78df7b478788ae4cf9d338"), "private_id");
                payload.Add(new StringContent(blockNum.ToString()), "block_num");
                payload.Add(new ByteArrayContent(File.ReadAllBytes(solutionFile)), "solution", "task.desc.sol");
                payload.Add(new ByteArrayContent(File.ReadAllBytes(puzzleSolutionFile)), "puzzle", "puzzle.cond.desc");

                var httpClient = new HttpClient();
                var response = await httpClient.PostAsync($"{BlockChainUrl}/submit", payload);
                var submissionResult = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"RESULT: {submissionResult}");
                File.WriteAllText(submissionResultFile, submissionResult);
            }
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
                }

                var timeout = TimeSpan.FromSeconds(25);
                Console.WriteLine($"Sleeping for {timeout} before attempting again");
                Thread.Sleep(timeout);
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