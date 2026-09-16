using System;

namespace Miareve {
    public static class StudyPolicy {
        public static readonly string DefaultKeywords = "after effects,애프터 이펙트,애프터이펙트,에프터 이펙트,애팩,에펙,illustrator,일러스트레이터,blender,블렌더,unity,유니티,motion graphics,모션그래픽,타이포그래피,그래픽 디자인";
        public static bool KnownApp(string process) {
            string p = (process ?? "").ToLowerInvariant();
            return p == "afterfx" || p == "illustrator" || p == "blender" || p == "unity" || p == "photoshop";
        }
        public static bool RelatedTitle(string title, string keywords) {
            foreach (string word in (keywords ?? "").Split(',')) {
                string w = word.Trim();
                if (w.Length > 0 && (title ?? "").IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }
        public static bool IsStudying(string process, string title, string keywords, double idleSeconds, bool screenChanged) {
            return (KnownApp(process) || RelatedTitle(title, keywords)) && (idleSeconds < 180 || screenChanged);
        }
    }
    public sealed class FocusClock {
        public double AwaySeconds { get; private set; }
        public double StudySeconds { get; private set; }
        public void ResetAway() { AwaySeconds = 0; }
        public void Tick(bool studying, double seconds) {
            // A delayed tick after suspend must never count as a long absence.
            if (seconds < 0 || seconds > 20) { ResetAway(); return; }
            if (studying) { StudySeconds += seconds; ResetAway(); }
            else AwaySeconds += seconds;
        }
    }
}
