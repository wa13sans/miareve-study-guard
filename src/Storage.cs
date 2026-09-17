using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Collections.Generic;

namespace Miareve {
    public static class Json {
        public static string Write(object value) { return new JavaScriptSerializer { MaxJsonLength=16000000 }.Serialize(value); }
        public static T Read<T>(string value) { return new JavaScriptSerializer { MaxJsonLength=16000000 }.Deserialize<T>(value); }
    }
    public sealed class Settings {
        public string Language { get; set; }
        public int AlertSeconds { get; set; }
        public int AiInterval { get; set; }
        public string Keywords { get; set; }
        public string Provider { get; set; }
        public string Endpoint { get; set; }
        public string Model { get; set; }
        public string ProtectedApiKey { get; set; }
        public bool AllowRemote { get; set; }
        public string CliPath { get; set; }
        public string CliArguments { get; set; }
        public bool AllowCli { get; set; }
        public bool Sound { get; set; }
        public bool CacheEnabled { get; set; }
        public int CacheMinutes { get; set; }
        public Settings() {
            Language="ko"; AlertSeconds=300; AiInterval=20; Keywords=StudyPolicy.DefaultKeywords;
            Provider="Ollama"; Endpoint="http://127.0.0.1:11434"; Model="qwen3-vl:2b-instruct";
            ProtectedApiKey=""; CliPath=""; CliArguments=""; CacheEnabled=true; CacheMinutes=30;
        }
        public Settings Clone() { return Json.Read<Settings>(Json.Write(this)); }
        public string T(string ko,string en) { return Language=="en"?en:ko; }
        public void Validate() {
            if (Language!="ko" && Language!="en") throw new ArgumentException("Language: ko / en");
            if (AlertSeconds<1 || AlertSeconds>86400) throw new ArgumentException("Alert: 1–86400 seconds");
            if (AiInterval<5 || AiInterval>600) throw new ArgumentException("AI interval: 5–600 seconds");
            if (CacheMinutes<1 || CacheMinutes>1440) throw new ArgumentException("Cache: 1–1440 minutes");
            if (Keywords==null || Keywords.Length>32000) throw new ArgumentException("Keywords: max 32000 characters");
            if (Provider!="Off" && Provider!="Ollama" && Provider!="OpenAI" && Provider!="CLI") throw new ArgumentException("Unknown provider");
            if (Provider=="Ollama" || Provider=="OpenAI") AiClient.ValidateEndpoint(this);
            if (Provider!="Off" && String.IsNullOrWhiteSpace(Model)) throw new ArgumentException("Model required");
        }
        public string ApiKey() {
            if (String.IsNullOrEmpty(ProtectedApiKey)) return "";
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(ProtectedApiKey),null,DataProtectionScope.CurrentUser));
        }
        public void SetApiKey(string key) { ProtectedApiKey=String.IsNullOrEmpty(key)?"":Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(key),null,DataProtectionScope.CurrentUser)); }
    }
    public sealed class Storage {
        public readonly string Root;
        public string Warning { get; private set; }
        public Storage(string root) { Root=Path.GetFullPath(root); }
        public Settings Load() {
            string file=Path.Combine(Root,"settings.json");
            if (!File.Exists(file)) return new Settings();
            try { var s=Json.Read<Settings>(File.ReadAllText(file)); s.Validate(); return s; }
            catch { Warning="설정 파일을 읽지 못해 기본값을 사용합니다. / Settings could not be loaded; defaults restored."; return new Settings(); }
        }
        public void Save(Settings value) { value.Validate(); AtomicWrite(Path.Combine(Root,"settings.json"),Json.Write(value)); }
        public static void AtomicWrite(string path,string value) {
            Directory.CreateDirectory(Path.GetDirectoryName(path)); string tmp=path+".tmp";
            File.WriteAllText(tmp,value,new UTF8Encoding(false));
            if(File.Exists(path)) File.Replace(tmp,path,null); else File.Move(tmp,path);
        }
    }
    public sealed class Decision {
        public string label { get; set; }
        public double confidence { get; set; }
        public string reason { get; set; }
        public static Decision Unknown(string why) { return new Decision { label="unknown",confidence=0,reason=why }; }
    }
    public sealed class CacheEntry {
        public string Key { get; set; }
        public string Label { get; set; }
        public double Confidence { get; set; }
        public DateTime Expires { get; set; }
    }
    public sealed class DecisionCache {
        readonly string path; readonly Dictionary<string,CacheEntry> entries=new Dictionary<string,CacheEntry>();
        public int Generation { get; private set; }
        public int Count { get { Prune(); return entries.Count; } }
        public long Bytes { get { return File.Exists(path)?new FileInfo(path).Length:0; } }
        public DecisionCache(string root) {
            path=Path.Combine(root,"cache","decisions.json");
            try { if(File.Exists(path) && new FileInfo(path).Length<2000000) foreach(var e in Json.Read<List<CacheEntry>>(File.ReadAllText(path))) if(e.Key!=null && (e.Label=="study" || e.Label=="leisure")) entries[e.Key]=e; } catch { entries.Clear(); }
            Prune();
        }
        void Prune() { foreach(var k in entries.Where(p=>p.Value.Expires<=DateTime.UtcNow).Select(p=>p.Key).ToList()) entries.Remove(k); }
        public Decision Get(string key) {
            Prune(); CacheEntry e; return entries.TryGetValue(key,out e)?new Decision { label=e.Label,confidence=e.Confidence,reason="cache" }:null;
        }
        public void Put(string key,Decision d,int minutes,int generation) {
            if(generation!=Generation || d.label=="unknown") return;
            Prune(); if(entries.Count>=500) entries.Remove(entries.OrderBy(p=>p.Value.Expires).First().Key);
            entries[key]=new CacheEntry { Key=key,Label=d.label,Confidence=d.confidence,Expires=DateTime.UtcNow.AddMinutes(minutes) };
            Storage.AtomicWrite(path,Json.Write(entries.Values.ToArray()));
        }
        public void Clear() {
            Generation++; entries.Clear();
            if(File.Exists(path)) File.Delete(path);
            if(File.Exists(path+".tmp")) File.Delete(path+".tmp");
        }
        public static string Key(Settings s,string title,string signature) {
            // Only a one-way fingerprint is persisted; never the title, image, or AI reasoning.
            string raw=Json.Write(new { version="study-v2",s.Provider,s.Endpoint,s.Model,s.Keywords,s.CliPath,s.CliArguments,title,signature });
            using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw))).Replace("-","");
        }
    }
}
