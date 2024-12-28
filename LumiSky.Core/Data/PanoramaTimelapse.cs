using System.ComponentModel.DataAnnotations.Schema;

namespace LumiSky.Core.Data;

public class PanoramaTimelapse : ICanBeCleanedUp
{
    public int Id { get; set; }

    [Column(TypeName = "DATETIME")]
    public DateTime CreatedOn { get; set; }

    [Column(TypeName = "TEXT COLLATE NOCASE")]
    public string Filename { get; set; } = null!;

    /// <summary>
    /// UTC unix timestamp, seconds.
    /// </summary>
    public long RangeBegin { get; set; }

    /// <summary>
    /// UTC unix timestamp, seconds.
    /// </summary>
    public long RangeEnd { get; set; }
}
