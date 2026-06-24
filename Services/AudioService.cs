using MeltySynth;

namespace voila.Services;

public sealed class AudioService
{
  private const int SampleRate = 44100;
  private const int Lead = 0, Pad = 1, Bass = 2, Drums = 9;

  private static readonly Dictionary<string, int[]> Scales = new()
  {
    ["major"] = new[] { 0, 2, 4, 5, 7, 9, 11 },
    ["minor"] = new[] { 0, 2, 3, 5, 7, 8, 10 },
    ["dorian"] = new[] { 0, 2, 3, 5, 7, 9, 10 },
  };
  private static readonly Dictionary<string, int[][]> Progs = new()
  {
    ["major"] = new[] { new[] { 0, 4, 5, 3 }, new[] { 0, 3, 4, 3 }, new[] { 5, 3, 0, 4 }, new[] { 1, 4, 0, 5 } },
    ["minor"] = new[] { new[] { 0, 5, 2, 6 }, new[] { 0, 3, 4, 0 }, new[] { 0, 6, 5, 6 }, new[] { 5, 2, 3, 4 } },
    ["dorian"] = new[] { new[] { 0, 3, 0, 4 }, new[] { 0, 6, 3, 4 }, new[] { 3, 4, 0, 0 } },
  };
  private static readonly int[] Leads = { 0, 24, 73, 11, 80, 56 };
  private static readonly int[] Pads = { 48, 89, 0 };
  private static readonly int[] Basses = { 33, 32, 38 };
  private static readonly double[][] Rhythms =
  {
    new[]{1.0,1,1,1}, new[]{0.5,0.5,1,1,1}, new[]{1.0,0.5,0.5,2}, new[]{2.0,1,1},
    new[]{0.5,0.5,0.5,0.5,1,1}, new[]{1.0,1,2}, new[]{0.5,0.5,1,0.5,0.5,1},
  };

  private readonly SoundFont _soundFont;

  public AudioService(IWebHostEnvironment env, IConfiguration config)
  {
    var rel = config["SOUNDFONT_PATH"] ?? Path.Combine("SoundFonts", "gm.sf2");
    var path = Path.IsPathRooted(rel) ? rel : Path.Combine(env.ContentRootPath, rel);
    if (!File.Exists(path))
      throw new FileNotFoundException($"SoundFont not found: {path}. Place a GM .sf2 there or set SOUNDFONT_PATH.");
    _soundFont = new SoundFont(path);
  }

  private sealed record Note(int Channel, int Program, int Pitch, double Start, double Dur, int Velocity);

  private static int ScalePitch(int root, int[] scale, int degree, int octave)
  {
    int o = (int)Math.Floor(degree / 7.0);
    int d = degree - o * 7;
    return root + 12 * (octave + o) + scale[d];
  }
  private static int[] Triad(int root, int[] scale, int deg) =>
    new[] { ScalePitch(root, scale, deg, 0), ScalePitch(root, scale, deg + 2, 0), ScalePitch(root, scale, deg + 4, 0) };

  private static T Pick<T>(Random r, IReadOnlyList<T> a) => a[r.Next(a.Count)];

  private (List<Note> notes, int bpm, int leadProg, int padProg, int bassProg) Compose(Random r)
  {
    var scaleName = Pick(r, new List<string>(Scales.Keys));
    var scale = Scales[scaleName];
    int root = r.Next(48, 58);
    int bpm = r.Next(72, 139);
    var verse = Pick(r, Progs[scaleName]);
    var chorus = Pick(r, Progs[scaleName]);
    var bars = new List<int>(); bars.AddRange(verse); bars.AddRange(chorus);

    int leadProg = Pick(r, Leads), padProg = Pick(r, Pads), bassProg = Pick(r, Basses);
    var notes = new List<Note>();
    int prev = root + 24;

    for (int bi = 0; bi < bars.Count; bi++)
    {
      int deg = bars[bi];
      double barStart = bi * 4;
      var tri = Triad(root, scale, deg);
      bool chorusPart = bi >= 4;

      foreach (var p in tri) notes.Add(new Note(Pad, padProg, p + 12, barStart, 4, 52));

      notes.Add(new Note(Bass, bassProg, tri[0] - 12, barStart, 2, 80));
      notes.Add(new Note(Bass, bassProg, tri[0] - 12, barStart + 2, 2, 72));

      for (int b = 0; b < 4; b++)
      {
        if (b == 0 || b == 2) notes.Add(new Note(Drums, -1, 36, barStart + b, 0.25, 90));
        if (b == 1 || b == 3) notes.Add(new Note(Drums, -1, 38, barStart + b, 0.25, 96));
      }
      for (double t = 0; t < 4; t += 0.5) notes.Add(new Note(Drums, -1, 42, barStart + t, 0.2, 60));

      var rhythm = Pick(r, Rhythms);
      var chordPcs = new HashSet<int> { tri[0] % 12, tri[1] % 12, tri[2] % 12 };
      double pos = 0;
      foreach (var dur in rhythm)
      {
        if (r.NextDouble() < (chorusPart ? 0.05 : 0.12)) { pos += dur; continue; }

        bool onBeat = Math.Abs(pos - Math.Round(pos)) < 1e-6;
        int pitch;
        if (onBeat && r.NextDouble() < 0.7)
        {
          pitch = prev; int best = int.MaxValue;
          for (int d2 = 0; d2 <= 7; d2++)
          {
            int cand = ScalePitch(root, scale, d2, 2);
            if (chordPcs.Contains(((cand % 12) + 12) % 12) && Math.Abs(cand - prev) < best)
            { best = Math.Abs(cand - prev); pitch = cand; }
          }
        }
        else
        {
          int degNow = 0, best = int.MaxValue;
          for (int d2 = 0; d2 < 15; d2++)
          {
            int cand = ScalePitch(root, scale, d2, 2);
            if (Math.Abs(cand - prev) < best) { best = Math.Abs(cand - prev); degNow = d2; }
          }
          int step = r.NextDouble() < 0.8 ? new[] { -2, -1, 1, 2 }[r.Next(4)]
                                          : new[] { -4, -3, 3, 4 }[r.Next(4)];
          pitch = ScalePitch(root, scale, Math.Max(0, degNow + step), 2);
        }

        int lo = root + 21, hi = root + 40;
        while (pitch < lo) pitch += 12;
        while (pitch > hi) pitch -= 12;
        int vel = Math.Min(127, r.Next(88, 113) + (chorusPart ? 8 : 0));
        notes.Add(new Note(Lead, leadProg, pitch, barStart + pos, dur * 0.95, vel));
        prev = pitch;
        pos += dur;
      }
    }
    return (notes, bpm, leadProg, padProg, bassProg);
  }

