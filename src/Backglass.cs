using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;


using System.Threading;
using System.Windows.Forms;

namespace CabinetBackglass
{
    sealed class GameFrame
    {
        // JVPPBPort v0.2.0 testhooks.c DumpGame writes a six-byte header followed
        // by the GBA layout documented in include/global.h.
        internal const int Size = 6 + 0x1410;
        internal uint Number;
        internal int DemoVariant, Area, Caught, Species, Balls, Coins, BallType, Seconds, BoardState;
        internal long Score;

        internal static GameFrame Decode(byte[] buffer)
        {
            if (buffer.Length != Size) throw new ArgumentException("Unexpected game-state record length.");
            const int data = 6;
            int timerMode = buffer[data + 0x294];
            return new GameFrame {
                Number = BitConverter.ToUInt32(buffer, 0),
                DemoVariant = BitConverter.ToUInt16(buffer, 4),
                Area = buffer[data + 0x035],
                BoardState = buffer[data + 0x013],
                Balls = Math.Min(10, buffer[data + 0x030] + 1),
                Score = BitConverter.ToUInt32(buffer, data + 0x048) * 100000000L
                    + BitConverter.ToUInt32(buffer, data + 0x044),
                Coins = buffer[data + 0x192],
                Seconds = timerMode == 1 || timerMode == 2
                    ? (BitConverter.ToUInt16(buffer, data + 0x296) + 59) / 60 : 0,
                Species = BitConverter.ToUInt16(buffer, data + 0x598),
                Caught = BitConverter.ToUInt16(buffer, data + 0x5F0),
                BallType = buffer[data + 0x5F6]
            };
        }
    }

    sealed class CabinetState
    {
        internal GameFrame Current;
        internal readonly List<string> Recent = new List<string>();
        internal DateTime LastFrame;
        GameFrame previous;
        bool attract, historyIncomplete, enteringGame;
        internal bool HistoryIncomplete { get { return historyIncomplete; } }

        internal void Apply(GameFrame frame, DateTime now)
        {
            bool gap = previous != null && frame.Number != unchecked(previous.Number + 1);
            if (previous == null || frame.DemoVariant != previous.DemoVariant)
                attract = frame.DemoVariant != 0;
            else if (gap)
                attract = false;
            previous = frame;
            if (attract || frame.Area >= 14 || frame.Caught > 999 || frame.Score > 999999999999L) return;

            // The first record after a menu can precede board initialization.
            // Wait for gameplay setup to advance before reading the new run.
            if (gap)
            {
                enteringGame = true;
                return;
            }
            if (enteringGame && frame.BoardState == 0 && frame.Coins == 0 && frame.Score == 0)
                return;
            enteringGame = false;

            if (Current == null || frame.Caught < Current.Caught ||
                (frame.Score == 0 && Current.Score > 0 && frame.BoardState == 0))
            {
                Recent.Clear();
                historyIncomplete = frame.Caught > 0;
            }
            else if (frame.Caught > Current.Caught)
            {
                int gained = frame.Caught - Current.Caught;
                if (gained == 2 && frame.Species == 42)
                    Remember("Shedinja");
                else if (gained > 1)
                    historyIncomplete = true;
                if (frame.Species < SpeciesNames.All.Length)
                    Remember(SpeciesNames.All[frame.Species]);
                else
                    historyIncomplete = true;
            }

            Current = frame;
            LastFrame = now;
        }

        void Remember(string name)
        {
            Recent.Insert(0, name);
            if (Recent.Count > 5) Recent.RemoveAt(5);
        }
    }

    sealed class LiveSession : IDisposable
    {
        readonly NamedPipeServerStream pipe;
        readonly Process game;
        readonly object stateLock = new object();
        readonly CabinetState state = new CabinetState();
        volatile bool closing;
        volatile bool connected;
        volatile bool disconnected;

