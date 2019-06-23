namespace BlockhainAutomator
{
    using System.IO;

    using Icfpc2019.Solution;

    public class Program
    {
        private static void Main(string[] args)
        {
            var blockDir = args[0];

            var puzzleFile = Path.Combine(blockDir, "puzzle.cond");
            var puzzle = new Puzzle(File.ReadAllText(puzzleFile));
            File.WriteAllText($"{puzzleFile}.desc", puzzle.SaveToMap());
            puzzle.SaveToBitmap().Save($"{puzzleFile}.png");
        }
    }
}