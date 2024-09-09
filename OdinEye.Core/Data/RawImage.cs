using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace OdinEye.Core.Data;

[Index(nameof(ExposedOn))]
public class RawImage
{
    public int Id { get; set; }

    [Column(TypeName = "DATETIME")]
    public DateTime CreatedOn { get; set; }

    [Column(TypeName = "TEXT COLLATE NOCASE")]
    public string Filename { get; set; } = null!;

    /// <summary>
    /// UTC unix timestamp, seconds.
    /// </summary>
    public long ExposedOn { get; set; }
}
