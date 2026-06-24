namespace voila.Models;

public sealed class SongRecord
{
  public long Index { get; init; }
  public string Title { get; init; } = "";
  public string Artist { get; init; } = "";
  public string Album { get; init; } = "";
  public string Genre { get; init; } = "";
  public int Likes { get; init; }
  public string Review { get; init; } = "";
  public string CoverUrl { get; set; } = "";
  public string AudioUrl { get; set; } = "";
}