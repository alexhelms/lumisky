namespace LumiSky.Core.Data;

public interface ICanBeCleanedUp
{
    public int Id { get; }
    public DateTime CreatedOn { get; }
    public string Filename { get; }
    public bool IsFavorite { get; }
}
