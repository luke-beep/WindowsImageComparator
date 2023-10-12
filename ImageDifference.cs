using System.Security.Cryptography;
using System.Text;

namespace WindowsImageComparator
{
    public class ImageDifference
    {
        private const int LineCount = 50;

        public string FilePath { get; }
        public string? CounterpartPath { get; }
        public DifferenceType Type { get; }
        public ItemType Kind { get; }
        public long Size => Kind == ItemType.File ? new FileInfo(FilePath).Length : 0;
        public long? CounterpartSize => CounterpartPath != null && Kind == ItemType.File ? new FileInfo(CounterpartPath).Length : null;
        public string Checksum { get; }
        public string? CounterpartChecksum { get; }
        public ImageDifference(string filepath, string? counterpartPath, DifferenceType type, ItemType kind)
        {
            FilePath = filepath;
            CounterpartPath = counterpartPath;
            Type = type;
            Kind = kind;
            Checksum = Kind == ItemType.File ? ComputeChecksum(filepath) : "";
            CounterpartChecksum = counterpartPath != null && Kind == ItemType.File ? ComputeChecksum(counterpartPath) : null;
        }
        public ImageDifference(string path, DifferenceType type, ItemType kind) : this(path, null, type, kind) { }

        private static string ComputeChecksum(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static bool FilesAreDifferent(string file1, string file2)
        {
            return ComputeChecksum(file1) != ComputeChecksum(file2) || new FileInfo(file1).Length != new FileInfo(file2).Length;
        }

        private static void CompareFilesInDirectories(string baselineDir, string modifiedDir, List<ImageDifference> differences)
        {
            var baselineFiles = Directory.GetFiles(baselineDir);
            var modifiedFiles = Directory.GetFiles(modifiedDir);

            foreach (var baselineFile in baselineFiles)
            {
                var counterpartFile = Path.Combine(modifiedDir, Path.GetFileName(baselineFile));
                if (!File.Exists(counterpartFile))
                {
                    Log.Info($"File removed: {baselineFile}");
                    differences.Add(new ImageDifference(baselineFile, DifferenceType.Removed, ItemType.File));
                }
                else
                {
                    if (!FilesAreDifferent(baselineFile, counterpartFile)) continue;
                    Log.Info($"File modified: {baselineFile}");
                    differences.Add(new ImageDifference(baselineFile, counterpartFile, DifferenceType.Modified, ItemType.File));
                }
            }

            foreach (var modifiedFile in modifiedFiles)
            {
                var counterpartFile = Path.Combine(baselineDir, Path.GetFileName(modifiedFile));
                if (File.Exists(counterpartFile)) continue;
                Log.Info($"File added: {modifiedFile}");
                differences.Add(new ImageDifference(modifiedFile, DifferenceType.Added, ItemType.File));
            }
        }


        public static List<ImageDifference> CompareImages(string baselinePath, string modifiedPath)
        {
            var differences = new List<ImageDifference>();
            CompareDirectories(baselinePath, modifiedPath, differences);
            return differences;
        }

        private static void CompareDirectories(string baselineDir, string modifiedDir, List<ImageDifference> differences)
        {
            if (!Directory.Exists(baselineDir))
            {
                Log.Info($"Directory added: {modifiedDir}");
                AddAllFilesInDirectoryAsDifference(modifiedDir, DifferenceType.Added, differences, ItemType.Directory);
                return;
            }

            if (!Directory.Exists(modifiedDir))
            {
                Log.Info($"Directory removed: {baselineDir}");
                AddAllFilesInDirectoryAsDifference(baselineDir, DifferenceType.Removed, differences, ItemType.Directory);
                return;
            }

            CompareFilesInDirectories(baselineDir, modifiedDir, differences);
            CompareSubDirectories(baselineDir, modifiedDir, differences);
        }

        private static void AddAllFilesInDirectoryAsDifference(string dir, DifferenceType type, List<ImageDifference> differences, ItemType kind)
        {
            foreach (var file in Directory.GetFiles(dir))
            {
                differences.Add(new ImageDifference(file, null, type, kind));
                Log.Info($"File {type.ToString().ToLower()}: {file}");
            }

            foreach (var subDir in Directory.GetDirectories(dir))
            {
                AddAllFilesInDirectoryAsDifference(subDir, type, differences, ItemType.Directory);
                Log.Info($"Directory {type.ToString().ToLower()}: {subDir}");
            }
        }

        private static void CompareSubDirectories(string baselineDir, string modifiedDir, List<ImageDifference> differences)
        {
            foreach (var baselineSubDir in Directory.GetDirectories(baselineDir))
            {
                var counterpartDir = Path.Combine(modifiedDir, new DirectoryInfo(baselineSubDir).Name);
                if (!Directory.Exists(counterpartDir))
                {
                    differences.Add(new ImageDifference(baselineSubDir, DifferenceType.Removed, ItemType.Directory));
                }
                CompareDirectories(baselineSubDir, counterpartDir, differences);
            }

            foreach (var modifiedSubDir in Directory.GetDirectories(modifiedDir))
            {
                var counterpartDir = Path.Combine(baselineDir, new DirectoryInfo(modifiedSubDir).Name);
                if (!Directory.Exists(counterpartDir))
                {
                    differences.Add(new ImageDifference(modifiedSubDir, DifferenceType.Added, ItemType.Directory));
                }
            }
        }

        public static void ValidateDirectoryExists(string path, string descriptor)
        {
            if (!Directory.Exists(path))
            {
                Log.Error($"{descriptor} does not exist: {path}");
                Environment.Exit(1);
            }
            else
            {
                Log.Success($"{descriptor}: {path}");
            }
        }

        public static void WriteDifferencesToFile(List<ImageDifference> differences, string outputPath)
        {
            try
            {
                using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
                foreach (var diff in differences)
                {
                    writer.WriteLine($"{diff.Kind}: {diff.FilePath}");
                    writer.WriteLine($"Type: {diff.Type}");

                    if (diff.Kind == ItemType.File)
                    {
                        writer.WriteLine($"Size: {diff.Size} bytes");
                        writer.WriteLine($"Checksum: {diff.Checksum}");
                    }

                    if (diff is { Type: DifferenceType.Modified, CounterpartPath: not null })
                    {
                        writer.WriteLine($"Modified {diff.Kind}: {diff.CounterpartPath}");
                        if (diff.Kind == ItemType.File)
                        {
                            writer.WriteLine($"Modified Size: {diff.CounterpartSize} bytes");
                            writer.WriteLine($"Modified Checksum: {diff.CounterpartChecksum}");
                        }
                    }

                    writer.WriteLine(new string('-', LineCount));
                }
                Log.Success($"Comparison completed. Results written to {outputPath}");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to write results to {outputPath}: {e.Message}");
                throw;
            }

        }

        public enum ItemType
        {
            File,
            Directory
        }
        public enum DifferenceType
        {
            Added,
            Removed,
            Modified
        }
    }
}

