namespace Icfpc2019.Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class CloneStrategy : IStrategy
    {
        private readonly IStrategy baseStrategy;

        public CloneStrategy(IStrategy baseStrategy)
        {
            this.baseStrategy = baseStrategy;
        }

        public string Name => $"Clone({this.baseStrategy.Name})";

        public IEnumerable<Command[]> Solve(State state)
        {
            if (state.Map.NumCloneBoosts == 0 || state.Map.NumSpawnPoints == 0)
            {
                return this.baseStrategy.Solve(state);
            }

            return this.SolveWithClones(state);
        }

        public IEnumerable<Command[]> SolveWithClones(State state)
        {
            var addedBots = new List<(int time, State.Bot bot)>();
            var time = 0;

            var cloningCommands = MakeClones().ToList();
            var cloningTime = cloningCommands.Count;

            var stateAfterCloning = state;

            var firstBotSolutionSize = this.baseStrategy
                .Solve(stateAfterCloning.ReplaceBots(resetBoosters: false, stateAfterCloning.GetBot(0)))
                .Count();

            var totalBonusTime = addedBots.Sum(tup => cloningTime - tup.time);
            var stepsPerBot = Math.Min(
                firstBotSolutionSize / (1 + addedBots.Count),
                Math.Max(0, firstBotSolutionSize - totalBonusTime) / (1 + addedBots.Count));

            while (true)
            {
                var commandsPerBot = new Command[addedBots.Count + 1][];

                var botState = stateAfterCloning.ReplaceBots(resetBoosters: false, state.GetBot(0));
                var bot = botState.GetBot(0);
                var botIdx = 0;

                while (true)
                {
                    var bonusTime = botIdx == 0
                        ? 0
                        : cloningTime - addedBots[botIdx - 1].time;
                    var left = stepsPerBot + bonusTime;
                    var commandsBuf = new List<Command>();
                    foreach (var cmd in this.baseStrategy.Solve(botState))
                    {
                        if (left-- <= 0)
                        {
                            break;
                        }

                        commandsBuf.Add(cmd.Single());

                        var nextState = botState.Next(cmd);
                        if (nextState == null)
                        {
                            throw new Exception("shouldn't happen");
                        }

                        botState = nextState;
                    }

                    commandsPerBot[botIdx] = commandsBuf.ToArray();

                    if (++botIdx >= stateAfterCloning.BotsCount)
                    {
                        break;
                    }

                    botState = botState.ReplaceBots(resetBoosters: true, addedBots[botIdx - 1].bot);
                }

                if (botState.Map.CellsToVisit.Count == botState.WrappedCellsCount)
                {
                    commandsPerBot[0] = cloningCommands.Concat(commandsPerBot[0]).ToArray();
                    foreach (var cmds in CommandsSerializer.Transponse(commandsPerBot))
                    {
                        yield return cmds;
                    }

                    break;
                }

                stepsPerBot = (int)(1 + (stepsPerBot * 1.1));
            }

            IEnumerable<Command> MakeClones()
            {
                var map = state.Map;
                var bfs = new BfsState(map);
                while (addedBots.Count < map.NumCloneBoosts)
                {
                    var bot = state.GetBot(0);

                    foreach (var cmd in FindBooster())
                    {
                        ++time;
                        state = state.Next(cmd) ?? throw new Exception("Impossible");
                        yield return cmd;
                    }

                    while (map[bot.X, bot.Y] == Map.Cell.SpawnPoint && state.CloneBoosterCount > 0)
                    {
                        ++time;
                        addedBots.Add((time, bot));
                        state = state.Next(Clone.Instance) ?? throw new Exception("Impossible");
                        yield return Clone.Instance;
                    }
                }

                IEnumerable<Command> FindBooster()
                {
                    var bot = state.GetBot(0);

                    ++bfs.Generation;
                    bfs.Queue.Clear();
                    bfs.Queue.Enqueue((bot.X, bot.Y, bot.Dir));
                    bfs.Nodes[bot.X, bot.Y, 0] = new BfsState.Node(bfs.Generation, -1, 0);
                    while (bfs.Queue.Count > 0)
                    {
                        var (x, y, dir) = bfs.Queue.Dequeue();

                        if ((map[x, y] == Map.Cell.Clone && !state.IsPickedUp(x, y) && (x, y) != (bot.X, bot.Y)) ||
                            (map[x, y] == Map.Cell.SpawnPoint && state.CloneBoosterCount > 0))
                        {
                            bfs.FindBackwardPath(x, y, 0, bot);
                            return bfs.Path;
                        }

                        for (var i = 0; i < Move.All.Length; ++i)
                        {
                            var move = Move.All[i];
                            var nx = x + move.Dx;
                            var ny = y + move.Dy;
                            if (map.IsFree(nx, ny) && bfs.Nodes[nx, ny, dir].Generation != bfs.Generation)
                            {
                                bfs.Nodes[nx, ny, dir] = new BfsState.Node(bfs.Generation, i, 0);
                                bfs.Queue.Enqueue((nx, ny, dir));
                            }
                        }
                    }

                    throw new Exception("oops");
                }
            }
        }
    }
}