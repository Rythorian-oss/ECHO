using NAudio.Wave;

namespace ECHO;

public sealed class MainForm : Form
{
    private readonly AudioEngine audio = new();
    private readonly ComboBox microphones = new();
    private readonly ComboBox voices = new();
    private readonly ComboBox formats = new();
    private readonly CheckBox monitor = new();
    private readonly TrackBar strength = new();
    private readonly TrackBar input = new();
    private readonly TrackBar output = new();
    private readonly Button start = new(), stop = new(), record = new(), folder = new(), studio = new();
    private readonly Label status = new(), recording = new(), levelLabel = new();
    private readonly ProgressBar meter = new();
    private readonly System.Windows.Forms.Timer meterTimer = new() { Interval = 60 };
    private volatile float currentLevel;
    private readonly Color blood = Color.FromArgb(190, 29, 37);
    private readonly Color panel = Color.FromArgb(28, 28, 30);
    private readonly Color pale = Color.FromArgb(226, 219, 211);
    private readonly Color muted = Color.FromArgb(159, 153, 150);

    public MainForm()
    {
        Text = "ECHO | THE VOICE IS YOURS";
        MinimumSize = new Size(840, 760);
        Size = new Size(940, 820);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(14, 14, 16);
        ForeColor = pale;
        Font = new Font("Segoe UI", 10);
        DoubleBuffered = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(30, 20, 30, 20),
            ColumnCount = 1,
            RowCount = 4,
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);

        var header = new Panel
        {
            Dock = DockStyle.Fill
        };
        root.Controls.Add(header, 0, 0);
        var logo = new Label
        {
            Text = "E C H O",
            Font = new Font("Georgia", 39, FontStyle.Bold),
            ForeColor = blood,
            AutoSize = true,
            Location = new Point(0, 1)
        };
        header.Controls.Add(logo);
        header.Controls.Add(new Label
        {
            Text = "THE VOICE IS YOURS. THE FEAR IS THEIRS.",
            Font = new Font("Consolas", 10, FontStyle.Bold),
            ForeColor = muted,
            AutoSize = true,
            Location = new Point(5, 76)
        });

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        root.Controls.Add(content, 0, 1);
        var left = PanelCard(); 
        var right = PanelCard();
        left.Padding = new Padding(22); 
        right.Padding = new Padding(22);
        content.Controls.Add(left, 0, 0); 
        content.Controls.Add(right, 1, 0);

        var leftFlow = Stack(); 
        left.Controls.Add(leftFlow);
        AddHeading(leftFlow, "01 / INPUT & CHARACTER");
        AddCaption(leftFlow, "MICROPHONE");
        ConfigureCombo(microphones);
        leftFlow.Controls.Add(microphones);
        AddCaption(leftFlow, "VOICE PROFILE");
        ConfigureCombo(voices);
        voices.Items.AddRange(["Creepy Child", "Young Woman", "Old Woman", "Boy", "Man", "Puppet / Jigsaw-inspired", "Demon"]);
        voices.SelectedIndex = 5;
        voices.SelectedIndexChanged += (_, _) => audio.Voice = (VoiceKind)voices.SelectedIndex;
        leftFlow.Controls.Add(voices);
        AddCaption(leftFlow, "TRANSFORMATION INTENSITY");
        SetupSlider(strength, 80, value => audio.Strength = value / 100f, leftFlow);
        AddCaption(leftFlow, "MICROPHONE GAIN");
        SetupSlider(input, 100, value => audio.InputGain = value / 100f * 2, leftFlow);
        AddCaption(leftFlow, "OUTPUT GAIN");
        SetupSlider(output, 75, value => audio.OutputGain = value / 100f * 1.5f, leftFlow);
        monitor.Text = "Hear transformed voice (headphones recommended)";
        monitor.Checked = true;
        monitor.AutoSize = true; monitor.ForeColor = pale; 
        monitor.Margin = new Padding(0, 14, 0, 8);
        leftFlow.Controls.Add(monitor);

