using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using WindowsImageComparator.Abstractions;
using WindowsImageComparator.Enums;
using WindowsImageComparator.Models;
using static WindowsImageComparator.Models.ImageComparatorModel;

namespace WindowsImageComparator.Services
{
    public class ImageComparatorService : IImageComparatorService
    {
        private const int LineCount = 50;
        private const int DirectorySize = 0;
        private const string OutputPath = "results.txt";

        public void CompareImages(string baselinePath, string modifiedPath)
        {
            ValidateDirectoryExists(baselinePath, "Baseline path");
            ValidateDirectoryExists(modifiedPath, "Modified path");
            var differences = new List<ImageComparatorModel>();
            CompareDirectories(baselinePath, modifiedPath, differences);
            WriteDifferencesToFile(differences, OutputPath);
        }

        private static bool FilesAreDifferent(string file1, string file2)
        {
            return ComputeChecksum(file1) != ComputeChecksum(file2) || new FileInfo(file1).Length != new FileInfo(file2).Length;
        }

        private static string ComputeChecksum(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static void CompareFilesInDirectories(string baselineDir, string modifiedDir, List<ImageComparatorModel> differences)
        {
            Log.Info($"Starting file comparison for directories: {baselineDir} and {modifiedDir}");
            var baselineFiles = Directory.GetFiles(baselineDir);
            var modifiedFiles = Directory.GetFiles(modifiedDir);

            foreach (var baselineFile in baselineFiles)
            {
                var baselineFileName = Path.GetFileName(baselineFile);
                var counterpartFile = Path.Combine(modifiedDir, baselineFileName);
                if (!File.Exists(counterpartFile))
                {
                    Log.Info($"File removed: {baselineFile}");
                    var fileInfo = new FileInfo(baselineFile);
                    var checksum = ComputeChecksum(baselineFile);
                    differences.Add(new ImageComparatorModel(baselineFile, null, DifferenceType.Removed, ItemType.File, fileInfo.Length, checksum));
                }
                else
                {
                    if (!FilesAreDifferent(baselineFile, counterpartFile)) continue;
                    Log.Info($"File modified: {baselineFile}");
                    var fileInfo = new FileInfo(baselineFile);
                    var counterpartFileInfo = new FileInfo(counterpartFile);
                    var checksum = ComputeChecksum(baselineFile);
                    var counterpartChecksum = ComputeChecksum(counterpartFile);
                    differences.Add(new ImageComparatorModel(baselineFile, counterpartFile, DifferenceType.Modified, ItemType.File, fileInfo.Length, checksum, counterpartFileInfo.Length, counterpartChecksum));
                }
            }

            foreach (var modifiedFile in modifiedFiles)
            {
                var counterpartFile = Path.Combine(baselineDir, Path.GetFileName(modifiedFile));
                if (File.Exists(counterpartFile)) continue;
                Log.Info($"File added: {modifiedFile}");
                var fileInfo = new FileInfo(modifiedFile);
                var checksum = ComputeChecksum(modifiedFile);
                differences.Add(new ImageComparatorModel(modifiedFile, null, DifferenceType.Added, ItemType.File, fileInfo.Length, checksum));
            }
            Log.Info($"Completed file comparison for directories: {baselineDir} and {modifiedDir}");
        }

        private void CompareDirectories(string baselineDir, string modifiedDir, List<ImageComparatorModel> differences)
        {
            Log.Info($"Starting directory comparison: {baselineDir} vs {modifiedDir}");

            CompareFilesInDirectories(baselineDir, modifiedDir, differences);

            var baselineSubDirs = Directory.GetDirectories(baselineDir);
            var modifiedSubDirs = Directory.GetDirectories(modifiedDir);

            foreach (var baselineSubDir in baselineSubDirs)
            {
                var counterpartDir = Path.Combine(modifiedDir, new DirectoryInfo(baselineSubDir).Name);
                if (!Directory.Exists(counterpartDir))
                {
                    differences.Add(new ImageComparatorModel(baselineSubDir, null, DifferenceType.Removed, ItemType.Directory, DirectorySize, null));
                    AddAllFilesInDirectoryAsDifference(baselineSubDir, DifferenceType.Removed, differences);
                }
                else
                {
                    CompareDirectories(baselineSubDir, counterpartDir, differences);
                }
            }

            foreach (var modifiedSubDir in modifiedSubDirs)
            {
                var counterpartDir = Path.Combine(baselineDir, new DirectoryInfo(modifiedSubDir).Name);
                if (Directory.Exists(counterpartDir)) continue;
                differences.Add(new ImageComparatorModel(modifiedSubDir, null, DifferenceType.Added, ItemType.Directory, DirectorySize, null));
                AddAllFilesInDirectoryAsDifference(modifiedSubDir, DifferenceType.Added, differences);
            }

            Log.Info($"Completed directory comparison: {baselineDir} vs {modifiedDir}");
        }

        private static void AddAllFilesInDirectoryAsDifference(string dir, DifferenceType type, List<ImageComparatorModel> differences)
        {
            Log.Info($"Starting {type.ToString().ToLower()} directory comparison: {dir}");

            foreach (var file in Directory.GetFiles(dir))
            {
                var fileInfo = new FileInfo(file);
                var checksum = ComputeChecksum(file);
                differences.Add(new ImageComparatorModel(file, null, type, ItemType.File, fileInfo.Length, checksum));
                Log.Info($"File {type.ToString().ToLower()}: {file}");
            }

            foreach (var subDir in Directory.GetDirectories(dir))
            {
                differences.Add(new ImageComparatorModel(subDir, null, type, ItemType.Directory, DirectorySize, null));
                Log.Info($"Directory {type.ToString().ToLower()}: {subDir}");
                AddAllFilesInDirectoryAsDifference(subDir, type, differences);
            }
            Log.Info($"Completed {type.ToString().ToLower()} directory comparison: {dir}");
        }

        private static void ValidateDirectoryExists(string path, string descriptor)
        {
            if (!Directory.Exists(path))
            {
                Log.Error($"{descriptor} does not exist: {path}");
                throw new ArgumentException($"{descriptor} does not exist: {path}");
            }
            Log.Success($"{descriptor}: {path}");
        }

        private static void WriteDifferencesToFile(List<ImageComparatorModel> differences, string outputPath)
        {
            try
            {
                Log.Info($"Writing results to {outputPath}");
                using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
                foreach (var diff in differences)
                {
                    writer.WriteLine($"{diff.Kind}: {diff.Path}");
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
            Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
        }
    }
}
