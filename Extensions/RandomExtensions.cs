namespace voila.Extensions;

public static class RandomExtensions
{
  public static T Pick<T>(this Random rng, IReadOnlyList<T> list) => list[rng.Next(list.Count)];

  public static bool Chance(this Random rng, double probability) => rng.NextDouble() < probability;
}