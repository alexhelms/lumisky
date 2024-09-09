using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OdinEye.Controllers;

[ApiController]
[Route("api/video")]
public class VideoController : Controller
{
    private readonly AppDbContext _appDbContext;

    public VideoController(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    private IActionResult GetActionResultForImage(string path, bool downloadFile)
    {
        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists)
            return NotFound();

        var filename = Path.GetFileName(fileInfo.FullName);
        var extension = fileInfo.Extension.ToLowerInvariant();
        var contentType = "video/mp4";

        if (downloadFile)
        {
            return PhysicalFile(fileInfo.FullName, contentType, filename);
        }
        else
        {
            return PhysicalFile(fileInfo.FullName, contentType);
        }
    }

    [HttpGet("download/{type}")]
    public async Task<IActionResult> GetDownload(
        [FromRoute] string type,
        [FromQuery(Name = "id")] int id)
    {
        string filename;

        if (type == "timelapse")
        {
            var timelapse = await _appDbContext.Timelapses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (timelapse is null)
                return NotFound();

            filename = timelapse.Filename;
        }
        else if (type == "panorama")
        {
            var panorama = await _appDbContext.PanoramaTimelapses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (panorama is null)
                return NotFound();

            filename = panorama.Filename;
        }
        else
        {
            return NotFound();
        }

        return GetActionResultForImage(filename, downloadFile: true);
    }
}
