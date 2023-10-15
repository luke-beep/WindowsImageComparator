using CommandLine;
using WindowsImageComparator.Abstractions;
using WindowsImageComparator.Models;
using WindowsImageComparator.Services;

namespace WindowsImageComparator
{
    internal class Program
    {
        private static void Main(string?[] args) =>
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunComparison);

        private static void RunComparison(Options options)
        {
            IImageComparatorService comparatorService = new ImageComparatorService();
            comparatorService.CompareImages(options.BaselinePath, options.ModifiedPath);
        }
    }
}