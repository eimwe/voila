using Bogus;
using voila.Extensions;
using voila.Models;

namespace voila.Services;

public sealed class SongService
{
  private const string SingleAlbum = "Single";

  public SongRecord Generate(LocaleData loc, ulong seed, long globalIndex, double likesAvg)
  {
    var rng = SeedService.Content(seed, globalIndex);
    var faker = new Faker(loc.BogusLocale) { Random = new Randomizer(rng.Next()) };
    string Person() => faker.Name.FullName();
    string City() => faker.Address.City();

    string title = PatternService.Render(loc, rng.Pick(loc.Title.Patterns), rng, Person, City);

    string artist = rng.Chance(loc.Artist.PersonRatio)
        ? Person()
        : PatternService.Render(loc, rng.Pick(loc.Artist.Patterns), rng, Person, City);

    string album = rng.Chance(loc.Album.SingleRatio)
        ? SingleAlbum
        : PatternService.Render(loc, rng.Pick(loc.Album.Patterns), rng, Person, City);

    string genre = rng.Pick(loc.Genres);

    string review = loc.ReviewPatterns.Count > 0
        ? PatternService.Render(loc, rng.Pick(loc.ReviewPatterns), rng, Person, City, loc.Review)
        : "";

    return new SongRecord
    {
      Index = globalIndex + 1,
      Title = title,
      Artist = artist,
      Album = album,
      Genre = genre,
      Review = review,
      Likes = SeedService.ComputeLikes(likesAvg, seed, globalIndex)
    };
  }

  public IEnumerable<SongRecord> GeneratePage(
      LocaleData loc, ulong seed, int page, int pageSize, double likesAvg)
  {
    long start = (long)page * pageSize;
    for (int i = 0; i < pageSize; i++)
      yield return Generate(loc, seed, start + i, likesAvg);
  }
}