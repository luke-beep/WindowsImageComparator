using CommandLine;

namespace WindowsImageComparator.Models;

public class Options
{
    [Value(0, Required = true, HelpText = "Path to baseline images.")]
    public required string BaselinePath { get; set; }

    [Value(1, Required = true, HelpText = "Path to modified images.")]
    public required string ModifiedPath { get; set; }
}