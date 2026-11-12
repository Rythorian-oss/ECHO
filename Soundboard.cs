using NAudio.Wave;
namespace ECHO;

public static class Soundboard
{
    public static void Play(string effect)
    {
        _ = Task.Run(() =>
    {
        try
        {
            const int rate = 44100;
            int length = rate * 3;
            var bytes = new byte[length * 2];
            var rng = new Random();
            double phase = 0;
            for (int i = 0;
            i < length;
            i++)
            {
                double t = i / (double)rate;
                double env = Math.Min(1, t * 12) * Math.Max(0, 1 - t / 3);
                double frequency = effect switch
                {
                    "Heartbeat" => 65,
                    "Whisper" => 190,
                    "Metal" => 850 - t * 180,
                    "Scream" => 380 + 210 * Math.Sin(t * 2.1),
                    "Footsteps" => 75,
                    _ => 110 + 50 * Math.Sin(t * 9)
                };
                phase += 2 * Math.PI * frequency / rate;
                double noise = rng.NextDouble() * 2 - 1;
                double gate = effect switch
                {
                    "Heartbeat" => Math.Pow(Math.Max(0, Math.Sin(2 * Math.PI * 1.2 * t)), 24),
                    "Footsteps" => Math.Pow(Math.Max(0, Math.Sin(2 * Math.PI * 1.8 * t)), 18),
                    "Laughter" => Math.Pow(Math.Max(0, Math.Sin(2 * Math.PI * 3.7 * t)), 3),
                    _ => 1
                };
                double sample = effect switch
                {
                    "Whisper" => noise * 0.16,
                    "Metal" => (Math.Sin(phase) + Math.Sin(phase * 1.41)) * 0.14,
                    "Footsteps" => noise * 0.25,
                    _ => (Math.Sin(phase) + noise * 0.22) * 0.23
                };
                short s = (short)(Math.Clamp(sample * gate * env, -0.85, 0.85) * short.MaxValue);
                bytes[i * 2] = (byte)s; bytes[i * 2 + 1] = (byte)(s >> 8);
            }
            using var source = new RawSourceWaveStream(new MemoryStream(bytes), new WaveFormat(rate, 16, 1));
            using var output = new WaveOutEvent();
            using var finished = new ManualResetEventSlim(false);
            output.PlaybackStopped += (_, _) => finished.Set();
            output.Init(source); output.Play(); finished.Wait(TimeSpan.FromSeconds(5));
        }
        catch 
        {
            /* No output device; main audio pipeline remains unaffected. */
        }
    });
    }
}