        var rightFlow = Stack(); 
        right.Controls.Add(rightFlow);
        AddHeading(rightFlow, "02 / CAPTURE & RECORD");
        AddCaption(rightFlow, "RECORDING FORMAT");
        ConfigureCombo(formats);
        formats.Items.AddRange(["WAV · PCM 44.1 kHz", "MP3 · compressed"]);
        formats.SelectedIndex = 0;
        rightFlow.Controls.Add(formats);
        AddCaption(rightFlow, "LIVE OUTPUT LEVEL");
        meter.Dock = DockStyle.Top; 
        meter.Height = 24; 
        meter.Maximum = 100; 
        meter.Margin = new Padding(0, 10, 0, 8);
        rightFlow.Controls.Add(meter);
        levelLabel.Text = "-∞ dBFS"; 
        levelLabel.ForeColor = muted; 
        levelLabel.AutoSize = true;
        rightFlow.Controls.Add(levelLabel);
        start = ConfigureButton("START VOICE ENGINE", blood);
        stop = ConfigureButton("STOP ENGINE", Color.FromArgb(62, 62, 66));
        record = ConfigureButton("● START RECORDING", Color.FromArgb(92, 24, 28));
        folder = ConfigureButton("OPEN ECHO FOLDER", Color.FromArgb(48, 48, 50));
        studio = ConfigureButton("OPEN HORROR STUDIO / FX", Color.FromArgb(74, 32, 39));
        foreach (var button in new[]
        {
            start,
            stop,
            record,
            folder,
            studio
        })
        {
            rightFlow.Controls.Add(button);
        }

        stop.Enabled = false; record.Enabled = false;
        start.Click += (_, _) => StartEngine();
        stop.Click += (_, _) => StopEngine();
        record.Click += (_, _) => ToggleRecording();
        folder.Click += (_, _) => OpenFolder();
        studio.Click += (_, _) => new StudioForm(audio).Show(this);
        recording.AutoSize = true; 
        recording.ForeColor = muted; 
        recording.Text = "No active recording";
        recording.MaximumSize = new Size(360, 0);
        rightFlow.Controls.Add(recording);

