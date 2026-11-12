namespace ECHO;

public enum VoiceKind { CreepyChild, YoungWoman, OldWoman, Boy, Man, Puppet, Demon }

/// <summary>Streaming dual-head overlap-add pitch shifter with tone shaping and effects.
/// No cloud dependency. Pitch is shifted without changing playback duration.</summary>
public sealed class VoiceProcessor
{
    private const int Size = 8192;
    private readonly float[] history = new float[Size];
    private long written;
    private double phase;
    private double lowpass, bass, tremoloPhase;
    private readonly float[] echo = new float[22050];
    private int echoIndex;
    private readonly Random random = new(9173);
    public VoiceKind Kind { get; set; } = VoiceKind.Puppet;
    public float Strength { get; set; } = 0.8f;
    public float InputGain { get; set; } = 1f;
    public float OutputGain { get; set; } = 0.75f;

    public void Reset()
    {
        Array.Clear(history); 
        Array.Clear(echo);
        written = 0; 
        phase = 0; 
        lowpass = bass = tremoloPhase = 0; 
        echoIndex = 0;
    }

    public void Process(ReadOnlySpan<float> input, Span<float> output)
    {
        var (pitch, cutoff, distortion, echoMix, modulation) = Parameters(Kind);
        pitch = 1 + (pitch - 1) * Strength;
        var lpAlpha = Math.Exp(-2 * Math.PI * cutoff / 44100.0);
        const double window = 1400;
        for (int i = 0;
            i < input.Length;
            i++)
        {
            var dry = Math.Clamp(input[i] * InputGain, -1f, 1f);
            history[(int)(written & (Size - 1))] = dry;
            // Read heads crossfade over a Hann-style triangular envelope.
            // 1200-sample lookback ensures read positions remain in written history.
            double a = phase / window;
            double b = (phase + window / 2) % window / window;
            double y1 = Read(written - 1700 + a * window * (pitch - 1));
            double y2 = Read(written - 1700 + b * window * (pitch - 1));
            double w1 = 0.5 - 0.5 * Math.Cos(2 * Math.PI * a);
            double w2 = 0.5 - 0.5 * Math.Cos(2 * Math.PI * b);
            double shifted = (y1 * w1 + y2 * w2) / Math.Max(0.01, w1 + w2);
            double blended = dry * (1 - Strength) + shifted * Strength;
            lowpass = (1 - lpAlpha) * blended + lpAlpha * lowpass;
            bass = 0.995 * bass + 0.005 * blended;
            double shaped = lowpass * 0.85 + bass * 0.15;
            if (Kind == VoiceKind.Puppet)
            {
                shaped = Math.Tanh(shaped * (1 + distortion * Strength)) * 0.83;
            }
            else if (Kind == VoiceKind.Demon)
            {
                double sub = Math.Sign(bass) * Math.Sqrt(Math.Abs(bass)) * 0.15;
                shaped = Math.Tanh((shaped + sub) * (1 + distortion * Strength));
            }
            else if (Kind == VoiceKind.OldWoman)
            {
                shaped += (random.NextDouble() - 0.5) * 0.004 * Strength;
            }

            tremoloPhase += modulation / 44100.0;
            if (tremoloPhase >= 1)
            {
                tremoloPhase -= 1;
            }

            if (modulation > 0)
            {
                shaped *= 1 - 0.16 * Strength + 0.16 * Strength * Math.Sin(2 * Math.PI * tremoloPhase);
            }

            double delayed = echo[echoIndex];
            echo[echoIndex] = (float)(shaped + delayed * 0.22);
            echoIndex = (echoIndex + 1) % echo.Length;
            double mixed = shaped + delayed * echoMix * Strength;
            output[i] = (float)Math.Clamp(Math.Tanh(mixed * OutputGain), -0.98, 0.98);
            phase++;
            if (phase >= window)
            {
                phase -= window;
            }

            written++;
        }
    }

    private double Read(double position)
    {
        long index = (long)Math.Floor(position);
        if (index < 0 || index > written)
        {
            return 0;
        }

        double fraction = position - index;
        float x = history[(int)(index & (Size - 1))];
        float y = history[(int)((index + 1) & (Size - 1))];
        return x + ((y - x) * fraction);
    }

    private static (double pitch, double cutoff, double distortion, double echo, double modulation) Parameters(VoiceKind kind) => kind switch
    {
        VoiceKind.CreepyChild => (1.65, 5900, 0, 0.18, 3.7),
        VoiceKind.YoungWoman => (1.30, 7300, 0, 0.03, 0),
        VoiceKind.OldWoman => (1.18, 3400, 0, 0.08, 6.2),
        VoiceKind.Boy => (1.42, 6200, 0, 0.02, 0),
        VoiceKind.Man => (0.85, 5400, 0, 0.02, 0),
        VoiceKind.Puppet => (0.76, 3200, 2.3, 0.35, 4.8),
        VoiceKind.Demon => (0.58, 2600, 3.7, 0.55, 2.4),
        _ => (1, 6000, 0, 0, 0)
    };
}
