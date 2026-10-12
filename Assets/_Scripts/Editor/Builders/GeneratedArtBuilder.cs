using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Kenney has no classic Flappy pipes and no music loop, so both are generated here.
// Deterministic (fixed seed, no wall-clock input): running it again produces identical files.
public static class GeneratedArtBuilder
{
    private const string ArtFolder = "Assets/_Art/Generated";
    private const string AudioFolder = "Assets/_Audio/Generated";

    private struct PipePalette
    {
        public Color Outline;
        public Color Dark;
        public Color Base;
        public Color Light;
        public Color Highlight;
    }

    private static readonly PipePalette Green = new()
    {
        Outline = Hex("3B6B1C"),
        Dark = Hex("5E9E27"),
        Base = Hex("7AC231"),
        Light = Hex("9BDB4E"),
        Highlight = Hex("C6F08A")
    };

    private static readonly PipePalette Gold = new()
    {
        Outline = Hex("8A5A0E"),
        Dark = Hex("D9971C"),
        Base = Hex("F5BE2E"),
        Light = Hex("FFD65C"),
        Highlight = Hex("FFF1B0")
    };

    [MenuItem("Tools/Flappy/Builders/Generated Art")]
    public static void Build()
    {
        Directory.CreateDirectory(ArtFolder);
        Directory.CreateDirectory(AudioFolder);

        WritePng("pipe_body_green.png", PipeBody(Green));
        WritePng("pipe_cap_green.png", PipeCap(Green));
        WritePng("pipe_body_gold.png", PipeBody(Gold));
        WritePng("pipe_cap_gold.png", PipeCap(Gold));
        WritePng("world_white.png", Solid(8, 8, Color.white));
        WritePng("tooltip_arrow.png", Triangle(48, 24));

        MusicGenerator.Write(Path.Combine(AudioFolder, "music_loop.wav"));

        AssetDatabase.Refresh();
        Debug.Log("GeneratedArtBuilder: done");
    }

    #region Sprites
    private const int BodyWidth = 128;
    private const int BodyHeight = 64;
    private const int CapWidth = 160;
    private const int CapHeight = 64;
    private const int OutlineWidth = 6;

    // Vertically tileable: every row is identical, shading only varies with x.
    private static Texture2D PipeBody(PipePalette palette)
    {
        var texture = NewTexture(BodyWidth, BodyHeight);
        for (int x = 0; x < BodyWidth; x++)
        {
            Color column = ShadeColumn(palette, x, BodyWidth);
            for (int y = 0; y < BodyHeight; y++)
                texture.SetPixel(x, y, column);
        }
        texture.Apply();
        return texture;
    }

    private static Texture2D PipeCap(PipePalette palette)
    {
        const int radius = 12;
        var texture = NewTexture(CapWidth, CapHeight);
        for (int x = 0; x < CapWidth; x++)
        {
            Color column = ShadeColumn(palette, x, CapWidth);
            for (int y = 0; y < CapHeight; y++)
            {
                float distanceOutside = RoundedRectDistance(x + 0.5f, y + 0.5f, CapWidth, CapHeight, radius);
                Color color;
                if (distanceOutside > 0f)
                    color = Color.clear;
                else if (distanceOutside > -OutlineWidth)
                    color = palette.Outline;
                else
                    color = y < OutlineWidth + 6 ? Color.Lerp(column, palette.Dark, 0.5f) : column;

                // Soft 1px anti-aliased edge.
                if (distanceOutside > -1f && distanceOutside <= 0f)
                    color.a = Mathf.Clamp01(-distanceOutside + 0.5f);

                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return texture;
    }

    private static Color ShadeColumn(PipePalette palette, int x, int width)
    {
        if (x < OutlineWidth || x >= width - OutlineWidth)
            return palette.Outline;

        float t = (x - OutlineWidth) / (float)(width - 2 * OutlineWidth);
        if (t < 0.08f) return palette.Light;
        if (t < 0.14f) return palette.Highlight;
        if (t < 0.22f) return palette.Light;
        if (t < 0.70f) return palette.Base;
        if (t < 0.85f) return Color.Lerp(palette.Base, palette.Dark, 0.5f);
        return palette.Dark;
    }

    private static float RoundedRectDistance(float px, float py, float width, float height, float radius)
    {
        float qx = Mathf.Abs(px - width * 0.5f) - (width * 0.5f - radius);
        float qy = Mathf.Abs(py - height * 0.5f) - (height * 0.5f - radius);
        float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
        return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
    }

    private static Texture2D Solid(int width, int height, Color color)
    {
        var texture = NewTexture(width, height);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                texture.SetPixel(x, y, color);
        texture.Apply();
        return texture;
    }

    // Downward-pointing triangle; the flat edge is at the top.
    private static Texture2D Triangle(int width, int height)
    {
        var texture = NewTexture(width, height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float halfWidthAtRow = (y + 0.5f) / height * (width * 0.5f);
                float distance = halfWidthAtRow - Mathf.Abs(x + 0.5f - width * 0.5f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(distance + 0.5f)));
            }
        }
        texture.Apply();
        return texture;
    }

