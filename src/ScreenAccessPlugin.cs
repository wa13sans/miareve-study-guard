using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Security.Cryptography;
[assembly: AssemblyTitle("Miareve Screen Access Plugin")]
[assembly: AssemblyCompany("Miareve")]
[assembly: AssemblyVersion("1.1.0.0")]
namespace Miareve.Plugins {
 public sealed class ScreenAccessPlugin {
  [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr handle,out Rect rect);
  [StructLayout(LayoutKind.Sequential)] struct Rect { public int Left,Top,Right,Bottom; }
  int[] previous; Rectangle previousBounds;
  public bool Enabled { get; private set; }
  public double Change { get; private set; }
  public string Signature { get; private set; }
  public void SetEnabled(bool enabled) { Enabled=enabled; previous=null; Signature=""; Change=0; }
  public double Sample() { CaptureFrame(); return Change; }
  public byte[] CaptureFrame() {
   if(!Enabled) throw new InvalidOperationException("Screen access requires consent.");
   Rect rect; IntPtr foreground=GetForegroundWindow();
   if(foreground==IntPtr.Zero || !GetWindowRect(foreground,out rect)) throw new IOException("No foreground window");
   Rectangle bounds=Rectangle.Intersect(Rectangle.FromLTRB(rect.Left,rect.Top,rect.Right,rect.Bottom),SystemInformation.VirtualScreen);
   if(bounds.Width<2 || bounds.Height<2) throw new IOException("Window is not visible");
   int[] current=new int[64*36]; byte[] fingerprint=new byte[current.Length*3]; byte[] jpeg;
   using(var full=new Bitmap(bounds.Width,bounds.Height)) {
    using(var g=Graphics.FromImage(full)) g.CopyFromScreen(bounds.Location,Point.Empty,bounds.Size);
    using(var small=new Bitmap(64,36)) {
     using(var g=Graphics.FromImage(small)) { g.InterpolationMode=InterpolationMode.Bilinear; g.DrawImage(full,new Rectangle(0,0,64,36)); }
     for(int y=0;y<36;y++) for(int x=0;x<64;x++) {
      int i=y*64+x; Color c=small.GetPixel(x,y); current[i]=c.ToArgb();
      fingerprint[i*3]=(byte)(c.R/16); fingerprint[i*3+1]=(byte)(c.G/16); fingerprint[i*3+2]=(byte)(c.B/16);
     }
    }
    double scale=Math.Min(1,960.0/Math.Max(full.Width,full.Height));
    using(var resized=new Bitmap(Math.Max(1,(int)(full.Width*scale)),Math.Max(1,(int)(full.Height*scale)))) {
     using(var g=Graphics.FromImage(resized)) { g.InterpolationMode=InterpolationMode.HighQualityBilinear; g.DrawImage(full,new Rectangle(0,0,resized.Width,resized.Height)); }
     using(var ms=new MemoryStream()) { resized.Save(ms,System.Drawing.Imaging.ImageFormat.Jpeg); jpeg=ms.ToArray(); }
    }
   }
   Change=0;
   if(previous!=null && previousBounds==bounds) {
    for(int i=0;i<current.Length;i++) { Color a=Color.FromArgb(current[i]),b=Color.FromArgb(previous[i]); Change+=(Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B))/3.0; }
    Change/=current.Length*255.0;
   }
   previous=current; previousBounds=bounds;
   using(var sha=SHA256.Create()) Signature=BitConverter.ToString(sha.ComputeHash(fingerprint)).Replace("-","");
   return jpeg;
  }
 }
}
