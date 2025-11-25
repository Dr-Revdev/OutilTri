using System.Text.Json;
using TriDownload.Models;

namespace TriDownload.Services;

/// <summary>
/// Service de tri de fichiers basé sur des règles configurables
/// </summary>
public class FileSortingService : IFileSortingService
{
    private List<SortingRule> _rules = new();

    public IReadOnlyList<SortingRule> Rules => _rules;

    private static string GetRulesFilePath()
    {
        var exeDirectory = AppContext.BaseDirectory;
        return Path.Combine(exeDirectory, "rules.json");
    }

    public void LoadRules()
    {
        var path = GetRulesFilePath();
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                _rules = JsonSerializer.Deserialize<List<SortingRule>>(json) ?? new();
            }
            catch
            {
                InitializeDefaultRules();
            }
        }
        else
        {
            InitializeDefaultRules();
        }
    }

    public void InitializeDefaultRules()
    {
        _rules = new List<SortingRule>
        {
            new() { Extensions = "jpg,jpeg,png,gif,bmp,webp,tiff,heic", DestinationFolder = "Images" },
            new() { Extensions = "pdf,doc,docx,txt,rtf,odt,xls,xlsx,csv,ppt,pptx", DestinationFolder = "Documents" },
            new() { Extensions = "mp3,wav,flac,m4a,ogg,aac", DestinationFolder = "Audio" },
            new() { Extensions = "mp4,mkv,avi,mov,wmv,webm", DestinationFolder = "Vidéos" },
            new() { Extensions = "zip,7z,rar,tar,gz,bz2", DestinationFolder = "Archives" },
            new() { Extensions = "exe,msi,msix,bat,cmd,ps1", DestinationFolder = "Installateurs" }
        };
    }

    public void SaveRules(List<SortingRule> rules)
    {
        _rules = rules;
        var json = JsonSerializer.Serialize(_rules, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(GetRulesFilePath(), json);
    }

    public string SuggestDestinationFolder(string extension)
    {
        if (string.IsNullOrEmpty(extension)) 
            return "_SansExtension";
        
        extension = extension.ToLowerInvariant();

        foreach (var rule in _rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Extensions)) 
                continue;
            
            var extensions = rule.Extensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (Array.Exists(extensions, e => e.ToLowerInvariant() == extension))
                return rule.DestinationFolder;
        }

        return "_Autres";
    }

    public List<FilePreviewInfo> PreviewFiles(string rootFolder)
    {
        var result = new List<FilePreviewInfo>();

        if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder))
            return result;

        foreach (var path in Directory.EnumerateFiles(rootFolder, "*", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(path);
            var extension = Path.GetExtension(path);
            if (extension.StartsWith(".")) 
                extension = extension[1..];

            var destinationFolder = SuggestDestinationFolder(extension);
            
            result.Add(new FilePreviewInfo
            {
                FullPath = path,
                FileName = fileName,
                Extension = extension,
                DestinationFolder = destinationFolder,
                Status = string.Empty
            });
        }

        return result;
    }

    public SortingResult ApplySorting(string rootFolder, List<FilePreviewInfo> files)
    {
        var result = new SortingResult();

        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.FullPath))
            {
                file.Status = "Erreur";
                result.FailedCount++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(file.DestinationFolder))
            {
                file.Status = "_";
                continue;
            }

            try
            {
                var destinationDirectory = Path.Combine(rootFolder, file.DestinationFolder);
                var sourceDirectory = Path.GetDirectoryName(file.FullPath);
                
                if (string.Equals(sourceDirectory ?? string.Empty, destinationDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    file.Status = "Déjà";
                    continue;
                }

                Directory.CreateDirectory(destinationDirectory);
                var destinationPath = Path.Combine(destinationDirectory, Path.GetFileName(file.FullPath));
                destinationPath = EnsureUniqueFilePath(destinationPath);

                File.Move(file.FullPath, destinationPath);
                file.Status = "OK";
                result.Movements.Add((destinationPath, file.FullPath));
                result.MovedCount++;
            }
            catch
            {
                file.Status = "Erreur";
                result.FailedCount++;
            }
        }

        return result;
    }

    public UndoResult UndoSorting(List<(string CurrentPath, string OriginalPath)> movements)
    {
        var result = new UndoResult();

        for (int i = movements.Count - 1; i >= 0; i--)
        {
            var (currentPath, originalPath) = movements[i];
            try
            {
                if (!File.Exists(currentPath))
                {
                    result.FailedCount++;
                    continue;
                }

                var originalDirectory = Path.GetDirectoryName(originalPath);
                if (!string.IsNullOrEmpty(originalDirectory))
                    Directory.CreateDirectory(originalDirectory);
                
                var restoredPath = EnsureUniqueFilePath(originalPath);
                File.Move(currentPath, restoredPath);
                result.RestoredCount++;
            }
            catch
            {
                result.FailedCount++;
            }
        }

        return result;
    }

    private static string EnsureUniqueFilePath(string filePath)
    {
        if (!File.Exists(filePath)) 
            return filePath;
        
        var directory = Path.GetDirectoryName(filePath)!;
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        
        int counter = 1;
        string candidatePath;
        
        do
        {
            candidatePath = Path.Combine(directory, $"{fileName} ({counter++}){extension}");
        }
        while (File.Exists(candidatePath));
        
        return candidatePath;
    }
}
