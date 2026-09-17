using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
[assembly: AssemblyTitle("Miareve Study Guard")]
[assembly: AssemblyDescription("Design study reminders with optional local vision AI / Beta 1.1")]
[assembly: AssemblyCompany("Miareve")]
[assembly: AssemblyProduct("Miareve Study Guard")]
[assembly: AssemblyCopyright("Copyright © 2026 Miareve")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]
[assembly: AssemblyInformationalVersion("1.1.0-beta")]
namespace Miareve {
 internal static class Program {
  [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
  [STAThread] static int Main(string[] args) {
   SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
   try {
    if(args.Length==2 && args[0]=="--preview") { Preview.Run(args[1]); return 0; }
    using(var single=new Mutex(false,"Local\\MiareveStudyGuard")) {
     bool acquired; try { acquired=single.WaitOne(0); } catch(AbandonedMutexException) { acquired=true; }
     if(!acquired) { MessageBox.Show("이미 실행 중입니다. Ctrl+Alt+H로 열어 주세요. / Already running: press Ctrl+Alt+H."); return 0; }
     try { Application.Run(new MainForm(new Storage(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Miareve","StudyGuard")))); }
     finally { single.ReleaseMutex(); }
    }
    return 0;
   } catch(Exception ex) { MessageBox.Show(ex.Message,"Miareve Study Guard",MessageBoxButtons.OK,MessageBoxIcon.Error); return 1; }
  }
 }
 internal static class Native {
  [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int GetWindowText(IntPtr h,StringBuilder b,int length);
  [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
  [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr h,int id,uint mod,uint key);
  [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr h,int id);
  [StructLayout(LayoutKind.Sequential)] internal struct LastInput { public uint size; public uint time; }
  [DllImport("user32.dll")] static extern bool GetLastInputInfo(ref LastInput info);
  internal static double Idle() { var l=new LastInput(); l.size=(uint)Marshal.SizeOf(l); if(!GetLastInputInfo(ref l)) throw new IOException("Input unavailable"); return unchecked((uint)Environment.TickCount-l.time)/1000.0; }
 }
 public sealed class MainForm : Form {
  readonly Storage storage; Settings settings; readonly DecisionCache cache;
  readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer(); readonly Stopwatch watch=Stopwatch.StartNew(); readonly FocusClock clock=new FocusClock();
  Label status,reason,stats,privacy,hotkeys; CheckBox consent; Button start; NotifyIcon tray; Reminder reminder;
  object plugin; Type pluginType; bool running,locked,editing,busy,disposing; int generation,messageIndex;
  double lastTick,nextSample,nextAi,breakUntil,decisionAt,lastRequest; string context="",lastTitle="",previousImage; Decision decision=Decision.Unknown("");
  CancellationTokenSource aiJob; readonly int ownPid=Process.GetCurrentProcess().Id;
  public MainForm(Storage store) {
   storage=store; settings=store.Load(); cache=new DecisionCache(store.Root);
   Text="Miareve Study Guard · Beta 1.1"; ClientSize=new Size(800,720); MinimumSize=new Size(816,720); StartPosition=FormStartPosition.CenterScreen;
   BackColor=Theme.Background; ForeColor=Color.White; Font=new Font("맑은 고딕",10); AutoScaleMode=AutoScaleMode.Dpi;
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ScreenAccessPlugin.dll")) {
    if(stream==null) throw new IOException("Screen plugin missing");
    using(var ms=new MemoryStream()) { stream.CopyTo(ms); pluginType=Assembly.Load(ms.ToArray()).GetType("Miareve.Plugins.ScreenAccessPlugin",true); plugin=Activator.CreateInstance(pluginType); }
   }
   tray=new NotifyIcon { Icon=SystemIcons.Information,Visible=true,Text="Miareve Study Guard" }; tray.DoubleClick+=delegate { ShowMain(); };
   Build(); timer.Interval=1000; timer.Tick+=delegate { Tick(); }; timer.Start();
   SystemEvents.SessionSwitch+=SessionSwitch; SystemEvents.PowerModeChanged+=PowerChanged;
   if(storage.Warning!=null) reason.Text=storage.Warning;
  }
  string T(string ko,string en) { return settings.T(ko,en); }
  void Build() {
   bool permission=consent!=null && consent.Checked;
   while(Controls.Count>0) Controls[0].Dispose();
   var root=Theme.Table(); Controls.Add(root);
   var brand=Theme.Label("M I A R E V E   /   S T U D Y  G U A R D",10); brand.ForeColor=Theme.Accent; Theme.Row(root,brand,35);
   Theme.Row(root,Theme.Label(T("오늘의 한 장면을 완성하자.","Make your next scene."),25),67);
   var card=Theme.Table(); card.BackColor=Theme.Card; card.Padding=new Padding(14);
   status=Theme.Label(T("시작할 준비가 됐어요","Ready when you are"),17); status.ForeColor=Theme.Accent;
   reason=Theme.Label(T("화면 허용 후 공부 시작을 누르세요.","Allow screen access, then start studying.")); stats=Theme.Label("");
   Theme.Row(card,status,44); Theme.Row(card,reason,76); Theme.Row(card,stats,34); Theme.Row(root,card,185);
   consent=new CheckBox { Text=T("화면 보기 허용 · 이번 실행 중에만","Allow screen access · this session only"),Checked=permission,AutoSize=true };
   consent.CheckedChanged+=delegate { if(!consent.Checked) Stop(); UpdateState(); }; Theme.Row(root,consent,40);
   privacy=Theme.Label(""); privacy.ForeColor=Theme.Muted; Theme.Row(root,privacy,68);
   start=Theme.Button(T("공부 시작","Start"),delegate { Toggle(); }); start.BackColor=Theme.Accent; start.ForeColor=Theme.Background;
   Theme.Row(root,Theme.Flow(start,Theme.Button(T("10분 쉬기","Rest 10 minutes"),delegate { Rest(); }),Theme.Button(T("알림 테스트","Test reminder"),delegate { ShowReminder(); }),Theme.Button(T("설정","Settings"),delegate { OpenSettings(); })),55);
   Theme.Row(root,Theme.Flow(Theme.Button(T("숨기기","Hide"),delegate { Hide(); }),Theme.Button(T("완전 종료","Exit completely"),delegate { Close(); })),50);
   hotkeys=Theme.Label(T("Ctrl+Alt+H 숨기기/표시 · Ctrl+Alt+Q 종료","Ctrl+Alt+H hide/show · Ctrl+Alt+Q exit"),9); hotkeys.ForeColor=Theme.Muted; Theme.Row(root,hotkeys,28);
   Theme.Row(root,Theme.Label(T("제작자 Miareve · 베타 1.1 · 무료 사용 / 유료 재배포 금지","Created by Miareve · Beta 1.1 · Free use / no paid redistribution"),9),30);
   if(tray.ContextMenuStrip!=null) tray.ContextMenuStrip.Dispose();
   var menu=new ContextMenuStrip(); menu.Items.Add(T("열기","Open"),null,delegate { ShowMain(); }); menu.Items.Add(T("설정","Settings"),null,delegate { ShowMain(); OpenSettings(); });
   menu.Items.Add(T("10분 쉬기","Rest 10 minutes"),null,delegate { Rest(); }); menu.Items.Add(T("화면 허용 해제","Revoke screen access"),null,delegate { consent.Checked=false; }); menu.Items.Add(T("완전 종료","Exit completely"),null,delegate { Close(); }); tray.ContextMenuStrip=menu;
   UpdateState();
  }
  protected override void OnHandleCreated(EventArgs e) {
   base.OnHandleCreated(e); bool a=Native.RegisterHotKey(Handle,1,0x4003,(uint)Keys.H),b=Native.RegisterHotKey(Handle,2,0x4003,(uint)Keys.Q);
   if((!a || !b) && hotkeys!=null) hotkeys.Text=T("단축키 충돌: 버튼 또는 트레이 메뉴를 이용하세요.","Hotkey conflict: use buttons or the tray menu.");
  }
  protected override void OnHandleDestroyed(EventArgs e) { Native.UnregisterHotKey(Handle,1); Native.UnregisterHotKey(Handle,2); base.OnHandleDestroyed(e); }
  protected override void WndProc(ref Message m) { if(m.Msg==0x312) { if(m.WParam.ToInt32()==1) { if(Visible) Hide(); else ShowMain(); } if(m.WParam.ToInt32()==2) Close(); } base.WndProc(ref m); }
  void ShowMain() { Show(); WindowState=FormWindowState.Normal; Activate(); }
  void EnablePlugin(bool enable) { pluginType.GetMethod("SetEnabled").Invoke(plugin,new object[]{enable}); }
  void ResetInference() {
   generation++; if(aiJob!=null) aiJob.Cancel(); previousImage=null; context=""; lastTitle=""; nextAi=0; decision=Decision.Unknown(""); decisionAt=0;
  }
  void Suspend() { ResetInference(); clock.ResetAway(); EnablePlugin(false); CloseReminder(); }
  void Stop() { running=false; breakUntil=0; Suspend(); status.Text=T("일시 정지 · 화면 접근 중지","Paused · screen access stopped"); reason.Text=T("공부 시작을 누르면 다시 확인합니다.","Press Start to resume."); UpdateState(); }
  void Toggle() {
   if(running) { Stop(); return; } if(!consent.Checked) return;
   running=true; breakUntil=0; ResetInference(); lastTick=watch.Elapsed.TotalSeconds; nextSample=0; EnablePlugin(true);
   status.Text=T("공부 상태 확인 중","Checking study activity"); reason.Text=T("공부할 앱이나 강의로 이동하세요.","Switch to your creative app or lesson."); UpdateState();
  }
  void Rest() { if(!running) { CloseReminder(); return; } breakUntil=watch.Elapsed.TotalSeconds+600; Suspend(); status.Text=T("10분 휴식 · 화면 접근 중지","10 minute break · screen access stopped"); UpdateState(); }
  void CloseReminder() { if(reminder!=null && !reminder.IsDisposed) reminder.Close(); reminder=null; }
  void ShowReminder() {
   if(reminder!=null && !reminder.IsDisposed) return;
   reminder=new Reminder(settings.Language,messageIndex++,delegate { clock.ResetAway(); },delegate { Rest(); }); reminder.Show();
   if(settings.Sound) System.Media.SystemSounds.Exclamation.Play();
  }
  void ClearCache() { ResetInference(); clock.ResetAway(); EnablePlugin(false); cache.Clear(); reason.Text=T("캐시 삭제 완료 · 새로 판단합니다.","Cache cleared · next check starts fresh."); }
  void OpenSettings() {
   if(editing) return; editing=true; Suspend();
   try {
    using(var f=new SettingsForm(settings,cache,ClearCache,storage.Root)) if(f.ShowDialog(this)==DialogResult.OK) {
     try { storage.Save(f.Result); settings=f.Result; ResetInference(); if(!settings.CacheEnabled) cache.Clear(); Build(); }
     catch(Exception ex) { MessageBox.Show(this,ex.Message,T("저장 실패","Save failed")); }
    }
   } finally { editing=false; lastTick=watch.Elapsed.TotalSeconds; nextSample=0; if(running && consent.Checked && !locked && watch.Elapsed.TotalSeconds>=breakUntil) EnablePlugin(true); UpdateState(); }
  }
  void SessionSwitch(object sender,SessionSwitchEventArgs e) {
   if(disposing || !IsHandleCreated) return;
   BeginInvoke((Action)delegate {
    if(e.Reason==SessionSwitchReason.SessionLock || e.Reason==SessionSwitchReason.SessionLogoff || e.Reason==SessionSwitchReason.RemoteDisconnect || e.Reason==SessionSwitchReason.ConsoleDisconnect) { locked=true; Suspend(); }
    else if(e.Reason==SessionSwitchReason.SessionUnlock || e.Reason==SessionSwitchReason.SessionLogon || e.Reason==SessionSwitchReason.RemoteConnect || e.Reason==SessionSwitchReason.ConsoleConnect) { locked=false; ResetInference(); nextSample=0; }
    lastTick=watch.Elapsed.TotalSeconds;
   });
  }
  void PowerChanged(object sender,PowerModeChangedEventArgs e) { if(disposing || !IsHandleCreated) return; BeginInvoke((Action)delegate { Suspend(); lastTick=watch.Elapsed.TotalSeconds; nextSample=0; }); }
  void UpdateState() {
   if(start==null) return; start.Enabled=consent.Checked; start.Text=running?T("일시 정지","Pause"):T("공부 시작","Start");
   string mode=settings.Provider=="Ollama"?T("무료 로컬 AI","Free local AI"):settings.Provider=="Off"?T("단어 규칙","Word rules"):settings.Provider;
   privacy.Text=mode+" · "+settings.Model+"\n"+T("활성 창만 분석 · 화면 저장 안 함 · 알림까지 ","Active window only · no image files · reminder after ")+settings.AlertSeconds+T("초"," sec");
   if(settings.Provider=="OpenAI") privacy.Text+=" · "+settings.Endpoint;
   stats.Text=T("공부 ","Study ")+TimeSpan.FromSeconds(clock.StudySeconds).ToString(@"hh\:mm\:ss")+T("  / 이탈 ","  / Away ")+TimeSpan.FromSeconds(clock.AwaySeconds).ToString(@"hh\:mm\:ss")+"  / "+settings.AlertSeconds+T("초","s");
   tray.Text="Miareve Study Guard · "+(running?T("실행 중","Running"):T("정지","Paused"));
  }
  void Tick() {
   double now=watch.Elapsed.TotalSeconds,elapsed=now-lastTick; lastTick=now;
   if(!running || !consent.Checked || editing) return;
   if(locked) { status.Text=T("화면 잠금 · 분석 중지","Screen locked · analysis paused"); return; }
   if(now<breakUntil) { status.Text=T("휴식 중 · ","Break · ")+TimeSpan.FromSeconds(breakUntil-now).ToString(@"mm\:ss"); return; }
   if(breakUntil>0) { breakUntil=0; Suspend(); nextSample=0; elapsed=0; }
   if(elapsed>10) { Suspend(); nextSample=0; elapsed=0; }
   try {
    IntPtr h=Native.GetForegroundWindow(); uint pid; Native.GetWindowThreadProcessId(h,out pid);
    if(h==IntPtr.Zero || pid==0) throw new IOException("Foreground window unavailable");
    if(pid==(uint)ownPid) { clock.ResetAway(); UpdateState(); return; }
    var title=new StringBuilder(1024); Native.GetWindowText(h,title,title.Capacity); string currentTitle=title.ToString();
    string nextContext=h.ToString()+"|"+currentTitle;
    if(nextContext!=context) { ResetInference(); context=nextContext; lastTitle=currentTitle; nextSample=0; }
    string process; using(var p=Process.GetProcessById((int)pid)) process=p.ProcessName;
    bool browser=process.Equals("chrome",StringComparison.OrdinalIgnoreCase) || process.Equals("msedge",StringComparison.OrdinalIgnoreCase) || process.Equals("firefox",StringComparison.OrdinalIgnoreCase) || process.Equals("brave",StringComparison.OrdinalIgnoreCase) || currentTitle.IndexOf("YouTube",StringComparison.OrdinalIgnoreCase)>=0;
    if(now>=nextSample) {
     nextSample=now+2;
     if(!(bool)pluginType.GetProperty("Enabled").GetValue(plugin,null)) EnablePlugin(true);
     byte[] jpeg=(byte[])pluginType.GetMethod("CaptureFrame").Invoke(plugin,null);
     double change=(double)pluginType.GetProperty("Change").GetValue(plugin,null);
     string signature=(string)pluginType.GetProperty("Signature").GetValue(plugin,null);
     if(browser && settings.Provider!="Off") {
      if(change>0.20) { decision=Decision.Unknown(T("화면 변경 · 다시 확인 중","Scene changed · checking again")); nextAi=0; }
      string key=DecisionCache.Key(settings,currentTitle,signature); Decision cached=settings.CacheEnabled?cache.Get(key):null;
      if(cached!=null) { decision=cached; decisionAt=now; }
      else if(!busy && now>=nextAi && now-lastRequest>=5) {
       string image=Convert.ToBase64String(jpeg); string[] images=previousImage==null?new[]{image}:new[]{previousImage,image};
       previousImage=image; nextAi=now+settings.AiInterval; lastRequest=now;
       var ignored=Analyze(settings.Clone(),currentTitle,images,key,generation,cache.Generation);
      }
      if(now-decisionAt>Math.Max(45,settings.AiInterval*2)) decision=Decision.Unknown(T("AI 응답 대기 / 설정에서 연결 테스트","Waiting for AI / test connection in Settings"));
     } else {
      bool studying=StudyPolicy.IsStudying(process,currentTitle,settings.Keywords,Native.Idle(),change>0.006);
      decision=new Decision { label=studying?"study":"leisure",confidence=1,reason=T("앱·단어·입력 활동 규칙 (추정)","App, word and activity rules (estimate)") }; decisionAt=now;
     }
    }
    if(decision.label=="unknown") clock.ResetAway(); else clock.Tick(decision.label=="study",elapsed);
    status.Text=decision.label=="study"?T("좋아요, 공부 흐름을 이어가요.","Keep your creative flow."):decision.label=="leisure"?T("공부에서 벗어난 것으로 보여요.","Looks like a study break."):T("판단 보류 · 확인 중","Uncertain · checking");
    reason.Text=decision.reason=="cache"?T("저장된 AI 판단 재사용 (추정)","Cached AI decision (estimate)"):String.IsNullOrEmpty(decision.reason)?T("AI가 준비되지 않았다면 설정에서 연결하세요.","Connect your AI in Settings if it is not ready."):decision.reason;
    UpdateState();
    if(clock.AwaySeconds>=settings.AlertSeconds) { clock.ResetAway(); ShowReminder(); }
   } catch { Stop(); status.Text=T("화면 분석 중지","Screen analysis stopped"); reason.Text=T("활성 창을 읽지 못했습니다. 잠금을 해제하고 다시 시작하세요.","Could not read the active window. Unlock and restart monitoring."); }
  }
  async Task Analyze(Settings snapshot,string title,string[] images,string key,int epoch,int cacheGeneration) {
   busy=true; var job=new CancellationTokenSource(TimeSpan.FromSeconds(120)); aiJob=job;
   try {
    var result=await AiClient.Classify(snapshot,title,images,job.Token);
    if(disposing || generation!=epoch || !running || !consent.Checked || editing) return;
    decision=result; decisionAt=watch.Elapsed.TotalSeconds;
    if(snapshot.CacheEnabled) try { cache.Put(key,result,snapshot.CacheMinutes,cacheGeneration); } catch { reason.Text=T("캐시 저장 실패 · AI 판단은 계속 사용합니다.","Cache save failed · AI result still available."); }
   } catch(OperationCanceledException) { }
   catch(Exception ex) { if(!disposing && generation==epoch) { decision=Decision.Unknown(T("AI 연결 확인 필요: ","AI connection needs attention: ")+ex.Message); decisionAt=watch.Elapsed.TotalSeconds; } }
   finally { if(aiJob==job) aiJob=null; job.Dispose(); busy=false; }
  }
  protected override void Dispose(bool disposingNow) {
   if(disposingNow && !disposing) { disposing=true; timer.Stop(); timer.Dispose(); SystemEvents.SessionSwitch-=SessionSwitch; SystemEvents.PowerModeChanged-=PowerChanged; Suspend(); tray.Visible=false; tray.Dispose(); }
   base.Dispose(disposingNow);
  }
 }
 public sealed class Reminder : Form {
  static readonly string[] Korean={"공부 안할거냐?","공부좀 하세요","공부 안함?","디자인하러 가라","꿈을 키워라","키프레임 하나만 더!"};
  static readonly string[] English={"Time to study!","Let's get some studying done.","Ready to get back to work?","Go create something.","Build your dream.","Just one more keyframe!"};
  readonly Rectangle area;
  protected override bool ShowWithoutActivation { get { return true; } }
  public Reminder(string language,int index,Action resume,Action rest) {
   bool en=language=="en"; area=Screen.FromHandle(Native.GetForegroundWindow()).WorkingArea;
   Text="Miareve · "+(en?"Study reminder":"공부 알림"); ClientSize=new Size(720,340); BackColor=Theme.Background; ForeColor=Color.White;
   Font=new Font("맑은 고딕",11); AutoScaleMode=AutoScaleMode.Dpi; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false; TopMost=true; ShowInTaskbar=true; StartPosition=FormStartPosition.Manual;
   var root=Theme.Table(); Controls.Add(root);
   var caption=Theme.Label("M I A R E V E   /   F O C U S",11); caption.ForeColor=Theme.Accent; caption.TextAlign=ContentAlignment.MiddleCenter; Theme.Row(root,caption,38);
   var title=Theme.Label((en?English:Korean)[index%Korean.Length],en?27:36); title.TextAlign=ContentAlignment.MiddleCenter; title.ForeColor=Theme.Accent; Theme.Row(root,title,109);
   var sub=Theme.Label(en?"Make one small step toward your next scene.":"작은 키프레임 하나부터 다시 시작해 보자.",12); sub.TextAlign=ContentAlignment.MiddleCenter; Theme.Row(root,sub,47);
   var buttons=new TableLayoutPanel { ColumnCount=2,Dock=DockStyle.Fill }; buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50)); buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
   var back=Theme.Button(en?"I'm getting back to it":"지금 할게",delegate { resume(); Close(); }); back.Name="Acknowledge"; back.Dock=DockStyle.Fill;
   var pause=Theme.Button(en?"Rest 10 minutes":"10분만 쉴게",delegate { rest(); Close(); }); pause.Name="Rest"; pause.Dock=DockStyle.Fill;
   buttons.Controls.Add(back,0,0); buttons.Controls.Add(pause,1,0); Theme.Row(root,buttons,57);
   var foot=Theme.Label(en?"This reminder stays until you dismiss it.":"확인할 때까지 화면에 표시됩니다.",9); foot.TextAlign=ContentAlignment.MiddleCenter; Theme.Row(root,foot,31);
   FormClosed+=delegate { resume(); };
  }
  protected override void OnShown(EventArgs e) { base.OnShown(e); Size=new Size(Math.Min(Width,area.Width-24),Math.Min(Height,area.Height-24)); Location=new Point(area.Left+(area.Width-Width)/2,area.Top+(area.Height-Height)/2); }
 }
 public static class Preview {
  public static void Save(Form f,string path) { f.Show(); Application.DoEvents(); using(var b=new Bitmap(f.Width,f.Height)) { f.DrawToBitmap(b,new Rectangle(0,0,f.Width,f.Height)); b.Save(path); } }
  public static void Run(string root) {
   Directory.CreateDirectory(root); var storage=new Storage(Path.Combine(root,"preview-data"));
   using(var f=new MainForm(storage)) { Save(f,Path.Combine(root,"main.png")); f.Close(); }
   using(var f=new SettingsForm(new Settings(),new DecisionCache(storage.Root),delegate{},storage.Root)) { Save(f,Path.Combine(root,"settings.png")); f.Close(); }
   using(var f=new Reminder("ko",0,delegate{},delegate{})) { Save(f,Path.Combine(root,"reminder.png")); f.Close(); }
  }
 }
}
