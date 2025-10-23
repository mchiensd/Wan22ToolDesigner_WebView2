using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Wan22ToolDesigner_WebView2.Services
{
    public static class JsonUtils
    {
        public static string? ExtractFirstLikelyVideoUrl(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var root = JToken.Parse(json);
                var urls = new List<string>();
                if (root.Type == JTokenType.String)
                {
                    var s0 = (string?)root ?? string.Empty;
                    if (s0.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s0.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) urls.Add(s0);
                }
                foreach (var t in root!.SelectTokens("$..*"))
                {
                    if (t.Type == JTokenType.String)
                    {
                        var s = (string?)t ?? string.Empty;
                        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) urls.Add(s);
                    }
                }
                foreach (var u in urls)
                {
                    var lower = u.ToLowerInvariant();
                    if (lower.EndsWith(".mp4") || lower.EndsWith(".webm") || lower.EndsWith(".mov")) return u;
                }
                foreach (var u in urls)
                {
                    var lower = u.ToLowerInvariant();
                    if (lower.Contains("video") || lower.Contains("output")) return u;
                }
                if (urls.Count > 0) return urls[0];
                var m = Regex.Match(json, @"https?://[\w\-\./?%&=+#:]+", RegexOptions.IgnoreCase);
                if (m.Success) return m.Value;
            }
            catch
            {
                var m = Regex.Match(json, @"https?://[\w\-\./?%&=+#:]+", RegexOptions.IgnoreCase);
                if (m.Success) return m.Value;
            }
            return null;
        }
    }
}