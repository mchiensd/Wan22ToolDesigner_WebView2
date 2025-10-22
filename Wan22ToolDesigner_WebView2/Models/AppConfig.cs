namespace Wan22ToolDesigner_WebView2.Models
{
    public class AppConfig
    {
        public string? ApiKey { get; set; }
        public string? AccountId { get; set; }
        public string? AccessToken { get; set; }
        public string UploadUrl { get; set; } = "https://console.atlascloud.ai/api/v1/model/uploadMedia";
        public string GenerateUrl { get; set; } = "https://api.atlascloud.ai/api/v1/model/generateVideo";
        public string? DownloadPathBill { get; set; }
    }
}