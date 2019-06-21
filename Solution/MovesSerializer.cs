namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class MovesSerializer
    {
        public static IEnumerable<Move> Parse(string moves)
        {
            var i = 0;
            while (i < moves.Length)
            {
                var move = moves[i];
                if (move == 'B')
                {
                    throw new NotImplementedException("Booster B is not implemented yet");
                }
                else
                {
                    yield return move switch
                        {
                        'W' => Move.MoveUp,
                        'S' => Move.MoveDown,
                        'A' => Move.MoveLeft,
                        'D' => Move.MoveRight,
                        'Q' => Move.TurnLeft,
                        'E' => Move.TurnRight,
                        'F' => Move.UseFastWheels,
                        'L' => Move.UseDrill,
                        _ => throw new ArgumentOutOfRangeException(nameof(moves), $"Unknown move: {move}"),
                        };
                    ++i;
                }
            }
        }

        public static string Serialize(IEnumerable<Move> moves) =>
            string.Join(string.Empty, moves.Select(SerializeMove));

        public static string SerializeMove(Move move) =>
            move switch
                {
                Move.MoveUp => "W",
                Move.MoveDown => "S",
                Move.MoveLeft => "A",
                Move.MoveRight => "D",
                Move.TurnLeft => "Q",
                Move.TurnRight => "E",
                Move.UseManipulatorExtension => throw new NotImplementedException(),
                Move.UseFastWheels => "F",
                Move.UseDrill => "L",
                _ => throw new ArgumentOutOfRangeException($"Unexpected move: {move}"),
                };
    }
}