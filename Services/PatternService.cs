using System.Text;
using System.Text.RegularExpressions;
using voila.Extensions;
using voila.Models;

namespace voila.Services;

public static partial class PatternService
{
  [GeneratedRegex(@"\{([^}]+)\}")] private static partial Regex TokenRx();
  [GeneratedRegex(@"\d+$")] private static partial Regex TrailingDigitsRx();

  private static string Base(string token) => TrailingDigitsRx().Replace(token, "");

  private static List<string>? Resolve(
      LocaleData loc, string token, IReadOnlyDictionary<string, List<string>>? block)
  {
    var pools = loc.Pools;
    if (pools.TryGetValue(token, out var exact) && exact.Count > 0) return exact;
    if (block is not null && block.TryGetValue(token, out var bExact) && bExact.Count > 0) return bExact;

    var b = Base(token);
    if (pools.TryGetValue(b, out var baseHit) && baseHit.Count > 0) return baseHit;
    if (block is not null && block.TryGetValue(b, out var bBaseHit) && bBaseHit.Count > 0) return bBaseHit;
    return null;
  }

  public static string Render(
      LocaleData loc,
      string pattern,
      Random rng,
      Func<string> personFn,
      Func<string> cityFn,
      IReadOnlyDictionary<string, List<string>>? block = null)
  {
    var sb = new StringBuilder(pattern.Length + 16);
    int pos = 0;
    foreach (Match m in TokenRx().Matches(pattern))
    {
      sb.Append(pattern, pos, m.Index - pos);
      string token = m.Groups[1].Value;
      string value = token switch
      {
        "@person" => personFn(),
        "@city" => cityFn(),
        _ => Resolve(loc, token, block) is { } pool ? rng.Pick(pool) : token
      };
      sb.Append(value);
      pos = m.Index + m.Length;
    }
    sb.Append(pattern, pos, pattern.Length - pos);
    return sb.ToString();
  }
}