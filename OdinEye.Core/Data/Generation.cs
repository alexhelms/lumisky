using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OdinEye.Core.Data;

public class Generation
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "DATETIME")]
    public DateTime CreatedOn { get; set; }

    [Column(TypeName = "DATETIME")]
    public DateTime? StartedOn { get; set; }

    [Column(TypeName = "DATETIME")]
    public DateTime? CompletedOn { get; set; }

    /// <summary>
    /// Unix timestamp, seconds.
    /// </summary>
    public long RangeBegin { get; set; }

    /// <summary>
    /// Unix timestamp, seconds.
    /// </summary>
    public long RangeEnd { get; set; }

    public GenerationKind Kind { get; set; }

    public GenerationState State { get; set; }

    public int Progress { get; set; }

    [Column(TypeName = "TEXT COLLATE NOCASE")]
    public string? OutputFilename { get; set; }

    public string? JobInstanceId { get; set; }

    public int? TimelapseId { get; set; }

    public int? PanoramaTimelapseId { get; set; }
}

public enum GenerationKind
{
    Timelapse,
    [Display(Description = "Panorama Timelapse")]
    PanoramaTimelapse,
}

public enum GenerationState
{
    Queued,
    Running,
    Success,
    Failed,
    Canceled,
}