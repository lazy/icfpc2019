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
            ManipulatorExtension,
            FastWheels,
            Drill,
            MysteriousPoint,
        }

        public int Width => this.cells.GetLength(0);
        public int Height => this.cells.GetLength(1);

        public int StartX { get; }

        public int StartY { get; }

        public Cell this[int x, int y] => this.cells[x, y];

        public static Map FromAscii(params string[] lines)
        {
            // matrix is transposed, because our coords are (x, y) but
            // visual coords are (y, x)
            var cells = new Cell[lines[0].Length, lines.Length];

            (int, int)? startPosition = null;

            // Turn map upside down and transpose so we can use convenient coords
            for (var reverseY = 0; reverseY < lines.Length; ++reverseY)
            {
                var y = lines.Length - reverseY - 1;
                var line = lines[reverseY];
                if (line.Length != lines[0].Length)
                {
                    throw new ArgumentException("All lines must be of equal length");
                }

                for (var x = 0; x < line.Length; ++x)
                {
                    if (line[x] == 'v')
                    {
                        if (startPosition != null)
                        {
                            throw new ArgumentException("More than one start position found");
                        }

                        startPosition = (x, y);
                    }
                    else
                    {
                        cells[x, y] = AsciiToCell(line[x]);
                    }
                }
            }

            var (startX, startY) = startPosition ?? throw new ArgumentException("Initial position was not found");

            return new Map(startX, startY, cells);
        }

        public bool IsFree(int x, int y)
        {
            if (x < 0 || y < 0)
            {
                return false;
            }

            var cell = this[x, y];
            return cell != Cell.Edge && cell != Cell.Obstacle;
        }

        public override string ToString()
        {
            var rows = new List<string>();

            for (var y = this.Height - 1; y >= 0; --y)
            {
                var currentRow = new StringBuilder();
                for (var x = 0; x < this.Width; ++x)
                {
                    var c = (x, y) == (this.StartX, this.StartY)
                        ? 'v'
                        : CellToAscii(this[x, y]);
                    currentRow.Append(c);
                }

                rows.Add(currentRow.ToString());
            }

            return string.Join("\n", rows);
        }

        private static Cell AsciiToCell(char c) =>
            c switch
                {
                '.' => Cell.Empty,
                '#' => Cell.Obstacle,
                'x' => Cell.Edge,
                'B' => Cell.ManipulatorExtension,
                'F' => Cell.FastWheels,
                'L' => Cell.Drill,
                'X' => Cell.MysteriousPoint,
                _ => throw new ArgumentOutOfRangeException($"Invalid cell ascii representation: {c}"),
                };

        private static char CellToAscii(Cell cell) =>
            cell switch
                {
                Cell.Empty => '.',
                Cell.Obstacle => '#',
                Cell.Edge => 'x',
                Cell.ManipulatorExtension => 'B',
                Cell.FastWheels => 'F',
                Cell.Drill => 'L',
                Cell.MysteriousPoint => 'X',
                _ => throw new Exception($"Invalid enum value: {cell}"),
                };
    }
}