  public byte[] Render(ulong seed, long index)
  {
    var rng = SeedService.Audio(seed, index);
    var (notes, bpm, leadProg, padProg, bassProg) = Compose(rng);

    var settings = new SynthesizerSettings(SampleRate) { EnableReverbAndChorus = true };
    var synth = new Synthesizer(_soundFont, settings);

    synth.ProcessMidiMessage(Lead, 0xC0, leadProg, 0);
    synth.ProcessMidiMessage(Pad, 0xC0, padProg, 0);
    synth.ProcessMidiMessage(Bass, 0xC0, bassProg, 0);

    double spb = 60.0 / bpm;
    int Sample(double beat) => (int)Math.Round(beat * spb * SampleRate);

    var evts = new List<(int sample, bool on, int ch, int key, int vel)>();
    foreach (var n in notes)
    {
      evts.Add((Sample(n.Start), true, n.Channel, n.Pitch, n.Velocity));
      evts.Add((Sample(n.Start + n.Dur), false, n.Channel, n.Pitch, 0));
    }
    evts.Sort((a, b) => a.sample != b.sample ? a.sample.CompareTo(b.sample) : a.on.CompareTo(b.on));

    int lastSample = evts.Count > 0 ? evts[^1].sample : 0;
    int total = lastSample + (int)(1.5 * SampleRate);
    var left = new float[total];
    var right = new float[total];

    int posSample = 0, ei = 0;
    while (ei < evts.Count)
    {
      int target = Math.Min(evts[ei].sample, total);
      if (target > posSample) { synth.Render(left.AsSpan(posSample, target - posSample), right.AsSpan(posSample, target - posSample)); posSample = target; }

      while (ei < evts.Count && evts[ei].sample <= posSample)
      {
        var e = evts[ei++];
        if (e.on) synth.NoteOn(e.ch, e.key, e.vel); else synth.NoteOff(e.ch, e.key);
      }
    }
    if (posSample < total) synth.Render(left.AsSpan(posSample, total - posSample), right.AsSpan(posSample, total - posSample));

    return WriteWav(left, right);
  }

  private static byte[] WriteWav(float[] left, float[] right)
  {
    int n = left.Length;

    float peak = 0f;
    for (int i = 0; i < n; i++) { peak = Math.Max(peak, Math.Abs(left[i])); peak = Math.Max(peak, Math.Abs(right[i])); }
    float gain = peak > 1f ? 0.98f / peak : 1f;

    using var ms = new MemoryStream();
    using var bw = new BinaryWriter(ms);
    int byteRate = SampleRate * 2 * 2, dataLen = n * 2 * 2;
    bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
    bw.Write(36 + dataLen);
    bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
    bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
    bw.Write(16); bw.Write((short)1); bw.Write((short)2);
    bw.Write(SampleRate); bw.Write(byteRate);
    bw.Write((short)4); bw.Write((short)16);
    bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
    bw.Write(dataLen);
    for (int i = 0; i < n; i++)
    {
      bw.Write((short)Math.Clamp(left[i] * gain * 32767f, -32768f, 32767f));
      bw.Write((short)Math.Clamp(right[i] * gain * 32767f, -32768f, 32767f));
    }
    bw.Flush();
    return ms.ToArray();
  }
}