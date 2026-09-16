using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
[assembly: AssemblyTitle("Miareve Study Guard")]
[assembly: AssemblyDescription("디자인 · After Effects 공부 알림 / Beta 1.0.0")]
[assembly: AssemblyCompany("Miareve")]
[assembly: AssemblyProduct("Miareve Study Guard")]
[assembly: AssemblyCopyright("Copyright © 2026 Miareve")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0-beta")]
namespace Miareve {
    internal static class Program {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
        [STAThread] static int Main(string[] args) {
            SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            try {
                using (MainForm f = new MainForm()) {
                    if (args.Length == 2 && args[0] == "--render-preview") {
                        f.Show(); Application.DoEvents();
                        using (Bitmap b = new Bitmap(f.Width, f.Height)) { f.DrawToBitmap(b, new Rectangle(0,0,f.Width,f.Height)); b.Save(args[1]); }
                        f.Close(); return 0;
                    }
                    Application.Run(f);
                }
                return 0;
            } catch (Exception ex) {
                MessageBox.Show("앱을 시작하지 못했습니다.\n" + ex.Message, "Miareve Study Guard", MessageBoxButtons.OK, MessageBoxIcon.Error); return 1;
            }
        }
    }
    internal static class Native {
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet=CharSet.Unicode)] internal static extern int GetWindowText(IntPtr h, StringBuilder b, int length);
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [StructLayout(LayoutKind.Sequential)] internal struct LastInput { public uint size; public uint time; }
        [DllImport("user32.dll")] static extern bool GetLastInputInfo(ref LastInput info);
        internal static double Idle() { LastInput l = new LastInput(); l.size = (uint)Marshal.SizeOf(l); if (!GetLastInputInfo(ref l)) throw new InvalidOperationException("입력 상태를 읽지 못했습니다."); return unchecked((uint)Environment.TickCount-l.time)/1000.0; }
    }
    public sealed class MainForm : Form {
        readonly Color bg = Color.FromArgb(17,20,30), panel = Color.FromArgb(28,33,47), muted = Color.FromArgb(158,170,195), accent = Color.FromArgb(183,165,255);
        readonly Timer timer = new Timer(); readonly Stopwatch watch = Stopwatch.StartNew();
        readonly FocusClock clock = new FocusClock();
        Label status, reason, stats, privacy; CheckBox consent; NumericUpDown minutes; TextBox keywords; Button start;
        NotifyIcon tray; Reminder reminder; object plugin; Type pluginType;
        bool running, locked; double lastTick, breakUntil; string loadError;
        public MainForm() {
            Text = "Miareve Study Guard · 베타 1.0.0Ver"; ClientSize = new Size(820, 780); MinimumSize = new Size(836, 819);
            StartPosition = FormStartPosition.CenterScreen; BackColor = bg; ForeColor = Color.White; Font = new Font("맑은 고딕", 10);
            AutoScaleMode = AutoScaleMode.Dpi;
            var root = new TableLayoutPanel { Dock=DockStyle.Fill, AutoScroll=true, Padding=new Padding(28), ColumnCount=1, RowCount=0 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); Controls.Add(root);
            Add(root, L("M I A R E V E   /   S T U D Y  G U A R D",10,accent),32);
            Add(root, L("오늘의 한 장면을 완성하자.",25,Color.White),62);
            Add(root, L("디자인 · After Effects · Illustrator · Blender · Unity",10,muted),32);
            var card = new Panel { Dock=DockStyle.Fill, BackColor=panel, Padding=new Padding(18) };
            status = L("시작할 준비가 됐어요",17,accent); status.Location=new Point(18,14); status.Size=new Size(620,38);
            reason = L("화면 접근을 허용한 뒤 공부 시작을 눌러 주세요.",10,Color.White); reason.Location=new Point(18,58); reason.Size=new Size(620,42);
            stats = L("이번 세션  00:00:00  ·  공부 이탈  00:00",10,muted); stats.Location=new Point(18,103); stats.Size=new Size(620,28);
            card.Controls.AddRange(new Control[]{status,reason,stats}); Add(root,card,151);
            Add(root,L("화면 접근 플러그인",12,Color.White),40);
            consent = new CheckBox { Text="화면 보기 허용 (현재 실행 중에만)", AutoSize=true, ForeColor=Color.White, Dock=DockStyle.Fill };
            consent.CheckedChanged += delegate { OnConsent(); }; Add(root,consent,34);
            privacy = L("5초마다 활성 창이 있는 모니터를 메모리에서 분석합니다.\n화면 이미지·창 제목 저장 없음 / 외부 전송 없음 / 언제든 허용 해제",9,muted); Add(root,privacy,52);
            var settings = new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=false };
            settings.Controls.Add(L("공부 이탈 알림까지",10,Color.White));
            minutes = new NumericUpDown { Minimum=1, Maximum=60, Value=5, Width=65, BackColor=panel, ForeColor=Color.White };
            settings.Controls.Add(minutes); settings.Controls.Add(L("분  (다시 알림도 같은 간격)",10,muted)); Add(root,settings,43);
            Add(root,L("강의·자료 창 제목에 포함되면 공부로 볼 단어 (쉼표로 구분)",10,Color.White),32);
            keywords = new TextBox { Text=StudyPolicy.DefaultKeywords, Multiline=true, ScrollBars=ScrollBars.Vertical, Dock=DockStyle.Fill, BackColor=panel, ForeColor=Color.White, BorderStyle=BorderStyle.FixedSingle };
            Add(root,keywords,61);
            var buttons = new FlowLayoutPanel { Dock=DockStyle.Fill, Padding=new Padding(0,12,0,0), WrapContents=false };
            start = B("공부 시작",accent); start.Enabled=false; start.Click += delegate { Toggle(); };
            var pause=B("10분 쉬기",panel); pause.Click += delegate { TakeBreak(); };
            var test=B("알림 테스트",panel); test.Click += delegate { ShowReminder(); };
            var hide=B("트레이로",panel); hide.Click += delegate { Hide(); tray.ShowBalloonTip(2000,"Miareve Study Guard","트레이 아이콘을 두 번 클릭하면 돌아옵니다.",ToolTipIcon.Info); };
            buttons.Controls.AddRange(new Control[]{start,pause,test,hide}); Add(root,buttons,66);
            Add(root,L("규칙 기반 추정이므로 오판할 수 있어요. AI가 학습 내용을 이해하는 방식은 아닙니다.\n제작자 Miareve   ·   베타 1.0.0Ver   ·   닫기(X)는 앱 완전 종료",9,muted),55);
            tray = new NotifyIcon { Icon=SystemIcons.Information, Text="Miareve Study Guard · 대기", Visible=true };
            var menu = new ContextMenuStrip(); menu.Items.Add("앱 열기",null,delegate { Show(); WindowState=FormWindowState.Normal; Activate(); });
            menu.Items.Add("10분 쉬기",null,delegate { TakeBreak(); }); menu.Items.Add("화면 허용 해제",null,delegate { consent.Checked=false; }); menu.Items.Add("종료",null,delegate { Close(); }); tray.ContextMenuStrip=menu;
            tray.DoubleClick += delegate { Show(); WindowState=FormWindowState.Normal; Activate(); };
            try {
                using (Stream s=Assembly.GetExecutingAssembly().GetManifestResourceStream("ScreenAccessPlugin.dll")) {
                    byte[] bytes=new byte[s.Length]; int offset=0, read; while ((read=s.Read(bytes,offset,bytes.Length-offset))>0) offset+=read;
                    pluginType=Assembly.Load(bytes).GetType("Miareve.Plugins.ScreenAccessPlugin",true); plugin=Activator.CreateInstance(pluginType);
                }
            } catch (Exception ex) { loadError=ex.GetBaseException().Message; consent.Enabled=false; reason.Text="화면 플러그인을 불러오지 못했습니다: " + loadError; }
            timer.Interval=5000; timer.Tick += delegate { Tick(); }; timer.Start();
            SystemEvents.SessionSwitch += SessionSwitch; SystemEvents.PowerModeChanged += PowerChanged;
        }
        Label L(string text,int size,Color color) { return new Label { Text=text, Font=new Font("맑은 고딕",size), ForeColor=color, AutoSize=true, Margin=new Padding(0,4,8,0) }; }
        Button B(string text,Color color) { return new Button { Text=text, BackColor=color, ForeColor=color==accent?bg:Color.White, FlatStyle=FlatStyle.Flat, Size=new Size(140,38), Margin=new Padding(0,0,10,0), Cursor=Cursors.Hand }; }
        void Add(TableLayoutPanel root,Control c,int height) { int row=root.RowCount++; root.RowStyles.Add(new RowStyle(SizeType.Absolute,height)); c.Dock=DockStyle.Fill; root.Controls.Add(c,0,row); }
        void EnablePlugin(bool enabled) { if (plugin!=null) pluginType.GetMethod("SetEnabled").Invoke(plugin,new object[]{enabled}); }
        void OnConsent() {
            if (!consent.Checked) { Stop(); privacy.Text="화면 접근 꺼짐 · 메모리 분석 데이터 삭제됨\n다시 허용하기 전에는 화면과 창 정보를 읽지 않습니다."; }
            else privacy.Text="화면 접근 허용됨 · 공부 시작을 누르면 분석합니다.\n화면 이미지·창 제목 저장 없음 / 외부 전송 없음";
            start.Enabled=consent.Checked && plugin!=null;
        }
        void Stop() { running=false; breakUntil=0; EnablePlugin(false); clock.ResetAway(); start.Text="공부 시작"; status.Text="일시 정지 · 화면 접근 중지"; reason.Text="공부 시작을 누르면 새로 판단합니다."; tray.Text="Miareve Study Guard · 정지"; CloseReminder(); }
        void Toggle() {
            if (running) { Stop(); return; } if (!consent.Checked || plugin==null) return;
            running=true; breakUntil=0; clock.ResetAway(); lastTick=watch.Elapsed.TotalSeconds; EnablePlugin(true); start.Text="일시 정지";
            status.Text="공부 상태 확인 중"; reason.Text="공부할 앱이나 강의 창으로 이동해 주세요."; tray.Text="Miareve Study Guard · 화면 분석 중";
        }
        void TakeBreak() { if (!running) return; breakUntil=watch.Elapsed.TotalSeconds+600; EnablePlugin(false); clock.ResetAway(); CloseReminder(); status.Text="10분 휴식 · 화면 접근 중지"; reason.Text="휴식이 끝나면 자동으로 다시 확인합니다."; tray.Text="Miareve Study Guard · 휴식"; }
        void CloseReminder() { if (reminder!=null && !reminder.IsDisposed) reminder.Close(); reminder=null; }
        void ShowReminder() {
            if (reminder!=null && !reminder.IsDisposed) return;
            reminder=new Reminder(delegate { clock.ResetAway(); }, delegate { TakeBreak(); }); reminder.Show();
            System.Media.SystemSounds.Exclamation.Play();
        }
        void SessionSwitch(object sender,SessionSwitchEventArgs e) {
            if (IsDisposed || !IsHandleCreated) return;
            BeginInvoke((Action)delegate { locked=e.Reason!=SessionSwitchReason.SessionUnlock && e.Reason!=SessionSwitchReason.SessionLogon; EnablePlugin(false); clock.ResetAway(); lastTick=watch.Elapsed.TotalSeconds; CloseReminder(); });
        }
        void PowerChanged(object sender,PowerModeChangedEventArgs e) {
            if (IsDisposed || !IsHandleCreated) return;
            BeginInvoke((Action)delegate { EnablePlugin(false); clock.ResetAway(); lastTick=watch.Elapsed.TotalSeconds; CloseReminder(); });
        }
        void Tick() {
            double now=watch.Elapsed.TotalSeconds, elapsed=now-lastTick; lastTick=now;
            if (!running || !consent.Checked) return;
            if (locked) { status.Text="화면 잠금 · 분석 중지"; return; }
            if (now<breakUntil) { status.Text="휴식 중 · " + TimeSpan.FromSeconds(breakUntil-now).ToString(@"mm\:ss"); return; }
            if (breakUntil>0) { breakUntil=0; clock.ResetAway(); elapsed=0; }
            // Skip system suspend gaps, rather than crediting unseen time.
            if (elapsed>20) { clock.ResetAway(); elapsed=0; EnablePlugin(false); }
            try {
                IntPtr h=Native.GetForegroundWindow(); uint pid; Native.GetWindowThreadProcessId(h,out pid);
                if (h==IntPtr.Zero || pid==0) throw new InvalidOperationException("활성 창을 확인할 수 없습니다.");
                string name; using (Process p=Process.GetProcessById((int)pid)) name=p.ProcessName;
                if (pid==(uint)Process.GetCurrentProcess().Id) { clock.ResetAway(); status.Text="설정 중 · 공부 앱으로 이동해 주세요"; reason.Text="앱 설정 중에는 공부 시간을 기록하지 않습니다."; return; }
                StringBuilder title=new StringBuilder(1024); Native.GetWindowText(h,title,title.Capacity);
                // Enabling is idempotent only when the plugin was paused; preserve sample history otherwise.
                if (!(bool)pluginType.GetProperty("Enabled").GetValue(plugin,null)) EnablePlugin(true);
                double change=(double)pluginType.GetMethod("Sample").Invoke(plugin,null);
                bool studying=StudyPolicy.IsStudying(name,title.ToString(),keywords.Text,Native.Idle(),change>0.006);
                clock.Tick(studying,elapsed);
                status.Text=studying?"좋아요, 공부 흐름을 이어가요.":"잠깐, 공부에서 벗어났나요?";
                reason.Text=studying?"공부 관련 앱·창과 활동이 확인됐어요. (추정)":"관련 앱·창 또는 활동을 찾지 못했어요. 설정한 시간이 지나면 알려드릴게요.";
                stats.Text="이번 세션  "+TimeSpan.FromSeconds(clock.StudySeconds).ToString(@"hh\:mm\:ss")+"  ·  공부 이탈  "+TimeSpan.FromSeconds(clock.AwaySeconds).ToString(@"mm\:ss");
                tray.Text="Miareve Study Guard · "+(studying?"공부 중 (추정)":"공부 이탈 (추정)");
                if (studying) CloseReminder();
                if (clock.AwaySeconds >= (double)minutes.Value*60) { clock.ResetAway(); ShowReminder(); }
            } catch {
                Stop(); status.Text="분석이 중지됐어요"; reason.Text="화면 또는 앱 정보를 읽지 못했습니다. 화면 잠금을 해제하고 공부 시작을 눌러 주세요.";
            }
        }
        protected override void Dispose(bool disposing) {
            if (disposing) { timer.Stop(); timer.Dispose(); SystemEvents.SessionSwitch-=SessionSwitch; SystemEvents.PowerModeChanged-=PowerChanged; EnablePlugin(false); CloseReminder(); if (tray!=null) { tray.Visible=false; tray.Dispose(); } }
            base.Dispose(disposing);
        }
    }
    internal sealed class Reminder : Form {
        readonly Timer closeTimer=new Timer();
        protected override bool ShowWithoutActivation { get { return true; } }
        public Reminder(Action resume,Action rest) {
            Text="Miareve · 공부 알림"; ClientSize=new Size(420,190); BackColor=Color.FromArgb(28,33,47); ForeColor=Color.White;
            Font=new Font("맑은 고딕",10); FormBorderStyle=FormBorderStyle.FixedToolWindow; TopMost=true; ShowInTaskbar=false; StartPosition=FormStartPosition.Manual;
            Rectangle area=Screen.PrimaryScreen.WorkingArea; Location=new Point(area.Right-Width-20,area.Bottom-Height-20);
            Controls.Add(new Label { Text="공부 안할거냐?", Font=new Font("맑은 고딕",23,FontStyle.Bold), AutoSize=true, Location=new Point(22,20), ForeColor=Color.FromArgb(195,177,255) });
            Controls.Add(new Label { Text="작은 키프레임 하나부터 다시 시작해 보자.", AutoSize=true, Location=new Point(24,75) });
            var back=new Button { Text="지금 할게", Location=new Point(24,120), Size=new Size(165,40) };
            var pause=new Button { Text="10분만 쉴게", Location=new Point(205,120), Size=new Size(185,40) };
            back.Click+=delegate { resume(); Close(); }; pause.Click+=delegate { rest(); Close(); }; Controls.AddRange(new Control[]{back,pause});
            closeTimer.Interval=20000; closeTimer.Tick+=delegate { Close(); }; closeTimer.Start();
        }
        protected override void Dispose(bool disposing) { if (disposing) closeTimer.Dispose(); base.Dispose(disposing); }
    }
}
