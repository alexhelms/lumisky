using Microsoft.AspNetCore.Mvc;

namespace OdinEye.Controllers;

[ApiController]
[Route("api/logs")]
public class LogController : Controller
{
    [HttpGet("download")]
    public IActionResult DownloadLog()
    {
        var path = CaptureLogFilePathHook.Path;
        if (path is null)
            return NotFound();

        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists)
            return NotFound();

        var filename = Path.GetFileName(fileInfo.FullName);
        var extension = fileInfo.Extension.ToLowerInvariant();
        return PhysicalFile(fileInfo.FullName, "text/plain", filename);
    }
}
