namespace TriDownload.Models;

/// <summary>
/// Résultat d'une opération d'annulation
/// </summary>
public class UndoResult
{
    public int RestoredCount { get; set; }
    public int FailedCount { get; set; }
}
