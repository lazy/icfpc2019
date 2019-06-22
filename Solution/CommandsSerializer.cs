namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public static class CommandsSerializer
    {
        public static IEnumerable<Command> Parse(string commands)
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

        public static string Serialize(IEnumerable<Command> commands) =>
            string.Join(string.Empty, commands);
    }
}