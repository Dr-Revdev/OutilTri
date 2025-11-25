namespace TriDownload.Models;

/// <summary>
/// Résultat d'une opération de tri
/// </summary>
public class SortingResult
{
    public int MovedCount { get; set; }
    public int FailedCount { get; set; }
    public List<(string CurrentPath, string OriginalPath)> Movements { get; set; } = new();
}
