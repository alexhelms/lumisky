﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LumiSky.Core.Services;
using LumiSky.Core.IO;
using LumiSky.Core.Utilities;

namespace LumiSky.Controllers;

[ApiController]
[Route("api/image")]
public class ImageController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly ImageService _service;

    public ImageController(
        AppDbContext dbContext,
        ImageService service)
    {
        _dbContext = dbContext;
        _service = service;
    }

    private IActionResult GetActionResultForImage(string path, bool downloadFile)
    {
        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists)
            return NotFound();

        var filename = Path.GetFileName(fileInfo.FullName);
        var extension = fileInfo.Extension.ToLowerInvariant();
        var contentType = Util.ExtensionToMimeType(extension);

        if (downloadFile)
        {
            return PhysicalFile(fileInfo.FullName, contentType, filename);
        }
        else
        {
            return PhysicalFile(fileInfo.FullName, contentType);
        }
    }

    [HttpGet("latest/{type}")]
    public IActionResult GetLatest([FromRoute] string type)
    {
        // Defensive copy, the latest could change during this function
        string? latest;
        if (type == "image") latest = _service.LatestImagePath;
        else if (type == "panorama") latest = _service.LatestPanoramaPath;
        else if (type == "focus") latest = _service.LatestFocusPath;
        else return NotFound();

        if (latest == null)
            return NotFound();

        return GetActionResultForImage(latest, downloadFile: false);
    }

    [HttpGet("download/{type}")]
    public async Task<IActionResult> GetDownload(
        [FromRoute] string type,
        [FromQuery(Name = "ts")] long unixTimestamp)
    {
        string filename;
                
        if (type == "image")
        {
            var image = await _dbContext.Images
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ExposedOn == unixTimestamp);
            if (image is null)
                return NotFound();

            filename = image.Filename;
        }
        else if (type == "panorama")
        {
            var panorama = await _dbContext.Panoramas
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ExposedOn == unixTimestamp);
            if (panorama is null)
                return NotFound();

            filename = panorama.Filename;
        }
        else if (type == "raw")
        {
            var rawImage = await _dbContext.RawImages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ExposedOn == unixTimestamp);
            if (rawImage is null)
                return NotFound();

            filename = rawImage.Filename;
        }
        else if (type == "focus")
        {
            filename = LumiSkyPaths.LatestFocusImage;
        }
        else
        {
            return NotFound();
        }

        return GetActionResultForImage(filename, downloadFile: true);
    }

    [HttpGet("view/{type}")]
    public async Task<IActionResult> GetView(
        [FromRoute] string type,
        [FromQuery(Name = "ts")] long unixTimestamp)
    {
        string filename;

        if (type == "image")
        {
            var image = await _dbContext.Images
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ExposedOn == unixTimestamp);
            if (image is null)
                return NotFound();
            
            filename = image.Filename;
        }
        else if (type == "panorama")
        {
            var panorama = await _dbContext.Panoramas
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ExposedOn == unixTimestamp);
            if (panorama is null)
                return NotFound();

            filename = panorama.Filename;
        }
        else if (type == "focus")
        {
            filename = LumiSkyPaths.LatestFocusImage;
        }
        else
        {
            return NotFound();
        }

        return GetActionResultForImage(filename, downloadFile: false);
    }
}
