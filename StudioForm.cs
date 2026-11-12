using System.Speech.Synthesis;
using NAudio.Wave;
namespace ECHO;

public sealed class StudioForm : Form
{
    private readonly AudioEngine audio;
    private readonly Color red = Color.FromArgb(195, 35, 43);
    private readonly Color dark = Color.FromArgb(23, 23, 25);
    private readonly Color pale = Color.FromArgb(229, 222, 217);
    private readonly TextBox narration = new() 
    { 
        Multiline = true, 
        Height = 150, 
        Dock = DockStyle.Top, 
        Text = "I want to play a game..." 
    };

    private readonly ComboBox speechVoices = new() 
    { 
        DropDownStyle = ComboBoxStyle.DropDownList, 
        Width = 370 
    };

    private readonly ListBox tracks = new() 
    { 
        Dock = DockStyle.Fill, 
        SelectionMode = SelectionMode.MultiExtended 
    };

    private readonly ComboBox outputDevices = new() 
    { 
        DropDownStyle = ComboBoxStyle.DropDownList, 
        Width = 440 
    };

    private readonly NumericUpDown buffer = new() 
    { 
        Minimum = 10, 
        Maximum = 100, 
        Value = 30, 
        Increment = 5 
    };

    private readonly CheckBox muteStartup = new() 
    { 
        Text = "Mute startup laughter", 
        AutoSize = true 
    };

    public StudioForm(AudioEngine engine)
    {
        audio = engine;
        Text = "ECHO | HORROR STUDIO"; 
        Size = new Size(860, 690); 
        MinimumSize = new Size(750, 600);
        BackColor = dark; 
        ForeColor = pale; 
        Font = new Font("Segoe UI", 10);
        var tabs = new TabControl 
        {
        Dock = DockStyle.Fill }; 
        Controls.Add(tabs);
        AddRack(tabs); 
        AddSoundboard(tabs); 
        AddNarrator(tabs); 
        AddTracks(tabs); 
        AddRouting(tabs);
    }

    private TabPage Page(TabControl tabs, string name)
    {
        var page = new TabPage(name) 
        {
            BackColor = dark, 
            ForeColor = pale, 
            Padding = new Padding(20) 
        };

        tabs.TabPages.Add(page); 
        return page;
    }

    private Button Button(string label, Action click)
    {
        var button = new Button 
        {
            Text = label, 
            AutoSize = true, 
            MinimumSize = new Size(180, 40), 
            BackColor = Color.FromArgb(95, 27, 34), 
            ForeColor = pale, 
            FlatStyle = FlatStyle.Flat, 
            Margin = new Padding(6) 
        };

        button.Click += (_, _) => 
        {
            try
            {
                click(); 
            } 
            catch (Exception ex) 
            {
                MessageBox.Show(this, ex.Message, 
                    "ECHO", MessageBoxButtons.OK, 
                    MessageBoxIcon.Error); 
            }
        };
        return button;
    }

    private static FlowLayoutPanel Flow() => new() 
    {
        Dock = DockStyle.Fill, 
        AutoScroll = true, 
        WrapContents = false, 
        FlowDirection = FlowDirection.TopDown 
    };

