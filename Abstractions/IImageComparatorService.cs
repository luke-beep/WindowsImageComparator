using WindowsImageComparator.Models;

namespace WindowsImageComparator.Abstractions;

public interface IImageComparatorService
{
    public void CompareImages(string baselinePath, string modifiedPath);
}