namespace Icfpc2019.Solution
{
    using System;

    using System.Collections.Generic;
    using System.Text;

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

        public int Width => this.cells.GetLength(1);
        public int Height => this.cells.GetLength(0);

        public int StartX { get; }

        public int StartY { get; }

        public Cell this[int i, int j] => this.cells[i, j];

        public static Map FromAscii(int startX, int startY, params string[] lines)
        {
            var cells = new Cell[lines.Length, lines[0].Length];

            // Turn map upside down so we can use convenient coords
            for (var i = lines.Length - 1; i >= 0; --i)
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

        public override string ToString()
        {
            var rows = new List<string>();

            for (var y = this.Height - 1; y >= 0; --y)
            {
                var currentRow = new StringBuilder();
                for (var x = 0; x < this.Width; ++x)
                {
                    currentRow.Append(this.CellToAscii(x, y));
                }

                rows.Add(currentRow.ToString());
            }

            return string.Join("\n", rows);
        }

        private static Cell AsciiToCell(char c)
        {
            switch (c)
            {
                case '.': return Cell.Empty;
                case '#': return Cell.Obstacle;
                case 'x': return Cell.Edge;
                case 'B': return Cell.BoosterB;
                case 'F': return Cell.BoosterF;
                case 'D': return Cell.BoosterD;
                case 'X': return Cell.BoosterX;
                default:
                    throw new ArgumentOutOfRangeException($"Invalid cell ascii representation: {c}");
            }
        }

        private char CellToAscii(int x, int y)
        {
            if (x == this.StartX && y == this.StartY)
            {
                return 'v';
            }

            var cell = this[x, y];
            return cell switch
                {
                Cell.Empty => '.',
                Cell.Obstacle => '#',
                Cell.Edge => 'x',
                Cell.BoosterB => 'B',
                Cell.BoosterF => 'F',
                Cell.BoosterD => 'D',
                Cell.BoosterX => 'X',
                _ => throw new Exception(string.Format("Invalid enum value: {}", cell)),
                };
        }
    }
}