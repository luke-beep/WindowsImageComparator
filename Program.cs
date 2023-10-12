using static WindowsImageComparator.ImageDifference;

namespace WindowsImageComparator
{
    internal class Program
    {
        private const string OutputPath = "differences.txt";

        private static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Log.Warning("Usage: WindowsImageComparator.exe <baseline_path> <modified_path>");
                Environment.Exit(1);
            }

            var baselinePath = args[0];
            var modifiedPath = args[1];

            ValidateDirectoryExists(baselinePath, "Baseline path");
            ValidateDirectoryExists(modifiedPath, "Modified path");

            var differences = CompareImages(baselinePath, modifiedPath);

            WriteDifferencesToFile(differences, OutputPath);
        }
    }
}