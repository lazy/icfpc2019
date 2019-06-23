namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class CommandsSerializer
    {
        public static IEnumerable<Command[]> Parse(string commands)
        {
            var transposedCommands = commands.Split('#').Select(cmds => ParseOneBot(cmds).ToArray()).ToArray();
            var nextCommandIdx = new int[transposedCommands.Length];
            var botsCount = 1;

            while (true)
            {
                var newBotsCount = botsCount;
                var botCommands = new Command[botsCount];
                var hasCommands = false;

                for (var i = 0; i < botsCount; ++i)
                {
                    var cmdIdx = nextCommandIdx[i]++;
                    if (cmdIdx < transposedCommands[i].Length)
                    {
                        var cmd = transposedCommands[i][cmdIdx];
                        botCommands[i] = cmd;
                        hasCommands = true;

                        if (cmd is Clone)
                        {
                            ++newBotsCount;
                        }
                    }
                }

                if (!hasCommands)
                {
                    yield break;
                }

                yield return botCommands;
                botsCount = newBotsCount;
            }
        }

        public static IEnumerable<Command> ParseOneBot(string commands)
        {
            var i = 0;
            while (i < commands.Length)
            {
                var commandKey = commands[i];
                if (commandKey == 'B')
                {
                    // Parsing like there's no tomorrow
                    var commandEnd = commands.IndexOf(')', i + 2);
                    if (commandEnd == -1)
                    {
                        throw new InvalidDataException($"Invalid commands string: '{commands}'");
                    }

                    var coords = commands.Substring(i + 2, commandEnd - i - 2).Split(',');
                    if (coords.Length != 2)
                    {
                        throw new InvalidDataException("'B' command must have 2 arguments");
                    }

                    yield return new UseManipulatorExtension(int.Parse(coords[0]), int.Parse(coords[1]));

                    i = commandEnd + 1;
                }
                else
                {
                    yield return commandKey switch
                        {
                        'W' => (Command)Move.Up,
                        'S' => Move.Down,
                        'A' => Move.Left,
                        'D' => Move.Right,
                        'Q' => Turn.Left,
                        'E' => Turn.Right,
                        'F' => UseFastWheels.Instance,
                        'L' => UseDrill.Instance,
                        'C' => Clone.Instance,
                        _ => throw new ArgumentOutOfRangeException(nameof(commands), $"Unknown move: {commandKey}"),
                        };
                    ++i;
                }
            }
        }

        public static string Serialize(IEnumerable<Command[]> commands)
        {
            var transposedCommands = new List<List<Command>>();

            foreach (var stepCommands in commands)
            {
                var idx = 0;
                foreach (var botCmd in stepCommands)
                {
                    if (idx >= transposedCommands.Count)
                    {
                        transposedCommands.Add(new List<Command>());
                    }

                    transposedCommands[idx].Add(botCmd);
                    ++idx;
                }
            }

            return string.Join('#', transposedCommands.Select(botCommands => string.Join(string.Empty, botCommands)));
        }
    }
}