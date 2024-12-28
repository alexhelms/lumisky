using LumiSky.Core.Profile;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace LumiSky.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController : Controller
{
    private readonly IProfileProvider _profileProvider;

    public ProfileController(IProfileProvider profileProvider)
    {
        _profileProvider = profileProvider;
    }

    [HttpGet("export")]
    public IActionResult ExportProfile()
    {
        var json = _profileProvider.ExportProfile();
        return File(Encoding.UTF8.GetBytes(json), "text/plain", $"default{ProfileProvider.ProfileExtension}");
    }

    [HttpPost("import")]
    public IActionResult ImportProfile([FromForm] IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        string json = reader.ReadToEnd();
        bool success = _profileProvider.ImportProfile(json);
        return success ? Ok() : BadRequest();
    }
}
