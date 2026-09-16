using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
[assembly: AssemblyTitle("Miareve Screen Access Plugin")]
[assembly: AssemblyCompany("Miareve")]
[assembly: AssemblyVersion("1.0.0.0")]
namespace Miareve.Plugins {
    // This module never writes images or connects to the network.
    public sealed class ScreenAccessPlugin {
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        private int[] previous;
        private string previousDisplay;
        public bool Enabled { get; private set; }
        public void SetEnabled(bool enabled) { Enabled = enabled; previous = null; previousDisplay = null; }
        public double Sample() {
            if (!Enabled) throw new InvalidOperationException("Screen access requires consent.");
            Screen monitor = Screen.FromHandle(GetForegroundWindow());
            Rectangle bounds = monitor.Bounds;
            int[] current = new int[64 * 36];
            using (Bitmap full = new Bitmap(bounds.Width, bounds.Height)) {
                using (Graphics g = Graphics.FromImage(full)) g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                using (Bitmap small = new Bitmap(64, 36)) {
                    using (Graphics g = Graphics.FromImage(small)) {
                        g.InterpolationMode = InterpolationMode.Bilinear;
                        g.DrawImage(full, new Rectangle(0, 0, 64, 36));
                    }
                    for (int y = 0; y < 36; y++) for (int x = 0; x < 64; x++) current[y * 64 + x] = small.GetPixel(x, y).ToArgb();
                }
            }
            double delta = 0;
            if (previous != null && previousDisplay == monitor.DeviceName) {
                for (int i = 0; i < current.Length; i++) {
                    Color a = Color.FromArgb(current[i]), b = Color.FromArgb(previous[i]);
                    delta += (Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B))/3.0;
                }
                delta /= current.Length * 255.0;
            }
            previous = current; previousDisplay = monitor.DeviceName;
            return delta;
        }
    }
}
