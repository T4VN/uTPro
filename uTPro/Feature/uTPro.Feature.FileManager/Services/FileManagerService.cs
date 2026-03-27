using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using uTPro.Feature.FileManager.Models;

namespace uTPro.Feature.FileManager.Services;

class DIFileManagerService : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IFileManagerService, FileManagerService>();
}

public interface IFileManagerService
{
    FileContentResult GetDirectoryContents(string relativePath);
    (bool Success, string Message) CreateFolder(string relativePath, string name);
    (bool Success, string Message) Delete(string relativePath);
    (bool Success, string Message) Rename(string relativePath, string newName);
    (bool Success, string FilePath) SaveUploadedFile(string relativePath, string fileName, Stream stream);
    (bool Success, string FullPath, string ContentType) GetFileForDownload(string relativePath);
    (bool Success, ReadFileResponse? Data, string Message) ReadTextFile(string relativePath);
    (bool Success, string Message) SaveTextFile(string relativePath, string content);
}

internal class FileManagerService(
    IWebHostEnvironment env,
    ILogger<FileManagerService> logger
) : IFileManagerService
{
    // Restrict browsing to wwwroot only
    private string RootPath => env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");

    private static readonly HashSet<string> BlockedExtensions =
    [
        ".exe", ".dll", ".bat", ".cmd", ".ps1", ".sh", ".msi",
        ".com", ".scr", ".pif", ".vbs", ".wsf", ".cpl"
    ];

    private string? ResolveSafePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            relativePath = "";

        // Normalize separators and remove leading slashes
        relativePath = relativePath.Replace('\\', '/').TrimStart('/');

        var fullPath = Path.GetFullPath(Path.Combine(RootPath, relativePath));

        // Prevent path traversal
        if (!fullPath.StartsWith(RootPath, StringComparison.OrdinalIgnoreCase))
            return null;

        return fullPath;
    }

    public FileContentResult GetDirectoryContents(string relativePath)
    {
        var fullPath = ResolveSafePath(relativePath);
        if (fullPath == null || !Directory.Exists(fullPath))
            return new FileContentResult { CurrentPath = relativePath, Items = [] };

        var items = new List<FileItemViewModel>();

        foreach (var dir in Directory.GetDirectories(fullPath))
        {
            var info = new DirectoryInfo(dir);
            items.Add(new FileItemViewModel
            {
                Name = info.Name,
                Path = Path.GetRelativePath(RootPath, dir).Replace('\\', '/'),
                IsDirectory = true,
                LastModified = info.LastWriteTimeUtc
            });
        }

        foreach (var file in Directory.GetFiles(fullPath))
        {
            var info = new FileInfo(file);
            items.Add(new FileItemViewModel
            {
                Name = info.Name,
                Path = Path.GetRelativePath(RootPath, file).Replace('\\', '/'),
                IsDirectory = false,
                Size = info.Length,
                LastModified = info.LastWriteTimeUtc,
                Extension = info.Extension.TrimStart('.')
            });
        }

        // Calculate parent path
        string? parentPath = null;
        var rel = Path.GetRelativePath(RootPath, fullPath).Replace('\\', '/');
        if (rel != "." && rel.Contains('/'))
            parentPath = rel[..rel.LastIndexOf('/')];
        else if (rel != ".")
            parentPath = "";

        return new FileContentResult
        {
            CurrentPath = rel == "." ? "" : rel,
            ParentPath = parentPath,
            Items = items.OrderByDescending(x => x.IsDirectory).ThenBy(x => x.Name)
        };
    }

    public (bool Success, string Message) CreateFolder(string relativePath, string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return (false, "Invalid folder name");

            var basePath = ResolveSafePath(relativePath);
            if (basePath == null) return (false, "Invalid path");

            var newPath = Path.Combine(basePath, name);
            if (!newPath.StartsWith(RootPath, StringComparison.OrdinalIgnoreCase))
                return (false, "Access denied");

            if (Directory.Exists(newPath))
                return (false, "Folder already exists");

            Directory.CreateDirectory(newPath);
            return (true, "Folder created");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating folder {Name} at {Path}", name, relativePath);
            return (false, ex.Message);
        }
    }

    public (bool Success, string Message) Delete(string relativePath)
    {
        try
        {
            var fullPath = ResolveSafePath(relativePath);
            if (fullPath == null) return (false, "Invalid path");
            if (fullPath.Equals(RootPath, StringComparison.OrdinalIgnoreCase))
                return (false, "Cannot delete root directory");

            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
                return (true, "Deleted");
            }
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
                return (true, "Deleted");
            }
            return (false, "Not found");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting {Path}", relativePath);
            return (false, ex.Message);
        }
    }

    public (bool Success, string Message) Rename(string relativePath, string newName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(newName) || newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return (false, "Invalid name");

            var fullPath = ResolveSafePath(relativePath);
            if (fullPath == null) return (false, "Invalid path");

            var parentDir = Path.GetDirectoryName(fullPath)!;
            var newPath = Path.Combine(parentDir, newName);
            if (!newPath.StartsWith(RootPath, StringComparison.OrdinalIgnoreCase))
                return (false, "Access denied");

            if (Directory.Exists(fullPath))
            {
                Directory.Move(fullPath, newPath);
                return (true, "Renamed");
            }
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Move(fullPath, newPath);
                return (true, "Renamed");
            }
            return (false, "Not found");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error renaming {Path}", relativePath);
            return (false, ex.Message);
        }
    }

    public (bool Success, string FilePath) SaveUploadedFile(string relativePath, string fileName, Stream stream)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName)) return (false, "No file name");

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (BlockedExtensions.Contains(ext))
                return (false, $"File type {ext} is not allowed");

            var basePath = ResolveSafePath(relativePath);
            if (basePath == null || !Directory.Exists(basePath)) return (false, "Invalid path");

            var filePath = Path.Combine(basePath, fileName);
            if (!filePath.StartsWith(RootPath, StringComparison.OrdinalIgnoreCase))
                return (false, "Access denied");

            using var fs = new FileStream(filePath, FileMode.Create);
            stream.CopyTo(fs);

            return (true, Path.GetRelativePath(RootPath, filePath).Replace('\\', '/'));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading {FileName} to {Path}", fileName, relativePath);
            return (false, ex.Message);
        }
    }

    public (bool Success, string FullPath, string ContentType) GetFileForDownload(string relativePath)
    {
        var fullPath = ResolveSafePath(relativePath);
        if (fullPath == null || !System.IO.File.Exists(fullPath))
            return (false, "Not found", "");

        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".html" or ".htm" => "text/html",
            ".txt" or ".md" or ".csv" => "text/plain",
            _ => "application/octet-stream"
        };

        return (true, fullPath, contentType);
    }

    private static readonly HashSet<string> EditableExtensions =
    [
        ".js", ".css", ".html", ".htm", ".json", ".xml", ".txt", ".md",
        ".csv", ".svg", ".cshtml", ".cs", ".config", ".yaml", ".yml",
        ".less", ".scss", ".sass", ".ts", ".map", ".env", ".gitignore"
    ];

    public (bool Success, ReadFileResponse? Data, string Message) ReadTextFile(string relativePath)
    {
        try
        {
            var fullPath = ResolveSafePath(relativePath);
            if (fullPath == null || !System.IO.File.Exists(fullPath))
                return (false, null, "File not found");

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            if (!EditableExtensions.Contains(ext))
                return (false, null, $"File type '{ext}' is not editable");

            var info = new FileInfo(fullPath);
            if (info.Length > 5 * 1024 * 1024) // 5MB limit
                return (false, null, "File too large to edit (max 5MB)");

            var content = System.IO.File.ReadAllText(fullPath);
            return (true, new ReadFileResponse
            {
                Path = Path.GetRelativePath(RootPath, fullPath).Replace('\\', '/'),
                Name = info.Name,
                Content = content,
                Extension = ext.TrimStart('.')
            }, "OK");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading file {Path}", relativePath);
            return (false, null, ex.Message);
        }
    }

    public (bool Success, string Message) SaveTextFile(string relativePath, string content)
    {
        try
        {
            var fullPath = ResolveSafePath(relativePath);
            if (fullPath == null || !System.IO.File.Exists(fullPath))
                return (false, "File not found");

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            if (!EditableExtensions.Contains(ext))
                return (false, $"File type '{ext}' is not editable");

            System.IO.File.WriteAllText(fullPath, content);
            return (true, "File saved");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving file {Path}", relativePath);
            return (false, ex.Message);
        }
    }
}
