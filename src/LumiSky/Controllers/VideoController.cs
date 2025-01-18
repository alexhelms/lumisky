using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LumiSky.Controllers;

[ApiController]
[Route("api/video")]
public class VideoController : Controller
{
    private readonly AppDbContext _dbContext;

    public VideoController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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
            var timelapse = await _dbContext.Timelapses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (timelapse is null)
                return NotFound();

            filename = timelapse.Filename;
        }
        else if (type == "panorama")
        {
            var panorama = await _dbContext.PanoramaTimelapses
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

    [HttpGet("latest/{type}/{timeOfDay}")]
    public async Task<IActionResult> GetLatest(
        [FromRoute] string type,
        [FromRoute] string timeOfDay)
    {
        string? filename = null;

        if (type == "timelapse")
        {
            var timelapse = await _dbContext.Timelapses
                .AsNoTracking()
                .OrderByDescending(x => x.RangeEnd)
                .FirstOrDefaultAsync();
            if (timelapse is null)
                return NotFound();
            
            string dayMatch = "videotimelapseday"; // video/timelapse/day but without dir seperators
            string filenameNoSeps = timelapse.Filename.Replace("/", "").Replace("\\", "");
            bool isDay = filenameNoSeps.Contains(dayMatch);

            if (timeOfDay == "day" && isDay)
            {
                filename = timelapse.Filename;
            }
            else if (timeOfDay == "night" && !isDay)
            {
                filename = timelapse.Filename;
            }
            else
            {
                return BadRequest();
            }
        }
        else if (type == "panorama")
        {
            var timelapse = await _dbContext.PanoramaTimelapses
                .AsNoTracking()
                .OrderByDescending(x => x.RangeEnd)
                .FirstOrDefaultAsync();
            if (timelapse is null)
                return NotFound();

            string dayMatch = "videotimelapseday"; // video/timelapse/day but without dir seperators
            string filenameNoSeps = timelapse.Filename.Replace("/", "").Replace("\\", "");
            bool isDay = filenameNoSeps.Contains(dayMatch);

            if (timeOfDay == "day" && isDay)
            {
                filename = timelapse.Filename;
            }
            else if (timeOfDay == "night" && !isDay)
            {
                filename = timelapse.Filename;
            }
            else
            {
                return BadRequest();
            }
        }

        if (filename is null)
            return BadRequest();

        var fileInfo = new FileInfo(filename);
        if (!fileInfo.Exists)
            return NotFound();

        if (fileInfo.Extension != ".mp4")
            return BadRequest();

        return PhysicalFile(fileInfo.FullName, "video/mp4");
    }
}
