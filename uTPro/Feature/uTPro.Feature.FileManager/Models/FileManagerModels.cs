namespace uTPro.Feature.FileManager.Models;

public class FileItemViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public string Extension { get; set; } = string.Empty;
}

public class FileContentResult
{
    public IEnumerable<FileItemViewModel> Items { get; set; } = [];
    public string CurrentPath { get; set; } = string.Empty;
    public string? ParentPath { get; set; }
}

public class CreateFolderRequest
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class RenameRequest
{
    public string Path { get; set; } = string.Empty;
    public string NewName { get; set; } = string.Empty;
}

public class DeleteRequest
{
    public string Path { get; set; } = string.Empty;
}

public class ListRequest
{
    public string Path { get; set; } = string.Empty;
}

public class ReadFileRequest
{
    public string Path { get; set; } = string.Empty;
}

public class SaveFileRequest
{
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ReadFileResponse
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
}
