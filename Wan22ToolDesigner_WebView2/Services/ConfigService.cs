using System;
using System.IO;
using Newtonsoft.Json;
using Wan22ToolDesigner_WebView2.Models;

namespace Wan22ToolDesigner_WebView2.Services
{
    public class ConfigService
    {
        private readonly string _baseDir;
        private readonly string _configPath;
        public ConfigService()
        {
            _baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Wan22ToolDesigner");
            Directory.CreateDirectory(_baseDir);
            _configPath = Path.Combine(_baseDir, "config.json");
        }
        public AppConfig Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var cfg = JsonConvert.DeserializeObject<AppConfig>(json);
                    if (cfg != null) return cfg;
                }
            }
            catch {}
            return new AppConfig();
        }
        public void Save(AppConfig cfg)
        {
            var json = JsonConvert.SerializeObject(cfg, Formatting.Indented);
            File.WriteAllText(_configPath, json);
        }
        public string BaseDir => _baseDir;
        public string ConfigPath => _configPath;
    }
}