        internal LiveSession(string gamePath)
        {
            string pipeName = "jvppbport-backglass-" + Guid.NewGuid().ToString("N");
            pipe = new NamedPipeServerStream(pipeName, PipeDirection.In, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous, GameFrame.Size * 16, 0);
            var start = new ProcessStartInfo(gamePath) {
                WorkingDirectory = Path.GetDirectoryName(gamePath), UseShellExecute = false
            };
            start.EnvironmentVariables["JVPPBPORT_GAMEDUMP"] = @"\\.\pipe\" + pipeName;
            var reader = new Thread(ReadFrames) { IsBackground = true, Name = "JVPPBPort state reader" };
            reader.Start();
            try
            {
                game = Process.Start(start);
                if (game == null) throw new InvalidOperationException("Windows did not start JVPPBPort.");
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal bool Ended { get { return game.HasExited; } }

        void ReadFrames()
        {
            try
            {
                pipe.WaitForConnection();
                connected = true;
                var buffer = new byte[GameFrame.Size];
                int size = 0;
                while (!closing)
                {
                    int count = pipe.Read(buffer, size, buffer.Length - size);
                    if (count == 0) break;
                    size += count;
                    if (size < buffer.Length) continue;
                    var frame = GameFrame.Decode(buffer);
                    lock (stateLock) state.Apply(frame, DateTime.UtcNow);
                    size = 0;
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            finally { disconnected = true; }
        }

        internal DisplayState Snapshot()
        {
            lock (stateLock)
            {
                if (disconnected) return new DisplayState(state, "LINK CLOSED");
                if (state.Current == null)
                    return new DisplayState(state, connected ? "START A GAME" : "WAITING FOR JVPPBPORT");
                return new DisplayState(state, DateTime.UtcNow - state.LastFrame < TimeSpan.FromSeconds(2)
                    ? "LIVE" : "LAST LIVE GAME");
            }
        }

        public void Dispose()
        {
            closing = true;
            pipe.Dispose();
            if (game != null) game.Dispose();
        }
    }

    sealed class DisplayState
    {
        internal readonly string Status;
        internal readonly GameFrame Frame;
        internal readonly string[] Recent;
        internal readonly bool HistoryIncomplete;

        internal DisplayState(CabinetState source, string status)
        {
            Status = status;
            Frame = source.Current;
            Recent = source.Recent.ToArray();
            HistoryIncomplete = source.HistoryIncomplete;
        }
    }

    static class Program
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main(string[] args)
        {
            SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                bool demo = args.Contains("--demo");
                Screen[] screens = Screen.AllScreens.OrderBy(screen => screen.DeviceName, StringComparer.OrdinalIgnoreCase).ToArray();
                int index = Array.IndexOf(args, "--screen");
                if (index >= 0)
                {
                    int screen;
                    if (index + 1 >= args.Length || !int.TryParse(args[index + 1], out screen) || screen < 1 || screen > screens.Length)
                        throw new ArgumentException("Choose a valid --screen number from the setup window.");
                    string game = GamePath(args);
                    if (!demo) VerifyGame(game);
                    Application.Run(new BackglassForm(screens[screen - 1], game, demo));
                    return;
                }
                using (var picker = new SetupForm(screens, GamePath(args), demo))
                    if (picker.ShowDialog() == DialogResult.OK)
                        Application.Run(new BackglassForm(picker.SelectedScreen, picker.Game, picker.Demo));
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Cabinet Backglass", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static string GamePath(string[] args)
        {
            int index = Array.IndexOf(args, "--game");
            if (index < 0) return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JVPPBPort.exe");
            if (index == args.Length - 1) throw new ArgumentException("--game needs the path to JVPPBPort.exe.");
            return Path.GetFullPath(args[index + 1]);
        }

        internal static void VerifyGame(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Select the JVPPBPort.exe that you already use on your cabinet.");
            var info = FileVersionInfo.GetVersionInfo(path);
            if (info.ProductVersion != "0.2.0" || !Path.GetFileName(path).Equals("JVPPBPort.exe", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Select the official Windows JVPPBPort.exe from release 0.2.0. The game-state format is version specific.");
        }
    }

    sealed class SetupForm : Form
    {
        readonly ComboBox monitorList = new ComboBox();
        readonly TextBox file = new TextBox();
        readonly Screen[] screens;
        internal Screen SelectedScreen { get { return screens[monitorList.SelectedIndex]; } }
        internal string Game { get { return file.Text.Trim(); } }
        internal bool Demo { get; private set; }

        internal SetupForm(Screen[] choices, string game, bool demo)
        {
            screens = choices;
            Text = "Cabinet Backglass";
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(550, 215);
            var screenLabel = new Label { Text = "Backglass display", AutoSize = true, Location = new Point(18, 17) };
            monitorList.SetBounds(18, 42, 512, 29);
            monitorList.DropDownStyle = ComboBoxStyle.DropDownList;
            int selected = 0;
            for (int i = 0; i < choices.Length; i++)
            {
                Screen display = choices[i];
                monitorList.Items.Add(string.Format("{0}   {1}   {2} x {3}{4}", i + 1, display.DeviceName,
                    display.Bounds.Width, display.Bounds.Height, display.Primary ? "   (primary)" : ""));
                if (choices[selected].Primary && !display.Primary) selected = i;
            }
            monitorList.SelectedIndex = selected;

            var pathLabel = new Label { Text = "JVPPBPort 0.2.0 executable", AutoSize = true, Location = new Point(18, 87) };
            file.SetBounds(18, 112, 405, 28);
            file.Text = game;
            var browse = new Button { Text = "Browse..." };
            browse.SetBounds(435, 110, 95, 31);
            browse.Click += delegate {
                using (var dialog = new OpenFileDialog { Filter = "JVPPBPort (JVPPBPort.exe)|JVPPBPort.exe" })
                    if (dialog.ShowDialog(this) == DialogResult.OK) file.Text = dialog.FileName;
            };
            var preview = new Button { Text = "Preview" };
            preview.SetBounds(18, 164, 105, 34);
            preview.Click += delegate { Demo = true; DialogResult = DialogResult.OK; };
            var launch = new Button { Text = "Launch game" };
            launch.SetBounds(399, 164, 131, 34);
            launch.Click += delegate {
                try { Program.VerifyGame(Game); Demo = false; DialogResult = DialogResult.OK; }
                catch (Exception error) { MessageBox.Show(this, error.Message, Text); }
            };
            AcceptButton = demo ? preview : launch;
            Controls.AddRange(new Control[] { screenLabel, monitorList, pathLabel, file, browse, preview, launch });
        }
    }

    sealed class BackglassMotion
    {
        internal double Time { get; private set; }
        double areaChanged = -10, caughtAt = -10, scoreAt = -10;
        long fromScore, goalScore;
        int lastArea = -1, lastCount = -1;

        internal void Update(DisplayState view, double time)
        {
            Time = time;
            var frame = view.Frame;
            if (frame == null) return;
            if (lastArea >= 0 && frame.Area != lastArea) areaChanged = time;
            if (lastCount >= 0 && frame.Caught > lastCount) caughtAt = time;
            if (lastCount >= 0 && frame.Caught < lastCount) caughtAt = -10;
            if (frame.Score != goalScore)
            {
                fromScore = lastArea >= 0 && frame.Score > goalScore ? Score : frame.Score;
                goalScore = frame.Score;
                scoreAt = time;
            }
            lastArea = frame.Area;
            lastCount = frame.Caught;
        }

        internal double Travel { get { return Progress(areaChanged, .65); } }
        internal double Catch { get { return Progress(caughtAt, 1.25); } }
        internal long Score
        {
            get
            {
                double progress = Progress(scoreAt, .28);
                double eased = 1 - Math.Pow(1 - progress, 3);
                return fromScore + (long)Math.Round((goalScore - fromScore) * eased);
            }
        }

        double Progress(double start, double duration)
        {
            return Math.Max(0, Math.Min(1, (Time - start) / duration));
        }
    }

    sealed class BackglassForm : Form
    {
        readonly string game;
        readonly bool demo;
        readonly System.Windows.Forms.Timer refresh;
        LiveSession session;
        readonly Stopwatch watch = new Stopwatch();
        readonly BackglassMotion motion = new BackglassMotion();
        readonly PartyIcons partyIcons = new PartyIcons(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PartyIcons"));
        readonly AreaPortraits portraits = new AreaPortraits(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Panoramas"));
        int lastSample = -1;
        DisplayState snapshot;

        internal BackglassForm(Screen target, string executable, bool preview)
        {
            game = executable;
            demo = preview;
            Text = "Cabinet Backglass";
            BackColor = Color.FromArgb(10, 22, 38);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = target.Bounds;
            DoubleBuffered = true;
            KeyPreview = true;
            KeyDown += delegate(object sender, KeyEventArgs key) { if (key.KeyCode == Keys.Escape) Close(); };
            snapshot = new DisplayState(new CabinetState(), "WAITING FOR JVPPBPORT");
            refresh = new System.Windows.Forms.Timer { Interval = 33 };
            refresh.Tick += delegate {
                if (session != null && session.Ended) { Close(); return; }
                double now = watch.Elapsed.TotalSeconds;
                int sample = (int)(now * 8);
                if (sample != lastSample)
                {
                    snapshot = demo ? DemoSnapshot(sample) : session.Snapshot();
                    lastSample = sample;
                }
                motion.Update(snapshot, now);
                Invalidate();
            };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                if (!demo) session = new LiveSession(game);
                else snapshot = DemoSnapshot(0);
                watch.Start();
                refresh.Start();
            }
            catch (Exception error)
            {
                MessageBox.Show(this, error.Message, Text);
                Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            BackglassArt.Render(e.Graphics, ClientSize, snapshot, motion, portraits, partyIcons);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            refresh.Dispose();
            portraits.Dispose();
            partyIcons.Dispose();
            if (session != null) session.Dispose();
            base.OnFormClosed(e);
        }

        internal static DisplayState DemoSnapshot(int ticks)
        {
            int[] habitats = { 1, 5, 9, 12, 2, 6, 8, 10, 11, 0, 4, 3, 7, 13 };
            int stage = (ticks / 48) % habitats.Length;
            int beat = ticks % 48;
            int caught = beat < 16 ? 1 : beat < 32 ? 2 : 3;
            var sample = new CabinetState {
                Current = new GameFrame { Area = habitats[stage],
                    Caught = caught, Score = 12842700 + (ticks / 4) * 2300 + caught * 3400, Balls = 2, Coins = 34,
                    Seconds = 46 - (ticks / 8) % 22, BallType = 1 }
            };
            sample.Recent.AddRange(new[] { "Shroomish", "Lotad", "Mudkip" }.Skip(3-caught));
            return new DisplayState(sample, "PREVIEW");
        }
    }
}
