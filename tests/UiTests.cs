using System;
using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using Miareve;
class UiTests {
    static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Field(object f,string n) { return f.GetType().GetField(n,flags).GetValue(f); }
    static void Call(object f,string n) { f.GetType().GetMethod(n,flags).Invoke(f,null); }
    static void Check(bool ok,string name) { if(!ok) throw new Exception(name); Console.WriteLine("PASS "+name); }
    [STAThread] static int Main(string[] args) {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        using(var f=new MainForm()) {
            ((Timer)Field(f,"timer")).Stop();
            Check(Field(f,"plugin")!=null,"Embedded screen plugin loads");
            var consent=(CheckBox)Field(f,"consent"); var start=(Button)Field(f,"start"); var plugin=Field(f,"plugin");
            Check(!consent.Checked && !start.Enabled,"Launch requires explicit opt-in");
            consent.Checked=true; Check(start.Enabled,"Opt-in unlocks start"); Call(f,"Toggle");
            Check((bool)plugin.GetType().GetProperty("Enabled").GetValue(plugin,null),"Start enables capture module");
            Call(f,"TakeBreak"); Check(!(bool)plugin.GetType().GetProperty("Enabled").GetValue(plugin,null),"Rest disables capture immediately");
            consent.Checked=false; Check(!(bool)Field(f,"running") && !start.Enabled,"Revocation stops monitoring");
            Check(!(bool)plugin.GetType().GetProperty("Enabled").GetValue(plugin,null),"Revocation clears module");
            f.Show(); Application.DoEvents();
            using(var b=new Bitmap(f.Width,f.Height)) { f.DrawToBitmap(b,new Rectangle(0,0,f.Width,f.Height)); b.Save(args[0]); }
            f.Close();
        }
        Type t=typeof(MainForm).Assembly.GetType("Miareve.Reminder");
        using(var reminder=(Form)Activator.CreateInstance(t,new object[]{(Action)delegate{},(Action)delegate{}})) {
            reminder.Show(); Application.DoEvents(); Check(reminder.TopMost,"Reminder appears above other windows");
            using(var b=new Bitmap(reminder.Width,reminder.Height)) { reminder.DrawToBitmap(b,new Rectangle(0,0,reminder.Width,reminder.Height)); b.Save(args[1]); }
            reminder.Close();
        }
        Console.WriteLine("8 UI integration checks passed; screen sampling was disabled."); return 0;
    }
}
