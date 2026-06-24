namespace voila.Services;

public static class SeedService
{
  private const ulong SaltContent = 0xC0FFEE1234ABCDUL;
  private const ulong SaltCover = 0x0A17C0DE5EED0001UL;
  private const ulong SaltAudio = 0x00BADA55F00D5EEDUL;
  private const ulong SaltLikes = 0x013579BDF2468ACEUL;

  private static ulong Avalanche(ulong z)
  {
    z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
    z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
    return z ^ (z >> 31);
  }

  private static int MixToSeed(ulong userSeed, long index, ulong salt)
  {
    ulong h = Avalanche(userSeed ^ salt);
    h = Avalanche(h ^ (ulong)index);
    return unchecked((int)(h ^ (h >> 32)));
  }

  public static Random Content(ulong seed, long index) => new(MixToSeed(seed, index, SaltContent));
  public static Random Cover(ulong seed, long index) => new(MixToSeed(seed, index, SaltCover));
  public static Random Audio(ulong seed, long index) => new(MixToSeed(seed, index, SaltAudio));

  public static int ComputeLikes(double avg, ulong seed, long index)
  {
    if (avg <= 0) return 0;
    var rng = new Random(MixToSeed(seed, index, SaltLikes));
    int baseLikes = (int)Math.Floor(avg);
    double frac = avg - baseLikes;
    return frac > 0 && rng.NextDouble() < frac ? baseLikes + 1 : baseLikes;
  }
}