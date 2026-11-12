using NAudio.Lame;
using NAudio.Wave;
namespace ECHO;

public static class AudioUtilities
{
    public static string OutputFolder()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop))
        {
            throw new IOException("Desktop folder unavailable.");
        }

        string path = Path.Combine(desktop, "ECHO");
        Directory.CreateDirectory(path);
        return path;
    }

    public static string MixWavFiles(IReadOnlyList<string> files, string name)
    {
        if (files.Count < 1)
        {
            throw new ArgumentException("Choose at least one WAV track.");
        }

        var readers = new List<AudioFileReader>();
        try
        {
            foreach (string file in files)
            {
                readers.Add(new AudioFileReader(file));
            }

            var format = readers[0].WaveFormat;
            if (readers.Any(r => r.WaveFormat.SampleRate != format.SampleRate || r.WaveFormat.Channels != format.Channels))
            {
                throw new InvalidOperationException("All tracks must have matching sample rates and channel counts.");
            }

            string path = Path.Combine(OutputFolder(), name + "_mix_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav");
            using var writer = new WaveFileWriter(path, WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRate, format.Channels));
            var blocks = readers.Select(_ => new float[4096]).ToArray();
            var mixed = new float[4096];
            while (true)
            {
                Array.Clear(mixed);
                int maximum = 0;
                for (int j = 0;
                    j < readers.Count; 
                    j++)
                {
                    int read = readers[j].Read(blocks[j], 0, blocks[j].Length);
                    maximum = Math.Max(maximum, read);
                    for (int i = 0; 
                        i < read; 
                        i++)
                    {
                        mixed[i] += blocks[j][i] / readers.Count;
                    }
                }
                if (maximum == 0)
                {
                    break;
                }

                for (int i = 0; i < maximum; i++) writer.WriteSample(Math.Clamp(mixed[i], -1, 1));
            }
            return path;
        }
        finally { foreach (var reader in readers)
            {
                reader.Dispose();
            }
        }
    }

    public static string ExportMp3(string inputFile)
    {
        string path = Path.Combine(OutputFolder(), Path.GetFileNameWithoutExtension(inputFile) + "_export.mp3");
        using var reader = new AudioFileReader(inputFile);
        using var resampled = new MediaFoundationResampler(reader, new WaveFormat(44100, 16, 2)) 
        { 
            ResamplerQuality = 60 
        };
        using var writer = new LameMP3FileWriter(path, resampled.WaveFormat, LAMEPreset.STANDARD);

        // Create a buffer to hold the audio data as it's processed
        byte[] buffer = new byte[resampled.WaveFormat.AverageBytesPerSecond];
        int bytesRead;

        // Read from the resampler and write to the MP3 writer until finished
        while ((bytesRead = resampled.Read(buffer, 0, buffer.Length)) > 0)
        {
            writer.Write(buffer, 0, bytesRead);
        }

        return path;
    }
}
