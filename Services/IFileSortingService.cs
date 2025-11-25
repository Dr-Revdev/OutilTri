using TriDownload.Models;

namespace TriDownload.Services;

/// <summary>
/// Service de tri de fichiers
/// </summary>
public interface IFileSortingService
{
    IReadOnlyList<SortingRule> Rules { get; }

    void LoadRules();
    void SaveRules(List<SortingRule> rules);
    void InitializeDefaultRules();
    string SuggestDestinationFolder(string extension);
    List<FilePreviewInfo> PreviewFiles(string rootFolder);
    SortingResult ApplySorting(string rootFolder, List<FilePreviewInfo> files);
    UndoResult UndoSorting(List<(string CurrentPath, string OriginalPath)> movements);
}
