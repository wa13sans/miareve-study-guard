using System;
using System.Reflection;
using Miareve;
class Tests {
    static int count;
    static void Check(bool condition,string name) { if (!condition) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
    static int Main(string[] args) {
        string k=StudyPolicy.DefaultKeywords;
        Check(StudyPolicy.IsStudying("AfterFX","",k,0,false),"After Effects input");
        Check(StudyPolicy.IsStudying("Illustrator","",k,0,false),"Illustrator input");
        Check(StudyPolicy.IsStudying("blender","",k,0,false),"Blender input");
        Check(StudyPolicy.IsStudying("Unity","",k,0,false),"Unity input");
        Check(StudyPolicy.IsStudying("chrome","After Effects tutorial",k,900,true),"Moving lesson without input");
        Check(!StudyPolicy.IsStudying("chrome","Gaming highlights",k,0,true),"Unrelated moving video");
        Check(!StudyPolicy.IsStudying("AfterFX","",k,500,false),"Idle editor");
        Check(!StudyPolicy.RelatedTitle("anything",", ,"),"Empty keywords cannot match everything");
        Check(StudyPolicy.RelatedTitle("특별한 수업","특별한"),"Custom keyword");
        FocusClock c=new FocusClock(); for(int i=0;i<60;i++) c.Tick(false,5);
        Check(c.AwaySeconds==300,"Continuous five minute threshold"); c.Tick(true,5);
        Check(c.AwaySeconds==0 && c.StudySeconds==5,"Study resets absence"); c.Tick(false,5); c.Tick(false,600);
        Check(c.AwaySeconds==0,"Sleep gap ignored");
        var assembly=Assembly.LoadFrom(args[0]); var t=assembly.GetType("Miareve.Plugins.ScreenAccessPlugin"); var p=Activator.CreateInstance(t);
        Check(!(bool)t.GetProperty("Enabled").GetValue(p,null),"Plugin off by default");
        bool denied=false; try { t.GetMethod("Sample").Invoke(p,null); } catch(TargetInvocationException ex) { denied=ex.InnerException is InvalidOperationException; }
        Check(denied,"Capture denied before consent"); t.GetMethod("SetEnabled").Invoke(p,new object[]{true}); t.GetMethod("SetEnabled").Invoke(p,new object[]{false});
        Check(!(bool)t.GetProperty("Enabled").GetValue(p,null),"Permission revocation");
        Console.WriteLine(count+" tests passed; no real screen was captured."); return 0;
    }
}
