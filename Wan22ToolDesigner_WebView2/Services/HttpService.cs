using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Wan22ToolDesigner_WebView2.Services
{
    public class HttpService : IDisposable
    {
        private readonly HttpClient _client;
        public HttpService(TimeSpan? timeout = null)
        {
            _client = new HttpClient();
            _client.Timeout = timeout ?? TimeSpan.FromMinutes(10);
        }
        public void SetAuthorizationBearer(string? apiKey)
        {
            _client.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrWhiteSpace(apiKey))
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        public void SetCookieAccessToken(string? accessToken)
        {
            _client.DefaultRequestHeaders.Remove("Cookie");
            if (!string.IsNullOrWhiteSpace(accessToken))
                _client.DefaultRequestHeaders.Add("Cookie", $"access-token={accessToken}");
        }
        public void SetXAccountId(string? accountId)
        {
            _client.DefaultRequestHeaders.Remove("x-account-id");
            if (!string.IsNullOrWhiteSpace(accountId))
                _client.DefaultRequestHeaders.Add("x-account-id", accountId);
        }

        public async Task<string> UploadFileMultipartAsync(string url, string filePath, string formField = "file")
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);
            using var form = new MultipartFormDataContent();
            var fileName = Path.GetFileName(filePath);
            var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                var fileContent = new StreamContent(stream);
                string contentType = "application/octet-stream";
                var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
                contentType = ext switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    ".bmp" => "image/bmp",
                    ".mp4" => "video/mp4",
                    ".mov" => "video/quicktime",
                    ".webm" => "video/webm",
                    ".avi" => "video/x-msvideo",
                    ".mkv" => "video/x-matroska",
                    _ => "application/octet-stream"
                };
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                form.Add(fileContent, formField, fileName);

                using var resp = await _client.PostAsync(url, form);
                var content = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                    throw new Exception($"Upload failed ({(int)resp.StatusCode} {resp.ReasonPhrase}): {content}");
                return content;
            }
            finally { stream.Dispose(); }
        }

        public async Task<string> PostJsonAsync(string url, string jsonBody)
        {
            using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            using var resp = await _client.PostAsync(url, content);
            var respContent = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"POST failed ({(int)resp.StatusCode}): {respContent}");
            return respContent;
        }

        public async Task<string> GetStringAsync(string url)
        {
            using var resp = await _client.GetAsync(url);
            var content = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"GET failed ({(int)resp.StatusCode}): {content}");
            return content;
        }

        public async Task<string> DownloadToFileAsync(string url, string dest)
        {
            using var resp = await _client.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            using var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None);
            await resp.Content.CopyToAsync(fs);
            return dest;
        }

        public void Dispose() => _client.Dispose();
    }
}