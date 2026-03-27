using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using uTPro.Feature.FileManager.Models;
using uTPro.Feature.FileManager.Services;

namespace uTPro.Feature.FileManager.Controllers;

[VersionedApiBackOfficeRoute("utpro/file-manager")]
[ApiExplorerSettings(GroupName = "uTPro File Manager")]
public class FileManagerApiController(IFileManagerService fileManagerService) : ManagementApiControllerBase
{
    [HttpPost("list")]
    public IActionResult List([FromBody] ListRequest? request)
        => Ok(fileManagerService.GetDirectoryContents(request?.Path ?? ""));

    [HttpPost("create-folder")]
    public IActionResult CreateFolder([FromBody] CreateFolderRequest request)
    {
        var (success, message) = fileManagerService.CreateFolder(request.Path, request.Name);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPost("upload")]
    public IActionResult Upload([FromQuery] string path, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        using var stream = file.OpenReadStream();
        var (success, result) = fileManagerService.SaveUploadedFile(path ?? "", file.FileName, stream);
        return success ? Ok(new { path = result }) : BadRequest(new { message = result });
    }

    [HttpPost("download")]
    public IActionResult Download([FromBody] DeleteRequest request)
    {
        var (success, fullPath, contentType) = fileManagerService.GetFileForDownload(request.Path);
        if (!success) return NotFound(new { message = fullPath });

        var fileName = Path.GetFileName(fullPath);
        return PhysicalFile(fullPath, contentType, fileName);
    }

    [HttpPost("delete")]
    public IActionResult Delete([FromBody] DeleteRequest request)
    {
        var (success, message) = fileManagerService.Delete(request.Path);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPost("rename")]
    public IActionResult Rename([FromBody] RenameRequest request)
    {
        var (success, message) = fileManagerService.Rename(request.Path, request.NewName);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPost("read-file")]
    public IActionResult ReadFile([FromBody] ReadFileRequest request)
    {
        var (success, data, message) = fileManagerService.ReadTextFile(request.Path);
        return success ? Ok(data) : BadRequest(new { message });
    }

    [HttpPost("save-file")]
    public IActionResult SaveFile([FromBody] SaveFileRequest request)
    {
        var (success, message) = fileManagerService.SaveTextFile(request.Path, request.Content);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }
}