        var bottom = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 16, 8, 0)
        };
        root.Controls.Add(bottom, 0, 2);
        status.Text = "STANDBY  /  Select a microphone, then start the engine.";
        status.ForeColor = muted; status.Dock = DockStyle.Top; status.Height = 34;
        bottom.Controls.Add(status);
        var warning = new Label
        {
            Text = "USE HEADPHONES TO PREVENT FEEDBACK  •  ORIGINAL HORROR-INSPIRED AUDIO  •  OFFLINE PROCESSING",
            Dock = DockStyle.Bottom,
            Height = 28,
            ForeColor = muted,
            Font = new Font("Consolas", 8)
        };
        bottom.Controls.Add(warning);
        var footer = new Label
        {
            Text = "ECHO  /  RECORDINGS → WINDOWS DESKTOP / ECHO",
            Dock = DockStyle.Fill,
            ForeColor = blood,
            Font = new Font("Consolas", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(footer, 0, 3);

        audio.LevelChanged += level => currentLevel = level;
        audio.Error += message =>
        {
            if (IsHandleCreated)
            {
                BeginInvoke((Action)(() =>
            {
                status.Text = message;
                status.ForeColor = blood; }));
            }
        };
        meterTimer.Tick += (_, _) =>
        {
            float rms = currentLevel;
            double db = 20 * Math.Log10(Math.Max(rms, 0.00001f));
            meter.Value = Math.Clamp((int)((db + 60) / 60 * 100), 0, 100);
            levelLabel.Text = $"{db:0.0} dBFS";
        };
        meterTimer.Start();
        LoadMicrophones();
        Shown += (_, _) => StartupSound.Play();
        FormClosing += (_, _) =>
        {
            meterTimer.Stop();
            audio.Dispose();
        };
    }

    private static FlowLayoutPanel Stack()
    {
        return new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(0, 0, 8, 0) };
    }

    private Panel PanelCard()
    {
        return new() { Dock = DockStyle.Fill, BackColor = panel, Margin = new Padding(0, 0, 12, 0) };
    }

    private void AddHeading(FlowLayoutPanel parent, string text)
    {
        parent.Controls.Add(new Label { Text = text, AutoSize = true, ForeColor = blood, Font = new Font("Consolas", 12, FontStyle.Bold), Margin = new Padding(0, 4, 0, 22) });
    }

    private void AddCaption(FlowLayoutPanel parent, string text)
    {
        parent.Controls.Add(new Label { Text = text, AutoSize = true, ForeColor = muted, Font = new Font("Consolas", 9, FontStyle.Bold), Margin = new Padding(0, 10, 0, 6) });
    }

    private void ConfigureCombo(ComboBox combo) { combo.DropDownStyle = ComboBoxStyle.DropDownList; combo.Width = 355; combo.Height = 34; combo.BackColor = Color.FromArgb(45, 45, 47); combo.ForeColor = pale; combo.FlatStyle = FlatStyle.Flat; combo.Margin = new Padding(0, 0, 0, 12); }
    private void SetupSlider(TrackBar slider, int value, Action<int> changed, FlowLayoutPanel parent)
    {
        slider.Minimum = 0;
        slider.Maximum = 100; 
        slider.Value = value; 
        slider.TickFrequency = 20; 
        slider.Width = 345; 
        slider.Height = 44;
        slider.BackColor = panel; 
        slider.Margin = new Padding(0, 0, 0, 4);
        slider.ValueChanged += (_, _) => changed(slider.Value);
        parent.Controls.Add(slider);
    }
    private Button ConfigureButton(string text, Color background) => new()
    {
        Text = text,
        Width = 350,
        Height = 48,
        BackColor = background,
        ForeColor = pale,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Consolas", 10, FontStyle.Bold),
        Margin = new Padding(0, 14, 0, 0),
        Cursor = Cursors.Hand
    };

    private void LoadMicrophones()
    {
        microphones.Items.Clear();
        for (int i = 0; i < WaveIn.DeviceCount; i++)
        {
            microphones.Items.Add(WaveIn.GetCapabilities(i).ProductName);
        }

        if (microphones.Items.Count > 0)
        {
            microphones.SelectedIndex = 0;
        }
        else { status.Text = "NO MICROPHONE DETECTED. Connect a recording device."; start.Enabled = false; }
    }
    private void StartEngine()
    {
        try
        {
            if (microphones.SelectedIndex < 0)
            {
                throw new InvalidOperationException("Select a microphone.");
            }

            audio.Voice = (VoiceKind)voices.SelectedIndex;
            audio.Strength = strength.Value / 100f;
            audio.InputGain = input.Value / 100f * 2;
            audio.OutputGain = output.Value / 100f * 1.5f;
            audio.Start(microphones.SelectedIndex, monitor.Checked);
            start.Enabled = false; stop.Enabled = true; record.Enabled = true;
            microphones.Enabled = false; monitor.Enabled = false;
            status.Text = "ENGINE ACTIVE  /  Live transformation running.";
            status.ForeColor = Color.FromArgb(157, 202, 149);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Unable to start audio", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void StopEngine()
    {
        try
        { 
            audio.Stop(); 
        }
        catch (Exception ex) 
        {
            MessageBox.Show(ex.Message, "Audio error");
        }
        start.Enabled = true; stop.Enabled = false; record.Enabled = false;
        microphones.Enabled = true; monitor.Enabled = true;
        record.Text = "● START RECORDING"; recording.Text = "No active recording";
        status.Text = "STANDBY  /  Engine stopped."; status.ForeColor = muted;
    }
    private void ToggleRecording()
    {
        try
        {
            if (!audio.IsRecording)
            {
                string path = audio.StartRecording(formats.SelectedIndex == 1);
                record.Text = "■ STOP RECORDING";
                recording.Text = "RECORDING: " + Path.GetFileName(path);
                recording.ForeColor = blood;
                formats.Enabled = false;
            }
            else
            {
                string? path = audio.StopRecording();
                record.Text = "● START RECORDING";
                recording.Text = "SAVED: " + Path.GetFileName(path);
                recording.ForeColor = pale;
                formats.Enabled = true;
            }
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Recording error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private static void OpenFolder()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var path = Path.Combine(desktop, "ECHO");
            Directory.CreateDirectory(path);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Folder error"); }
    }
}
