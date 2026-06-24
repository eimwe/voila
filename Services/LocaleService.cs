using System.Text.Json;
using voila.Models;

namespace voila.Services;

public sealed class LocaleService
{
  private readonly Dictionary<string, LocaleData> _byCode;
  private readonly string _defaultCode;
  private static readonly JsonSerializerOptions JsonOpts = new()
  {
    PropertyNameCaseInsensitive = true
  };

  public LocaleService(IWebHostEnvironment env, IConfiguration config, ILogger<LocaleService> log)
  {
    var rel = config["LOCALES_DIR"] ?? Path.Combine("Data", "locales");
    var dir = Path.IsPathRooted(rel) ? rel : Path.Combine(env.ContentRootPath, rel);

    if (!Directory.Exists(dir))
      throw new DirectoryNotFoundException($"Locale directory not found: {dir}");

    _byCode = new(StringComparer.OrdinalIgnoreCase);
    foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
    {
      LocaleData data;
      try
      {
        data = JsonSerializer.Deserialize<LocaleData>(File.ReadAllText(file), JsonOpts)
               ?? throw new InvalidDataException("null");
      }
      catch (Exception ex)
      {
        throw new InvalidDataException($"Failed to parse locale file '{file}': {ex.Message}", ex);
      }

      if (string.IsNullOrWhiteSpace(data.Locale))
        throw new InvalidDataException($"Locale file '{file}' is missing 'locale'.");

      _byCode[data.Locale] = data;
    }

    if (_byCode.Count == 0)
      throw new InvalidDataException($"No locale files found in {dir}");

    _defaultCode = config["DEFAULT_LOCALE"] ?? "en-US";
    if (!_byCode.ContainsKey(_defaultCode))
      _defaultCode = _byCode.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).First();
  }

  public bool TryGet(string code, out LocaleData locale) => _byCode.TryGetValue(code, out locale!);

  public LocaleData GetOrDefault(string? code) =>
    code is not null && _byCode.TryGetValue(code, out var l) ? l : _byCode[_defaultCode];

  public IEnumerable<(string Code, string Display)> Available =>
    _byCode.Values
        .OrderByDescending(l => l.Locale == _defaultCode)
        .ThenBy(l => l.Locale, StringComparer.OrdinalIgnoreCase)
        .Select(l => (l.Locale, l.DisplayName));
}