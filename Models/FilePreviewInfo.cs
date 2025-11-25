namespace TriDownload.Models;

/// <summary>
/// Informations sur un fichier dans l'aperçu de tri
/// </summary>
public class FilePreviewInfo
{
    public string FullPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string DestinationFolder { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
