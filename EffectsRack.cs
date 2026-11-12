namespace ECHO;

public sealed class EffectsRack
{
    private readonly float[] delay = new float[44100];
    private int cursor;
    private double lp, hpInput, hpOutput, chorusPhase, tremoloPhase;
    public float Drive { get; set; } = 0.15f;
    public float Delay { get; set; } = 0.10f;
    public float Tremolo { get; set; }
    public float Tone { get; set; } = 0.75f;
    public float Chorus { get; set; }
    public float Gate { get; set; } = 0.01f;
    public float Compression { get; set; } = 0.4f;
    public float Reverb { get; set; } = 0.12f;
    public bool Enabled { get; set; } = true;
    public void Reset()
    {
        Array.Clear(delay); 
        cursor = 0;
        lp = hpInput = hpOutput = chorusPhase = tremoloPhase = 0;
    }
    public void Process(Span<float> samples)
    {
        if (!Enabled) return;
        var alpha = Math.Exp(-2 * Math.PI * (350 + Tone * 16000) / 44100);
        for (int i = 0; i < samples.Length; i++)
        {
            double x = samples[i];
            if (Math.Abs(x) < Gate * 0.12) x *= 0.2;
            hpOutput = 0.996 * (hpOutput + x - hpInput); 
            hpInput = x;
            x = hpOutput;
            lp = (1 - alpha) * x + alpha * lp; 
            x = lp;
            x = Math.Tanh(x * (1 + Drive * 8)) / Math.Tanh(1 + Drive * 8);
            x = Math.Sign(x) * Math.Pow(Math.Abs(x), 1 - Compression * 0.35);
            tremoloPhase = (tremoloPhase + 4.5 / 44100) % 1;
            x *= 1 - Tremolo * 0.5 + Tremolo * 0.5 * Math.Sin(tremoloPhase * 2 * Math.PI);
            chorusPhase = (chorusPhase + 0.7 / 44100) % 1;
            int chorusOffset = (int)(350 + (180 * Math.Sin(chorusPhase * 2 * Math.PI)));
            double chorus = delay[(cursor + delay.Length - chorusOffset) % delay.Length];
            double echo = delay[(cursor + delay.Length - 11025) % delay.Length];
            double reverb = delay[(cursor + delay.Length - 3871) % delay.Length];
            double result = x + Chorus * 0.35 * chorus + Delay * 0.45 * echo + Reverb * 0.30 * reverb;
            delay[cursor] = (float)Math.Clamp(x + (echo * 0.22) + reverb * 0.12, -1, 1);
            cursor = (cursor + 1) % delay.Length;
            samples[i] = (float)Math.Clamp(result, -0.98, 0.98);
        }
    }
}
