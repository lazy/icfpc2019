namespace Solution
{
    public struct Map
    {
        private readonly Cell[,] cells;

        public Map(Cell[,] cells)
        {
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

        public Cell this[int i, int j] => this.cells[i, j];
    }
}
