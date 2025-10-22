using System;

namespace Wan22ToolDesigner_WebView2.Models
{
    public class HistoryItem
    {
        public DateTime Timestamp { get; set; }
        public string? ImageLocalPath { get; set; }
        public string? ImageUploadedUrl { get; set; }
        public string? VideoLocalPath { get; set; }
        public string? VideoUploadedUrl { get; set; }
        public string? Prompt { get; set; }
        public int Seed { get; set; } = -1;
        public string? GenerateRawJson { get; set; }
        public string? OutputVideoUrl { get; set; }
        public string? OutputLocalPath { get; set; }
        public string? UrlIdVideo { get; set; }
        public string? UrlVideo { get; set; }
        public string? Error { get; set; }
    }
}