namespace TriDownload.Models;

/// <summary>
/// Règle de tri : extensions → dossier de destination
/// </summary>
public class SortingRule
{
    /// <summary>
    /// Extensions de fichiers séparées par virgule (ex: "jpg,png,gif")
    /// </summary>
    public string Extensions { get; set; } = string.Empty;

    /// <summary>
    /// Nom du dossier de destination
    /// </summary>
    public string DestinationFolder { get; set; } = string.Empty;
}
