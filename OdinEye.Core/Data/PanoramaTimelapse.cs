﻿using System.ComponentModel.DataAnnotations.Schema;

namespace OdinEye.Core.Data;

public class PanoramaTimelapse
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
