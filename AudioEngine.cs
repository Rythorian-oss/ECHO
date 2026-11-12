using NAudio.Wave;
using NAudio.Lame;

namespace ECHO;

public sealed class AudioEngine : IDisposable
{
    private readonly Lock gate = new();
    private WaveInEvent? capture;
    private WaveOutEvent? playback;
    private BufferedWaveProvider? buffer;
    private WaveFileWriter? wavWriter;
    private LameMP3FileWriter? mp3Writer;
    private string? recordingPath;
    private readonly VoiceProcessor processor = new();
    public EffectsRack Rack { get; } = new();
    public int OutputDeviceNumber { get; set; } = -1;
    public int CaptureBufferMs { get; set; } = 30;
    public int PlaybackLatencyMs { get; set; } = 120;
    private bool disposed;
    public bool IsRunning { get; private set; }
    public bool IsRecording => recordingPath != null;
    public event Action<float>? LevelChanged;
    public event Action<string>? Error;
    public VoiceKind Voice { get { lock (gate) return processor.Kind; } set { lock (gate) processor.Kind = value; } }
    public float Strength { set { lock (gate) processor.Strength = value; } }
    public float InputGain { set { lock (gate) processor.InputGain = value; } }
    public float OutputGain { set { lock (gate) processor.OutputGain = value; } }

    public void Start(int deviceNumber, bool monitor)
    {
        lock (gate)
        {
            if (IsRunning) 
                throw new InvalidOperationException("Stop the audio engine before restarting.");
            processor.Reset(); 
            Rack.Reset();
            try
            {
                var format = new WaveFormat(44100, 16, 1);
                buffer = new BufferedWaveProvider(format) 
                { 
                    BufferDuration = TimeSpan.FromMilliseconds(450), 
                    DiscardOnBufferOverflow = true, 
                    ReadFully = true 
                };
                capture = new WaveInEvent 
                { 
                    DeviceNumber = deviceNumber, 
                    WaveFormat = format, 
                    BufferMilliseconds = Math.Clamp(CaptureBufferMs, 10, 100) 
                };
                capture.DataAvailable += OnData;
                capture.RecordingStopped += OnCaptureStopped;
                if (monitor)
                {
                    playback = new WaveOutEvent 
                    { 
                        DeviceNumber = OutputDeviceNumber, 
                        DesiredLatency = Math.Clamp(PlaybackLatencyMs, 40, 300), 
                        NumberOfBuffers = 3 
                    };
                    playback.Init(buffer);
                    playback.Play();
                }
                capture.StartRecording();
                IsRunning = true;
            }
            catch 
            { 
                StopCore(); 
                throw; 
            }
        }
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        try
        {
            lock (gate)
            {
                if (!IsRunning) return;
                int count = e.BytesRecorded / 2;
                float[] input = new float[count];
                float[] output = new float[count];
                for (int i = 0;
                    i < count;
                    i++)
                {
                    input[i] = BitConverter.ToInt16(e.Buffer, i * 2) / 32768f;
                }

                processor.Process(input, output);
                Rack.Process(output);
                byte[] bytes = new byte[count * 2];
                double rms = 0;
                for (int i = 0;
                    i < count;
                    i++)
                {
                    float sample = output[i];
                    rms += sample * sample;
                    short pcm = (short)Math.Clamp((int)(sample * 32767), -32768, 32767);
                    bytes[i * 2] = (byte)pcm;
                    bytes[i * 2 + 1] = (byte)(pcm >> 8);
                }
                buffer?.AddSamples(bytes, 0, bytes.Length);
                wavWriter?.Write(bytes, 0, bytes.Length);
                mp3Writer?.Write(bytes, 0, bytes.Length);
                LevelChanged?.Invoke((float)Math.Sqrt(rms / Math.Max(1, count)));
            }
        }
        catch (Exception ex)
        {
            Error?.Invoke("Audio processing error: " + ex.Message);
            try
            {
                capture?.StopRecording();
            } 
            catch 
            { 
            }
        }
    }

    private void OnCaptureStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null) Error?.Invoke("Microphone stopped: " + e.Exception.Message);
    }

    public string StartRecording(bool mp3)
    {
        lock (gate)
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Start microphone processing first.");
            }

            if (IsRecording)
            {
                throw new InvalidOperationException("A recording is already active.");
            }

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(desktop))
            {
                throw new IOException("Windows Desktop location is unavailable.");
            }

            string folder = Path.Combine(desktop, "ECHO");
            Directory.CreateDirectory(folder);
            string extension = mp3 ? "mp3" : "wav";
            string path = Path.Combine(folder, $"ECHO_{DateTime.Now:yyyyMMdd_HHmmss_fff}.{extension}");
            try
            {
                if (mp3)
                {
                    mp3Writer = new LameMP3FileWriter(path, new WaveFormat(44100, 16, 1), LAMEPreset.STANDARD);
                }
                else
                {
                    wavWriter = new WaveFileWriter(path, new WaveFormat(44100, 16, 1));
                }

                recordingPath = path;
                return path;
            }
            catch
            {
                wavWriter?.Dispose(); mp3Writer?.Dispose(); wavWriter = null; mp3Writer = null; throw;
            }
        }
    }

    public string? StopRecording()
    {
        lock (gate)
        {
            string? path = recordingPath;
            recordingPath = null;
            try
            {
                wavWriter?.Dispose(); 
                mp3Writer?.Dispose(); 
            }
            finally
            {
                wavWriter = null; 
                mp3Writer = null; 
            }
            return path;
        }
    }

    public void Stop() { lock (gate) StopCore(); }
    private void StopCore()
    {
        IsRunning = false;
        try 
        { 
            capture?.StopRecording();
        }
        catch
        {
        }
        capture?.Dispose(); 
        capture = null;
        playback?.Stop();
        playback?.Dispose();
        playback = null;
        buffer = null;
        StopRecording();
    }
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true; 
        Stop(); 
    }
}
