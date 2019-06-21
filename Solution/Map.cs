namespace Icfpc2019.Solution
{
    using System;

    public struct Map
    {
        private readonly Cell[,] cells;

        public Map(int startX, int startY, Cell[,] cells)
        {
            this.StartX = startX;
            this.StartY = startY;
            this.cells = cells;
        }

        public enum Cell
        {
            Empty,
            Obstacle,
            Edge,
            BoosterB,
            BoosterF,
            BoosterD,
            BoosterX,
        }

        public int Width => this.cells.GetLength(0);
        public int Height => this.cells.GetLength(1);

        public int StartX { get; }

        public int StartY { get; }

        public Cell this[int i, int j] => this.cells[i, j];

        public static Map FromAscii(int startX, int startY, params string[] lines)
        {
            var cells = new Cell[lines.Length, lines[0].Length];
            for (var i = 0; i < lines.Length; ++i)
            {
                if (lines[i].Length != lines[0].Length)
                {
                    throw new ArgumentException("All lines must be of equal length");
                }

                for (var j = 0; j < lines[i].Length; ++j)
                {
                    cells[i, j] = AsciiToCell(lines[i][j]);
                }
            }

            return new Map(startX, startY, cells);
        }

        private static Cell AsciiToCell(char c)
        {
            switch (c)
            {
                case ' ': return Cell.Empty;
                case '#': return Cell.Obstacle;
                case '!': return Cell.Edge;
                case 'B': return Cell.BoosterB;
                case 'F': return Cell.BoosterF;
                case 'D': return Cell.BoosterD;
                case 'X': return Cell.BoosterX;
                default:
                    throw new ArgumentOutOfRangeException($"Invalid cell ascii representation: {c}");
            }
        }
    }
}