    private void AddRack(TabControl tabs)
    {
        var flow = Flow(); 
        Page(tabs, "Effects rack").Controls.Add(flow);
        flow.Controls.Add(new Label
        {
            Text = "LIVE FX / Changes take effect immediately", 
            ForeColor = red, 
            AutoSize = true 
        });

        foreach ((string? name, float value, Action<float>? apply) in new (string, float, Action<float>)[]
        {
            ("Drive / distortion",audio.Rack.Drive,v=>audio.Rack.Drive=v),
            ("Delay",audio.Rack.Delay,v=>audio.Rack.Delay=v),
            ("Tremolo",audio.Rack.Tremolo,v=>audio.Rack.Tremolo=v),
            ("Tone / low-pass EQ",audio.Rack.Tone,v=>audio.Rack.Tone=v),
            ("Chorus",audio.Rack.Chorus,v=>audio.Rack.Chorus=v),
            ("Noise gate",audio.Rack.Gate,v=>audio.Rack.Gate=v),
            ("Compression",audio.Rack.Compression,v=>audio.Rack.Compression=v),
            ("Reverb",audio.Rack.Reverb,v=>audio.Rack.Reverb=v)
        })
        {
            flow.Controls.Add(new Label 
            {
                Text = name, 
                AutoSize = true, 
                ForeColor = pale, 
                Margin = new Padding(4, 14, 4, 0) 
            });
            var slider = new TrackBar 
            {
                Minimum = 0, 
                Maximum = 100, 
                Value = (int)(value * 100), 
                Width = 520, TickFrequency = 10 
            };
            slider.ValueChanged += (_, _) => apply(slider.Value / 100f);
            flow.Controls.Add(slider);
        }
        var enabled = new CheckBox 
        {
            Text = "Enable effects rack", 
            Checked = audio.Rack.Enabled, 
            AutoSize = true };
        enabled.CheckedChanged += (_, _) => audio.Rack.Enabled = enabled.Checked;
        flow.Controls.Add(enabled);
    }
    private void AddSoundboard(TabControl tabs)
    {
        var flow = Flow(); 
        Page(tabs, "Soundboard").Controls.Add(flow);
        flow.Controls.Add(new Label 
        {
            Text = "ORIGINAL SYNTHESIZED HORROR EFFECTS", 
            AutoSize = true, 
            ForeColor = red 
        });
        foreach (var effect in new[]
        { "Laughter", 
            "Heartbeat", 
            "Whisper", 
            "Metal", 
            "Scream", 
            "Footsteps" 
        })
        {
            flow.Controls.Add(Button("PLAY / " + effect, () => Soundboard.Play(effect)));
        }

        flow.Controls.Add(new Label 
        {
            Text = "Soundboard effects play on the default speakers; they are not mixed into microphone capture.", 
            AutoSize = true, 
            ForeColor = pale 
        });
    }
    private void AddNarrator(TabControl tabs)
    {
        var flow = Flow(); 
        Page(tabs, "Narrator").Controls.Add(flow);
        flow.Controls.Add(new Label 
        { 
            Text = "OFFLINE WINDOWS TEXT-TO-SPEECH", 
            AutoSize = true, 
            ForeColor = red 
        });
        try
        {
            using var synth = new SpeechSynthesizer();
            foreach (var voice in synth.GetInstalledVoices())
            {
                speechVoices.Items.Add(voice.VoiceInfo.Name);
            }

            if (speechVoices.Items.Count > 0) speechVoices.SelectedIndex = 0;
        }
        catch (Exception ex) 
        { 
            flow.Controls.Add(new Label 
            { 
                Text = "Speech voices unavailable: " + ex.Message, 
                AutoSize = true 
            }); 
        }
        flow.Controls.Add(speechVoices);
        narration.Width = 700; flow.Controls.Add(narration);
        flow.Controls.Add(Button("RENDER NARRATION TO WAV", () =>
        {
            if (string.IsNullOrWhiteSpace(narration.Text))
            {
                throw new InvalidOperationException("Enter narration text.");
            }

            var path = Path.Combine(AudioUtilities.OutputFolder(), 
                "ECHO_Narration_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav");
            using var synth = new SpeechSynthesizer();
            if (speechVoices.SelectedItem is string voice)
            {
                synth.SelectVoice(voice);
            }

            synth.SetOutputToWaveFile(path); 
            synth.Speak(narration.Text);
            MessageBox.Show(this, "Saved: " + path);
        }));
    }
    private void AddTracks(TabControl tabs)
    {
        var page = Page(tabs, "Track mixer");
        var layout = new TableLayoutPanel 
        { 
            Dock = DockStyle.Fill, 
            RowCount = 2, 
            ColumnCount = 1 
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); 
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        page.Controls.Add(layout); layout.Controls.Add(tracks, 0, 0);
        var buttons = new FlowLayoutPanel 
        { 
            Dock = DockStyle.Fill 
        }; 
        layout.Controls.Add(buttons, 0, 1);
        buttons.Controls.Add(Button("ADD AUDIO TRACKS", () =>
        {
            using var dialog = new OpenFileDialog 
            { 
                Filter = "Audio files|*.wav;*.mp3", 
                Multiselect = true 
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                foreach (var path in dialog.FileNames) tracks.Items.Add(path);
            }
        }));
        buttons.Controls.Add(Button("MIX TO WAV", () =>
        {
            var files = tracks.Items.Cast<string>().ToArray();
            string path = AudioUtilities.MixWavFiles(files, "ECHO_Studio");
            MessageBox.Show(this, "Mixed: " + path);
        }));
        buttons.Controls.Add(Button("EXPORT MIX TO MP3", () =>
        {
            string wav = AudioUtilities.MixWavFiles([.. tracks.Items.Cast<string>()], "ECHO_Studio");
            string mp3 = AudioUtilities.ExportMp3(wav);
            MessageBox.Show(this, "Exported: " + mp3);
        }));
    }
    private void AddRouting(TabControl tabs)
    {
        var flow = Flow(); 
        Page(tabs, "Audio routing").Controls.Add(flow);
        flow.Controls.Add(new Label 
        { 
            Text = "OUTPUT DEVICE / select a virtual cable playback endpoint if installed", 
            ForeColor = red, 
            AutoSize = true 
        });
        outputDevices.Items.Add("Windows default output");
        for (int i = 0;
            i < WaveOut.DeviceCount; 
            i++)
        {
            outputDevices.Items.Add(WaveOut.GetCapabilities(i).ProductName);
        }

        outputDevices.SelectedIndex = Math.Clamp(audio.OutputDeviceNumber + 1, 0, 
            outputDevices.Items.Count - 1);
        flow.Controls.Add(outputDevices);
        flow.Controls.Add(new Label 
        { 
            Text = "Capture buffer (milliseconds) — restart engine after changing", 
            AutoSize = true 
        });
        buffer.Value = audio.CaptureBufferMs; flow.Controls.Add(buffer);
        flow.Controls.Add(Button("APPLY DEVICE & BUFFER", () =>
        {
            if (audio.IsRunning)
            {
                throw new InvalidOperationException("Stop the voice engine before changing audio routing.");
            }

            audio.OutputDeviceNumber = outputDevices.SelectedIndex - 1;
            audio.CaptureBufferMs = (int)buffer.Value;
            MessageBox.Show(this, "Settings applied. Restart the engine.");
        }));
        flow.Controls.Add(new Label 
        { 
            Text = "Virtual microphone support requires a separately installed, signed virtual audio cable driver. ECHO does not install kernel drivers.", 
            AutoSize = true, 
            MaximumSize = new Size(650, 0) 
        });
    }
}
