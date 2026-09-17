using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;

namespace Miareve {
    public static class Theme {
        public static readonly Color Background=Color.FromArgb(17,20,30),Card=Color.FromArgb(28,33,47),Accent=Color.FromArgb(189,170,255),Muted=Color.FromArgb(165,178,200);
        public static Label Label(string text,int size=10) { return new Label { Text=text,ForeColor=Color.White,Font=new Font("맑은 고딕",size),AutoSize=false,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Margin=new Padding(4) }; }
        public static Button Button(string text,EventHandler click=null) { var b=new Button { Text=text,AutoSize=false,Size=new Size(170,40),BackColor=Card,ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Margin=new Padding(4),Cursor=Cursors.Hand }; if(click!=null) b.Click+=click; return b; }
        public static TableLayoutPanel Table() { var t=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=1,AutoScroll=true,Padding=new Padding(18),BackColor=Background }; t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); return t; }
        public static void Row(TableLayoutPanel t,Control c,int height) { int r=t.RowCount++; t.RowStyles.Add(new RowStyle(SizeType.Absolute,height)); c.Dock=DockStyle.Fill; t.Controls.Add(c,0,r); }
        public static TextBox Text(string text,bool multi=false) { return new TextBox { Text=multi?text.Replace("\r\n","\n").Replace("\r","\n").Replace("\n",Environment.NewLine):text,Multiline=multi,AcceptsReturn=multi,ScrollBars=multi?ScrollBars.Vertical:ScrollBars.None,BackColor=Card,ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle,Dock=DockStyle.Fill,MaxLength=32000 }; }
        public static FlowLayoutPanel Flow(params Control[] controls) { var f=new FlowLayoutPanel { Dock=DockStyle.Fill,WrapContents=true }; f.Controls.AddRange(controls); return f; }
    }
    public sealed class SettingsForm : Form {
        readonly Settings original; readonly DecisionCache cache; readonly Action clear; readonly string root;
        CancellationTokenSource job;
        ComboBox language,provider; NumericUpDown mins,secs,interval,ttl;
        TextBox keywords,endpoint,model,apiKey,cliPath,cliArgs; CheckBox remote,cliConsent,sound,useCache;
        Label cacheInfo,jobStatus; bool keyReadable=true; Button test,pull;
        public Settings Result { get; private set; }
        string T(string ko,string en) { return original.T(ko,en); }
        public SettingsForm(Settings settings,DecisionCache decisionCache,Action clearCache,string dataRoot) {
            original=settings.Clone(); cache=decisionCache; clear=clearCache; root=dataRoot;
            Text=T("설정 · Miareve Study Guard","Settings · Miareve Study Guard"); ClientSize=new Size(850,700); MinimumSize=new Size(760,650); StartPosition=FormStartPosition.CenterParent; BackColor=Theme.Background; ForeColor=Color.White; Font=new Font("맑은 고딕",10); AutoScaleMode=AutoScaleMode.Dpi;
            var shell=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=1,RowCount=2 }; shell.RowStyles.Add(new RowStyle(SizeType.Percent,100)); shell.RowStyles.Add(new RowStyle(SizeType.Absolute,62)); Controls.Add(shell);
            var tabs=new TabControl { Dock=DockStyle.Fill }; shell.Controls.Add(tabs,0,0);
            var general=Page(tabs,T("일반","General")); var words=Page(tabs,T("공부 단어","Study words")); var ai=Page(tabs,"AI / API / CLI"); var cachePage=Page(tabs,T("캐시 · 개인정보","Cache · Privacy"));
            language=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList,Width=200 }; language.Items.AddRange(new object[]{"한국어","English"}); language.SelectedIndex=settings.Language=="en"?1:0;
            Theme.Row(general,Theme.Label("언어 / Language"),32); Theme.Row(general,language,36);
            Theme.Row(general,Theme.Label(T("알림까지 기다릴 시간 (최소 1초)","Time away before reminder (minimum 1 second)")),36);
            mins=Number(0,1440,settings.AlertSeconds/60); secs=Number(0,59,settings.AlertSeconds%60);
            Theme.Row(general,Theme.Flow(mins,Inline(T("분","min")),secs,Inline(T("초","sec"))),46);
            sound=new CheckBox { Text=T("알림 소리도 함께 재생 (화면 알림은 항상 표시)","Also play sound (visual reminders always appear)"),Checked=settings.Sound,AutoSize=true };
            Theme.Row(general,sound,44);
            Theme.Row(general,Theme.Label(T("큰 알림은 사용 중인 모니터 정중앙에 표시되며, 확인할 때까지 유지됩니다.\n화면 잠금·일시 정지·종료 시에는 닫힙니다.","Large reminders stay centered on your active monitor until dismissed.\nThey close when you lock, pause, or exit.")),74);
            Theme.Row(general,Theme.Label(T("단축키\nCtrl + Alt + H : 숨기기 / 다시 표시\nCtrl + Alt + Q : 완전 종료\n트레이 메뉴에서도 열기·휴식·허용 해제·종료가 가능합니다.","Hotkeys\nCtrl + Alt + H : Hide / show\nCtrl + Alt + Q : Fully exit\nTray menu: Open, rest, revoke screen access, exit.")),125);
            Theme.Row(words,Theme.Label(T("공부로 볼 단어 · 줄바꿈 또는 쉼표로 구분\n저장을 누르면 다음 실행에도 유지됩니다. AI에는 참고 단어로 전달됩니다.","Study words · separate with new lines or commas\nSave keeps words across restarts. AI uses them as context hints.")),62);
            keywords=Theme.Text(settings.Keywords,true); keywords.WordWrap=true; Theme.Row(words,keywords,370);
            Theme.Row(words,Theme.Flow(Theme.Button(T("기본 단어 추가","Append defaults"),delegate { keywords.Text+="\r\n"+StudyPolicy.DefaultKeywords; })),50);
            Theme.Row(ai,Theme.Label(T("무료 로컬 AI: Ollama + Qwen3-VL (API 사용료 없음)","Free local AI: Ollama + Qwen3-VL (no API usage fees)")),34);
            provider=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList }; provider.Items.AddRange(new object[]{"Off","Ollama","OpenAI","CLI"}); provider.SelectedItem=settings.Provider;
            Theme.Row(ai,provider,32);
            Theme.Row(ai,Theme.Label(T("서버 주소 · Ollama: http://127.0.0.1:11434 / API: .../v1","Server URL · Ollama: http://127.0.0.1:11434 / API: .../v1")),30);
            endpoint=Theme.Text(settings.Endpoint); Theme.Row(ai,endpoint,32);
            Theme.Row(ai,Theme.Label(T("이미지 입력을 지원하는 모델 이름","Model name (must support images)")),28); model=Theme.Text(settings.Model); Theme.Row(ai,model,32);
            Theme.Row(ai,Theme.Label(T("API 키 (로컬 Ollama는 불필요, Windows 계정으로 암호화 저장)","API key (not needed for local Ollama; encrypted for your Windows account)")),30);
            apiKey=Theme.Text(""); apiKey.UseSystemPasswordChar=true; try { apiKey.Text=settings.ApiKey(); } catch { keyReadable=false; } Theme.Row(ai,apiKey,32);
            remote=new CheckBox { AutoSize=true,Checked=settings.AllowRemote,Text=T("외부 API로 활성 창 이미지·창 제목·공부 단어 전송 허용 (요금은 제공자 기준)","Allow sending window images, title and study words to remote API (provider may charge)") }; Theme.Row(ai,remote,48);
            interval=Number(5,600,settings.AiInterval); Theme.Row(ai,Theme.Flow(Inline(T("AI 확인 주기(초)","AI interval (seconds)"),200),interval),42);
            Theme.Row(ai,Theme.Label(T("CLI 연결 · 신뢰하는 JSON 브리지 EXE 경로 (셸 명령 입력란 아님)","CLI connection · trusted JSON bridge EXE path (not a shell command)")),30);
            cliPath=Theme.Text(settings.CliPath); Theme.Row(ai,cliPath,32); cliArgs=Theme.Text(settings.CliArguments);
            Theme.Row(ai,Theme.Label(T("고정 CLI 인수 (화면의 텍스트는 인수로 실행되지 않습니다)","Fixed CLI arguments (screen text is never executed as arguments)")),28); Theme.Row(ai,cliArgs,32);
            cliConsent=new CheckBox { AutoSize=true,Checked=settings.AllowCli,Text=T("선택한 CLI 실행 및 이미지·제목 전달 허용 (외부 전송 여부는 CLI 설정 확인)","Allow this CLI to run and receive images/title (check its own network settings)") }; Theme.Row(ai,cliConsent,44);
            test=Theme.Button(T("연결 테스트","Test connection"),async delegate { await Test(); });
            pull=Theme.Button(T("추천 모델 받기","Get default model"),async delegate { await Pull(); });
            Theme.Row(ai,Theme.Flow(test,pull,Theme.Button(T("Ollama 공식 설치","Official Ollama"),delegate { Process.Start("https://ollama.com/download/windows"); })),52);
            jobStatus=Theme.Label(T("테스트는 합성 이미지로 실행합니다. 화면을 촬영하지 않습니다.","Connection test uses a synthetic image, never your screen.")); Theme.Row(ai,jobStatus,78);
            Theme.Row(ai,Theme.Label(T("모델 약 1.9GB. 설치 후 Ollama를 실행하고 ‘추천 모델 받기’를 누르세요.\n기본은 로컬 주소만 허용합니다. CLI는 동봉된 CLI-BRIDGE.md를 참고하세요.","Model ~1.9GB. Install and run Ollama, then get the default model.\nLocal mode allows loopback only. See CLI-BRIDGE.md for CLI integration.")),78);
            useCache=new CheckBox { AutoSize=true,Checked=settings.CacheEnabled,Text=T("AI 판단 캐시 사용","Use AI decision cache") }; Theme.Row(cachePage,useCache,42);
            ttl=Number(1,1440,settings.CacheMinutes); Theme.Row(cachePage,Theme.Flow(Inline(T("유효 기간(분)","Lifetime (minutes)"),200),ttl),45);
            cacheInfo=Theme.Label(""); Theme.Row(cachePage,cacheInfo,62); RefreshCache();
            Theme.Row(cachePage,Theme.Flow(Theme.Button(T("캐시 전체 삭제","Clear all cache"),delegate { try { clear(); RefreshCache(); } catch(Exception ex) { MessageBox.Show(ex.Message); } })),52);
            Theme.Row(cachePage,Theme.Label(T("캐시는 최대 500건이며 오래된 항목은 자동 만료됩니다.\n화면·창 제목 원문·AI 설명·API 키는 캐시에 저장하지 않습니다.\n캐시를 삭제해도 공부 단어와 설정은 유지됩니다.\nOllama 모델 파일은 캐시 삭제 대상이 아닙니다.","Cache is capped at 500 entries with automatic expiry.\nNo screenshots, raw titles, AI reasoning or API keys are stored in cache.\nClearing cache preserves study words and settings.\nOllama model files are not deleted.")),140);
            Theme.Row(cachePage,Theme.Label(T("데이터 저장 위치 / Data folder\n","Data folder\n")+root),85);
            Theme.Row(cachePage,Theme.Label(T("화면 허용은 매 실행마다 다시 선택합니다.\n정지·휴식·설정창에서는 화면 분석과 AI 요청을 중단합니다.\n외부 API/CLI는 사용자가 명시적으로 켠 경우에만 사용합니다.","Screen permission is requested on every launch.\nPause, rest and Settings suspend screen capture and AI requests.\nRemote API/CLI require your explicit opt-in.")),120);
            var save=Theme.Button(T("저장 · 적용","Save · Apply"),delegate { try { Result=Read(); Result.Validate(); DialogResult=DialogResult.OK; Close(); } catch(Exception ex) { MessageBox.Show(this,ex.Message,T("설정 확인","Check settings")); } });
            var cancel=Theme.Button(T("취소","Cancel"),delegate { Close(); }); shell.Controls.Add(Theme.Flow(save,cancel),0,1);
            FormClosed+=delegate { if(job!=null) job.Cancel(); };
        }
        static Label Inline(string text,int width=65) { var l=Theme.Label(text); l.Dock=DockStyle.None; l.Size=new Size(width,34); return l; }
        static NumericUpDown Number(int min,int max,int value) { return new NumericUpDown { Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value)),Width=90,BackColor=Theme.Card,ForeColor=Color.White }; }
        TableLayoutPanel Page(TabControl tabs,string title) { var page=new TabPage(title) { BackColor=Theme.Background,ForeColor=Color.White }; var t=Theme.Table(); page.Controls.Add(t); tabs.TabPages.Add(page); return t; }
        Settings Read() {
            var s=original.Clone(); s.Language=language.SelectedIndex==1?"en":"ko"; s.AlertSeconds=(int)mins.Value*60+(int)secs.Value;
            s.Keywords=keywords.Text; s.Provider=(string)provider.SelectedItem; s.Endpoint=endpoint.Text.Trim(); s.Model=model.Text.Trim(); s.AllowRemote=remote.Checked;
            if(keyReadable || apiKey.Text.Length>0) s.SetApiKey(apiKey.Text);
            s.CliPath=cliPath.Text.Trim(); s.CliArguments=cliArgs.Text; s.AllowCli=cliConsent.Checked; s.Sound=sound.Checked; s.AiInterval=(int)interval.Value; s.CacheEnabled=useCache.Checked; s.CacheMinutes=(int)ttl.Value; return s;
        }
        void RefreshCache() { cacheInfo.Text=T("저장된 판단: ","Cached decisions: ")+cache.Count+"  /  "+cache.Bytes+" bytes"; }
        async Task Test() {
            if(job!=null) return; job=new CancellationTokenSource(TimeSpan.FromSeconds(120)); test.Enabled=false; pull.Enabled=false; ((ScrollableControl)jobStatus.Parent).ScrollControlIntoView(jobStatus);
            try { var s=Read(); s.Validate(); jobStatus.Text=T("연결 테스트 중…","Testing connection…"); jobStatus.Text=await AiClient.Test(s,job.Token); }
            catch(Exception ex) { if(!IsDisposed) jobStatus.Text=T("연결 실패: ","Connection failed: ")+ex.Message; }
            finally { job.Dispose(); job=null; if(!IsDisposed) { test.Enabled=true; pull.Enabled=true; } }
        }
        async Task Pull() {
            if(job!=null) return;
            var s=Read(); s.Provider="Ollama"; s.Model="qwen3-vl:2b-instruct";
            try { AiClient.ValidateEndpoint(s); } catch(Exception ex) { MessageBox.Show(ex.Message); return; }
            if(MessageBox.Show(this,T("공식 Ollama 모델 약 1.9GB를 내려받습니다. 계속할까요?","Download the official Ollama model (~1.9GB)?"),"Qwen3-VL",MessageBoxButtons.OKCancel)!=DialogResult.OK) return;
            job=new CancellationTokenSource(); test.Enabled=false; pull.Enabled=false; ((ScrollableControl)jobStatus.Parent).ScrollControlIntoView(jobStatus);
            try {
                using(var handler=new HttpClientHandler { AllowAutoRedirect=false,UseProxy=false }) using(var client=new HttpClient(handler)) {
                    client.Timeout=System.Threading.Timeout.InfiniteTimeSpan;
                    var body=new StringContent(Json.Write(new { name="qwen3-vl:2b-instruct",stream=true }),System.Text.Encoding.UTF8,"application/json");
                    using(var request=new HttpRequestMessage(HttpMethod.Post,s.Endpoint.TrimEnd('/')+"/api/pull") { Content=body })
                    using(var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,job.Token)) {
                        response.EnsureSuccessStatusCode();
                        using(var stream=await response.Content.ReadAsStreamAsync()) using(var reader=new StreamReader(stream))
                        using(job.Token.Register(delegate { stream.Dispose(); })) {
                            string line; bool success=false;
                            while((line=await reader.ReadLineAsync())!=null) { var obj=Json.Read<Dictionary<string,object>>(line); if(obj.ContainsKey("error")) throw new IOException((string)obj["error"]); string state=obj.ContainsKey("status")?(string)obj["status"]:""; if(state=="success") success=true;
                                if(!IsDisposed) jobStatus.Text=state+(obj.ContainsKey("completed") && obj.ContainsKey("total")?" · "+Math.Round(Convert.ToDouble(obj["completed"])/Math.Max(1,Convert.ToDouble(obj["total"]))*100)+"%":"");
                            }
                            if(!success) throw new IOException("Model download incomplete");
                        }
                    }
                }
                if(!IsDisposed) { provider.SelectedItem="Ollama"; model.Text=s.Model; jobStatus.Text=T("모델 준비 완료. 연결 테스트 후 저장하세요.","Model ready. Test connection and save."); }
            } catch(Exception ex) { if(!IsDisposed) jobStatus.Text=T("모델 설치 중단: ","Model setup stopped: ")+ex.Message; }
            finally { job.Dispose(); job=null; if(!IsDisposed) { test.Enabled=true; pull.Enabled=true; } }
        }
    }
}
