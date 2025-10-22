using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Wan22ToolDesigner_WebView2.Models;

namespace Wan22ToolDesigner_WebView2.Services
{
    public class HistoryService
    {
        private readonly string _baseDir;
        private readonly string _historyFile;
        public HistoryService()
        {
            _baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Wan22ToolDesigner");
            Directory.CreateDirectory(_baseDir);
            _historyFile = Path.Combine(_baseDir, "history.jsonl");
        }
        public string BaseDir => _baseDir;
        public string HistoryFile => _historyFile;

        public List<HistoryItem> LoadAll()
        {
            var list = new List<HistoryItem>();
            if (!File.Exists(_historyFile)) return list;
            foreach (var line in File.ReadAllLines(_historyFile))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var item = JsonConvert.DeserializeObject<HistoryItem>(line.Trim());
                    if (item != null) list.Add(item);
                }
                catch {}
            }
            list.Sort((a,b) => b.Timestamp.CompareTo(a.Timestamp));
            return list;
        }

        private bool EqualsExceptTimestamp(HistoryItem a, HistoryItem b)
        {
            return a.ImageLocalPath == b.ImageLocalPath
                && a.ImageUploadedUrl == b.ImageUploadedUrl
                && a.VideoLocalPath == b.VideoLocalPath
                && a.VideoUploadedUrl == b.VideoUploadedUrl
                && a.Prompt == b.Prompt
                && a.Seed == b.Seed
                && a.GenerateRawJson == b.GenerateRawJson
                && a.OutputVideoUrl == b.OutputVideoUrl
                && a.OutputLocalPath == b.OutputLocalPath
                && a.UrlIdVideo == b.UrlIdVideo
                && a.UrlVideo == b.UrlVideo
                && a.Error == b.Error;
        }

        public bool AppendIfNotDuplicate(HistoryItem item)
        {
            var all = LoadAll();
            foreach (var it in all) if (EqualsExceptTimestamp(it, item)) return false;
            File.AppendAllText(_historyFile, JsonConvert.SerializeObject(item) + Environment.NewLine);
            return true;
        }
    }
}