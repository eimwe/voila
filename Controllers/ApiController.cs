using Microsoft.AspNetCore.Mvc;
using voila.Services;

namespace voila.Controllers;

[ApiController]
[Route("api")]
public sealed class ApiController : ControllerBase
{
  private readonly LocaleService _locales;
  private readonly SongService _generator;
  private readonly CoverService _cover;
  private readonly int _pageSize;

  public ApiController(LocaleService locales, SongService generator, CoverService cover, IConfiguration config)
  {
    _locales = locales;
    _generator = generator;
    _cover = cover;
    _pageSize = Math.Clamp(config.GetValue("PAGE_SIZE", 20), 1, 100);
  }

  [HttpGet("songs")]
  public IActionResult Songs(
      [FromQuery] string? locale,
      [FromQuery] string? seed,
      [FromQuery] int page = 0,
      [FromQuery] double likes = 0)
  {
    var loc = _locales.GetOrDefault(locale);
    ulong seedVal = ParseSeed(seed);
    page = Math.Max(0, page);
    likes = Math.Clamp(likes, 0, 10);

    var items = _generator
        .GeneratePage(loc, seedVal, page, _pageSize, likes)
        .Select(r =>
        {
          r.CoverUrl = BuildUrl("cover", loc.Locale, seedVal, r.Index - 1);
          r.AudioUrl = BuildUrl("audio", loc.Locale, seedVal, r.Index - 1);
          return r;
        })
        .ToList();

    return Ok(new
    {
      locale = loc.Locale,
      seed = seedVal.ToString(),
      page,
      pageSize = _pageSize,
      likes,
      items
    });
  }

  [HttpGet("locales")]
  public IActionResult Locales() =>
      Ok(_locales.Available.Select(a => new { code = a.Code, display = a.Display }));

  [HttpGet("cover")]
  public IActionResult Cover([FromQuery] string? locale, [FromQuery] string? seed, [FromQuery] long index)
  {
    var loc = _locales.GetOrDefault(locale);
    ulong seedVal = ParseSeed(seed);
    if (index < 0) index = 0;

    var rec = _generator.Generate(loc, seedVal, index, 0);
    byte[] png = _cover.Render(rec.Title, rec.Artist, seedVal, index);

    Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    return File(png, "image/png");
  }

  private string BuildUrl(string kind, string locale, ulong seed, long index) =>
      $"/api/{kind}?locale={Uri.EscapeDataString(locale)}&seed={seed}&index={index}";

  private static ulong ParseSeed(string? raw)
  {
    if (string.IsNullOrWhiteSpace(raw)) return 0;
    if (ulong.TryParse(raw, out var n)) return n;

    ulong h = 1469598103934665603UL;
    foreach (byte b in System.Text.Encoding.UTF8.GetBytes(raw))
    {
      h ^= b;
      h *= 1099511628211UL;
    }
    return h;
  }
}