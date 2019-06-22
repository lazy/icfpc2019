namespace BlockhainAutomator
{
    using System.IO;

    using Icfpc2019.Solution;

    public class Program
    {
        private static void Main(string[] args)
        {
            var puzzleFile = args[0];
            var puzzle = new Puzzle(File.ReadAllText(puzzleFile));
            puzzle.SaveToBitmap().Save($"{puzzleFile}.png");
        }
    }
}