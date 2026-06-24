using System.Text.Json.Serialization;

namespace voila.Models;

public sealed class LocaleData
{
  public string Locale { get; set; } = "";
  public string DisplayName { get; set; } = "";
  public string BogusLocale { get; set; } = "";
  public PatternSet Title { get; set; } = new();
  public AlbumSet Album { get; set; } = new();
  public ArtistSet Artist { get; set; } = new();
  public List<string> Genres { get; set; } = new();
  public Dictionary<string, List<string>> Review { get; set; } = new();
  public Dictionary<string, List<string>> Pools { get; set; } = new();

  [JsonIgnore]
  public IReadOnlyList<string> ReviewPatterns =>
      Review.TryGetValue("patterns", out var p) ? p : Array.Empty<string>();
}

public sealed class PatternSet
{
  public List<string> Patterns { get; set; } = new();
}

public sealed class AlbumSet
{
  public List<string> Patterns { get; set; } = new();
  public double SingleRatio { get; set; }
}

public sealed class ArtistSet
{
  public double PersonRatio { get; set; }
  public List<string> Patterns { get; set; } = new();
}