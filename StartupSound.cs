using NAudio.Wave;

namespace ECHO;

/// <summary>Original procedural eerie chuckle; no film audio or samples.</summary>
internal static class StartupSound
{
    public static void Play()
    {
        _ = Task.Run(() =>
        {
            try
            {
                const int rate = 44100;
                const double duration = 2.25;
                var bytes = new byte[(int)(rate * duration) * 2];
                var rng = new Random(712);
                double phase = 0;
                for (int i = 0;
                i < bytes.Length / 2;
                i++)
                {
                    double t = i / (double)rate;
                    double pulse = Math.Pow(Math.Max(0, Math.Sin(2 * Math.PI * 3.7 * t)), 3);
                    double envelope = Math.Min(1, t * 10) * Math.Min(1, (duration - t) * 3);
                    double frequency = 108 + 48 * Math.Sin(2 * Math.PI * 1.6 * t) + 25 * Math.Sin(2 * Math.PI * 0.47 * t);
                    phase += 2 * Math.PI * frequency / rate;
                    double noise = (rng.NextDouble() * 2 - 1) * 0.1;
                    double value = (Math.Sin(phase) + 0.4 * Math.Sin(2.03 * phase) + noise) * pulse * envelope * 0.24;
                    short sample = (short)(Math.Clamp(value, -1, 1) * short.MaxValue);
                    bytes[2 * i] = (byte)sample;
                    bytes[2 * i + 1] = (byte)(sample >> 8);
                }
                using var provider = new RawSourceWaveStream(new MemoryStream(bytes), new WaveFormat(rate, 16, 1));
                using var output = new WaveOutEvent();
                output.Init(provider);
                using var done = new ManualResetEventSlim(false);
                output.PlaybackStopped += (_, _) => done.Set();
                output.Play();
                done.Wait(TimeSpan.FromSeconds(4));
            }
            catch 
            {
                /* Sound is optional when no output device is present. */
            }
        });
    }
}
