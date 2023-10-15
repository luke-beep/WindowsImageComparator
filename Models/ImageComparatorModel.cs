using WindowsImageComparator.Enums;

namespace WindowsImageComparator.Models;

public class ImageComparatorModel
{
    public string Path { get; }
    public string? CounterpartPath { get; }
    public DifferenceType Type { get; }
    public ItemType Kind { get; }
    public long Size { get; }
    public long? CounterpartSize { get; }
    public string? Checksum { get; }
    public string? CounterpartChecksum { get; }

    public ImageComparatorModel(string path, string? counterpartPath, DifferenceType type, ItemType kind, long size,
        string? checksum, long? counterpartSize = null, string? counterpartChecksum = null)
    {
        Path = path;
        CounterpartPath = counterpartPath;
        Type = type;
        Kind = kind;
        Size = size;
        Checksum = checksum;
        CounterpartSize = counterpartSize;
        CounterpartChecksum = counterpartChecksum;
    }

    public ImageComparatorModel(string path, DifferenceType type, ItemType kind, long size, string checksum)
        : this(path, null, type, kind, size, checksum)
    {
    }
}