    private static Texture2D NewTexture(int width, int height) =>
        new(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };

    private static void WritePng(string fileName, Texture2D texture)
    {
        File.WriteAllBytes(Path.Combine(ArtFolder, fileName), texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }
    #endregion

    #region Music
    private static class MusicGenerator
    {
        private const int SampleRate = 44100;
        private const double Bpm = 110;
        private const int Bars = 8;
        private const int BeatsPerBar = 4;
        private const int StepsPerBeat = 2;
        private const float PeakAmplitude = 0.5f; // about -6 dBFS
        private const int Seed = 1337;

        // C major pentatonic, two octaves (MIDI).
        private static readonly int[] Scale = { 72, 74, 76, 79, 81, 84, 86, 88, 91, 93 };
        // C - Am - F - G, repeated.
        private static readonly int[] BassRoots = { 48, 45, 41, 43, 48, 45, 41, 43 };
        private static readonly int[] ChordTonesPerBar = { 0, 4, 2, 3, 0, 4, 2, 3 };

        public static void Write(string path)
        {
            double secondsPerStep = 60.0 / Bpm / StepsPerBeat;
            int stepsTotal = Bars * BeatsPerBar * StepsPerBeat;
            int sampleCount = (int)Math.Round(stepsTotal * secondsPerStep * SampleRate);
            var buffer = new float[sampleCount];
            var random = new System.Random(Seed);

            int[] melody = ComposeMelody(random);
            for (int step = 0; step < stepsTotal; step++)
            {
                double start = step * secondsPerStep;
                if (melody[step] >= 0)
                    AddNote(buffer, start, secondsPerStep * 0.9, Midi(melody[step]), 0.32f, Waveform.Lead);

                // Soft off-beat hat.
                if (step % 2 == 1)
                    AddNoise(buffer, start, 0.035, 0.05f, random);
            }

            for (int bar = 0; bar < Bars; bar++)
            {
                for (int beat = 0; beat < BeatsPerBar; beat++)
                {
                    double start = (bar * BeatsPerBar + beat) * StepsPerBeat * secondsPerStep;
                    int note = beat == 2 ? BassRoots[bar] + 7 : BassRoots[bar];
                    AddNote(buffer, start, secondsPerStep * 1.8, Midi(note), 0.45f, Waveform.Bass);
                }
            }

            Normalize(buffer);
            WriteWav(path, buffer);
        }

        private static int[] ComposeMelody(System.Random random)
        {
            int stepsPerBar = BeatsPerBar * StepsPerBeat;
            int phraseSteps = 4 * stepsPerBar;
            var melody = new int[phraseSteps * 2];
            int index = 2;

            for (int step = 0; step < phraseSteps; step++)
            {
                int bar = step / stepsPerBar;
                bool onBeat = step % StepsPerBeat == 0;

                if (!onBeat && random.NextDouble() < 0.3)
                {
                    melody[step] = -1;
                    continue;
                }

                // Downbeats land on a chord tone, the rest is a gentle random walk.
                if (step % stepsPerBar == 0)
                    index = ChordTonesPerBar[bar] + (random.NextDouble() < 0.5 ? 0 : 5);
                else
                    index = Mathf.Clamp(index + random.Next(-2, 3), 0, Scale.Length - 1);

                melody[step] = Scale[index];
            }

            // Second phrase repeats the first, then resolves to the tonic so the loop closes naturally.
            for (int step = 0; step < phraseSteps; step++)
                melody[phraseSteps + step] = melody[step];

            int lastBarStart = phraseSteps * 2 - stepsPerBar;
            melody[lastBarStart + 4] = Scale[2];
            melody[lastBarStart + 5] = Scale[1];
            melody[lastBarStart + 6] = Scale[0];
            melody[lastBarStart + 7] = -1;
            return melody;
        }

        private enum Waveform { Lead, Bass }

        private static double Midi(int note) => 440.0 * Math.Pow(2.0, (note - 69) / 12.0);

        private static void AddNote(float[] buffer, double startSeconds, double durationSeconds, double frequency, float gain, Waveform waveform)
        {
            int start = (int)(startSeconds * SampleRate);
            int length = (int)(durationSeconds * SampleRate);
            const double attack = 0.008;
            const double release = 0.06;

            for (int i = 0; i < length && start + i < buffer.Length; i++)
            {
                double t = (double)i / SampleRate;
                double phase = 2.0 * Math.PI * frequency * t;
                double sample = waveform == Waveform.Lead
                    ? Triangle(phase) * 0.75 + Math.Sin(phase * 2.0) * 0.25
                    : Triangle(phase);

                // Envelope always starts and ends at zero, so notes never click and the loop seam is silent.
                double envelope = Math.Min(1.0, t / attack);
                envelope *= Math.Exp(-t * (waveform == Waveform.Lead ? 3.0 : 2.0));
                double remaining = durationSeconds - t;
                if (remaining < release)
                    envelope *= remaining / release;

                buffer[start + i] += (float)(sample * envelope * gain);
            }
        }

        private static void AddNoise(float[] buffer, double startSeconds, double durationSeconds, float gain, System.Random random)
        {
            int start = (int)(startSeconds * SampleRate);
            int length = (int)(durationSeconds * SampleRate);
            for (int i = 0; i < length && start + i < buffer.Length; i++)
            {
                double envelope = 1.0 - (double)i / length;
                buffer[start + i] += (float)((random.NextDouble() * 2.0 - 1.0) * envelope * envelope * gain);
            }
        }

        private static double Triangle(double phase)
        {
            double x = phase / (2.0 * Math.PI);
            x -= Math.Floor(x);
            return 4.0 * Math.Abs(x - 0.5) - 1.0;
        }

        private static void Normalize(float[] buffer)
        {
            float peak = 0f;
            foreach (float sample in buffer)
                peak = Mathf.Max(peak, Mathf.Abs(sample));

            if (peak <= 0f)
                return;

            float scale = PeakAmplitude / peak;
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] *= scale;

            // Hard guarantee for a click-free loop seam.
            buffer[0] = 0f;
            buffer[^1] = 0f;
        }

        private static void WriteWav(string path, float[] samples)
        {
            const short channels = 1;
            const short bitsPerSample = 16;
            int dataSize = samples.Length * channels * bitsPerSample / 8;

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + dataSize);
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(SampleRate);
            writer.Write(SampleRate * channels * bitsPerSample / 8);
            writer.Write((short)(channels * bitsPerSample / 8));
            writer.Write(bitsPerSample);
            writer.Write("data".ToCharArray());
            writer.Write(dataSize);

            foreach (float sample in samples)
                writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(sample * short.MaxValue), short.MinValue, short.MaxValue));
        }
    }
    #endregion
}
