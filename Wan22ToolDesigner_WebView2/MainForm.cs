using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Wan22ToolDesigner_WebView2.Models;
using Wan22ToolDesigner_WebView2.Services;

namespace Wan22ToolDesigner_WebView2
{
    public partial class MainForm : Form
    {
        private readonly HttpService _http = new(TimeSpan.FromMinutes(30));
        private readonly ConfigService _configService = new();
        private AppConfig _config = new();
        private int _pageNo = 1;
        private int _total = 0;
        public string HistoryUrl { get; set; } = "https://console.atlascloud.ai/api/v1/model/history";
        public string PredictionBaseUrl { get; set; } = "https://api.atlascloud.ai/api/v1/model/prediction/";
        private int PageSize => (int)numPageSize.Value;
        private string? pollUrl;
        private bool isPolling;
        private DateTime? processingStartAt;

        public MainForm()
        {
            InitializeComponent();
            BuildBillHistoryColumns();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _config = _configService.Load();
            txtApiKey.Text = _config.ApiKey ?? "";
            txtAccountId.Text = _config.AccountId ?? "";
            txtAccessToken.Text = _config.AccessToken ?? "";
            txtUploadUrl.Text = _config.UploadUrl;
            txtGenerateUrl.Text = _config.GenerateUrl;
            txtDownloadPathBill.Text = _config.DownloadPathBill;
        }

        private async void MainForm_Shown(object sender, EventArgs e)
        {
            await EnsureWebView2Async();
            await RefreshBalanceAsync();
            _ = LoadPageAsync(1);
            //BuildMarqueeHeader();
            //animTitleTimer?.Start();
        }

        private async Task EnsureWebView2Async()
        {
            try
            {
                if (webView.CoreWebView2 == null)
                    await webView.EnsureCoreWebView2Async();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Khởi tạo WebView2 lỗi: " + ex.Message);
            }
        }

        public async Task LoadPageAsync(int page)
        {
            try
            {
                _pageNo = Math.Max(1, page);
                _http.SetAuthorizationBearer(_config.ApiKey);

                string url = $"{HistoryUrl}?no={_pageNo}&size={PageSize}";
                var resp = await _http.GetStringAsync(url);
                var root = JObject.Parse(resp);
                var code = root.SelectToken("code")?.ToString();
                if (code != "200" && code != "OK" && code != "ok") throw new Exception("API code != 200");

                _total = root.SelectToken("data.total")?.Value<int?>() ?? 0;
                var items = root.SelectToken("data.items") as JArray ?? new JArray();

                var rows = items.Select(x => new
                {
                    ID = x["ID"]?.ToString() ?? "",
                    model = x["model"]?.ToString() ?? "",
                    status = x["status"]?.ToString() ?? "",
                    createdAt = x["createdAt"]?.Value<long?>() ?? 0L,
                    CreatedAtFmt = FromUnixSeconds(x["createdAt"]?.Value<long?>() ?? 0L).ToString("yyyy-MM-dd HH:mm:ss")
                }).ToList();

                grid.DataSource = rows;
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                lblPage.Text = $"Trang: {_pageNo}";
                lblTotal.Text = $"Tổng: {_total}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải Lịch sử Bill API: " + ex.Message);
            }
        }
        private void BuildBillHistoryColumns()
        {
            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();

            var colCreated = new DataGridViewTextBoxColumn { Name = "CreatedAtFmt", HeaderText = "Ngày giờ", DataPropertyName = "CreatedAtFmt", Width = 160 };
            var colModel = new DataGridViewTextBoxColumn { Name = "model", HeaderText = "Model", DataPropertyName = "model", Width = 260 };
            var colStatus = new DataGridViewTextBoxColumn { Name = "status", HeaderText = "Status", DataPropertyName = "status", Width = 120 };
            var colId = new DataGridViewTextBoxColumn { Name = "ID", HeaderText = "ID", DataPropertyName = "ID", Width = 340 };
            var colBtn = new DataGridViewButtonColumn { Name = "colAction", HeaderText = "Xử lý", Text = "Xem & tải xuống", UseColumnTextForButtonValue = true, Width = 160 };
            grid.Columns.AddRange(new DataGridViewColumn[] { colCreated, colModel, colStatus, colId, colBtn });
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }
        private void grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (grid.Columns[e.ColumnIndex].Name == "status" && e.Value != null)
            {
                string status = e.Value == null ? "" : e.Value.ToString()!.ToLower();
                var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];

                cell.Style.Font = new Font(grid.Font, FontStyle.Bold);

                switch (status)
                {
                    case "created":
                        cell.Style.ForeColor = Color.Yellow; // vàng nhạt
                        break;
                    case "completed":
                        cell.Style.ForeColor = Color.Blue; // xanh nhạt
                        break;
                    default:
                        cell.Style.ForeColor = Color.Cyan; // xanh nhạt khác
                        break;
                }
            }
        }

        private static DateTime FromUnixSeconds(long sec)
        {
            if (sec <= 0) return DateTime.MinValue;
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(sec).ToLocalTime();
        }

        private void btnPrev_Click(object? sender, EventArgs e)
        {
            if (_pageNo > 1) _ = LoadPageAsync(_pageNo - 1);
        }

        private void btnNext_Click(object? sender, EventArgs e)
        {
            int maxPage = Math.Max(1, (int)Math.Ceiling(_total / (double)PageSize));
            if (_pageNo < maxPage) _ = LoadPageAsync(_pageNo + 1);
        }

        private void btnReset_Click(object? sender, EventArgs e)
        {
            _ = LoadPageAsync(1);
        }

        private async void gridBillHistory_CellContentClickAsync(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (!(grid.Columns[e.ColumnIndex] is DataGridViewButtonColumn)) return;

            var rowObj = grid.Rows[e.RowIndex].DataBoundItem;
            var id = rowObj?.GetType().GetProperty("ID")?.GetValue(rowObj)?.ToString();
            var status = rowObj?.GetType().GetProperty("Status")?.GetValue(rowObj)?.ToString();

            if (string.IsNullOrWhiteSpace(id)) { MessageBox.Show("Không tìm thấy ID."); return; }
            if (string.IsNullOrWhiteSpace(status) || status.ToLower() != "completed") { MessageBox.Show("Chưa hoàn tất."); return; }

            try
            {
                _http.SetAuthorizationBearer(_config.ApiKey);
                string predUrl = PredictionBaseUrl.Trim().TrimEnd('/') + "/" + id;
                var resp = await _http.GetStringAsync(predUrl);
                var root = JObject.Parse(resp);
                var outputs = root.SelectToken("data.outputs") as JArray;
                string? videoUrl = null;

                if (outputs != null)
                {
                    foreach (var el in outputs)
                    {
                        if (el.Type == JTokenType.String)
                        {
                            var s = el.ToString();
                            if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) { videoUrl = s; break; }
                        }
                        else
                        {
                            var maybe = el["url"]?.ToString() ?? el["download_url"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(maybe)) { videoUrl = maybe; break; }
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(videoUrl))
                    videoUrl = JsonUtils.ExtractFirstLikelyVideoUrl(resp);

                if (string.IsNullOrWhiteSpace(videoUrl))
                {
                    MessageBox.Show("Không tìm thấy link video trong outputs.");
                    return;
                }

                string baseDir = _config.DownloadPathBill ?? "";
                if (string.IsNullOrWhiteSpace(baseDir))
                {
                    MessageBox.Show("Cài đặt đường dẫn DownloadPathBill trong API Setting");
                    return;
                }
                Directory.CreateDirectory(baseDir);

                var fileName = Path.GetFileName(new Uri(videoUrl).AbsolutePath);
                if (string.IsNullOrWhiteSpace(Path.GetExtension(fileName))) fileName += ".mp4";
                var dest = Path.Combine(baseDir, $"{DateTime.Now:yyyyMMdd_HHmmss}_{id}_{fileName}");

                await _http.DownloadToFileAsync(videoUrl, dest);
                if (File.Exists(dest))
                    Process.Start(new ProcessStartInfo(dest) { UseShellExecute = true });
                else
                    MessageBox.Show("File tải về không tồn tại sau khi download.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải video: " + ex.Message);
            }
        }

        private void btnSaveConfig_Click(object? sender, EventArgs e)
        {
            _config.ApiKey = txtApiKey.Text?.Trim();
            _config.AccountId = txtAccountId.Text?.Trim();
            _config.AccessToken = txtAccessToken.Text?.Trim();
            _config.UploadUrl = string.IsNullOrWhiteSpace(txtUploadUrl.Text) ? _config.UploadUrl : txtUploadUrl.Text.Trim();
            _config.GenerateUrl = string.IsNullOrWhiteSpace(txtGenerateUrl.Text) ? _config.GenerateUrl : txtGenerateUrl.Text.Trim();
            _config.DownloadPathBill = string.IsNullOrWhiteSpace(txtDownloadPathBill.Text) ? _config.DownloadPathBill : txtDownloadPathBill.Text.Trim();
            _configService.Save(_config);
            MessageBox.Show("Đã cập nhật cấu hình.");
        }

        private void ApplyHeaders()
        {
            _http.SetAuthorizationBearer(_config.ApiKey);
            _http.SetXAccountId(_config.AccountId);
            _http.SetCookieAccessToken(_config.AccessToken);
        }

        private void btnBrowseImage_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog { Filter = "Image files|*.jpg;*.jpeg;*.png;*.webp;*.bmp" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtImagePath.Text = ofd.FileName;
                try { picPreview.Image = Image.FromFile(ofd.FileName); } catch { picPreview.Image = null; }
            }
        }

        private async void btnUploadImage_Click(object? sender, EventArgs e)
        {
            if (!File.Exists(txtImagePath.Text)) { MessageBox.Show("Chưa chọn file ảnh."); return; }
            try
            {
                Cursor = Cursors.WaitCursor;
                ApplyHeaders();
                lblUploadImageStatus.ForeColor = Color.DimGray; lblUploadImageStatus.Text = "Uploading...";
                var resp = await _http.UploadFileMultipartAsync(_config.UploadUrl, txtImagePath.Text);
                txtGenerateResponse.Text = resp;
                var jo = JObject.Parse(resp);
                var url = jo.SelectToken("data.download_url")?.ToString();
                if (string.IsNullOrWhiteSpace(url)) url = JsonUtils.ExtractFirstLikelyVideoUrl(resp);
                txtImageUploadedUrl.Text = url ?? "";
                lblUploadImageStatus.ForeColor = string.IsNullOrWhiteSpace(url) ? Color.OrangeRed : Color.SeaGreen;
                lblUploadImageStatus.Text = string.IsNullOrWhiteSpace(url) ? "Upload thất bại" : "Upload thành công";
            }
            catch (Exception ex)
            {
                lblUploadImageStatus.ForeColor = Color.OrangeRed; lblUploadImageStatus.Text = "Upload thất bại";
                MessageBox.Show("Upload ảnh lỗi: " + ex.Message);
            }
            finally { Cursor = Cursors.Default; }
        }

        private void btnBrowseVideo_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog { Filter = "Video files|*.mp4;*.mov;*.webm;*.avi;*.mkv" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtVideoPath.Text = ofd.FileName;
                _ = TryPreviewVideoAsync();
            }
        }

        private async void btnUploadVideo_Click(object? sender, EventArgs e)
        {
            if (!File.Exists(txtVideoPath.Text)) { MessageBox.Show("Chưa chọn file video."); return; }
            try
            {
                Cursor = Cursors.WaitCursor;
                ApplyHeaders();
                lblUploadVideoStatus.ForeColor = Color.DimGray; lblUploadVideoStatus.Text = "Uploading...";
                var resp = await _http.UploadFileMultipartAsync(_config.UploadUrl, txtVideoPath.Text);
                txtGenerateResponse.Text = resp;
                var jo = JObject.Parse(resp);
                var url = jo.SelectToken("data.download_url")?.ToString();
                if (string.IsNullOrWhiteSpace(url)) url = JsonUtils.ExtractFirstLikelyVideoUrl(resp);
                txtVideoUploadedUrl.Text = url ?? "";
                lblUploadVideoStatus.ForeColor = string.IsNullOrWhiteSpace(url) ? Color.OrangeRed : Color.SeaGreen;
                lblUploadVideoStatus.Text = string.IsNullOrWhiteSpace(url) ? "Upload thất bại" : "Upload thành công";
                await TryPreviewVideoAsync();
            }
            catch (Exception ex)
            {
                lblUploadVideoStatus.ForeColor = Color.OrangeRed; lblUploadVideoStatus.Text = "Upload thất bại";
                MessageBox.Show("Upload video lỗi: " + ex.Message);
            }
            finally { Cursor = Cursors.Default; }
        }

        private async void btnReplay_Click(object? sender, EventArgs e)
        {
            if (webView?.CoreWebView2 != null)
                await webView.CoreWebView2.ExecuteScriptAsync("replay && replay();");
        }

        private async Task TryPreviewVideoAsync()
        {
            try
            {
                var path = txtVideoPath.Text;
                if (!File.Exists(path)) return;

                await EnsureWebView2Async();

                var folder = Path.GetDirectoryName(path)!;
                var fileName = Path.GetFileName(path);

                // Map thư mục local -> host ảo: https://local.videos/
                // Allow để video được load (bao gồm cross-origin nội bộ)
                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "local.videos",
                    folder,
                    CoreWebView2HostResourceAccessKind.Allow
                );

                // Tạo URL an toàn (không dùng file:/// nữa)
                var safeUrl = $"https://local.videos/{Uri.EscapeDataString(fileName)}";

                // Nạp HTML5 <video> tham chiếu tới safeUrl
                var html = $@"
                <!doctype html>
                <html><head><meta charset='utf-8' />
                <style>
                  html,body{{margin:0;height:100%}}
                  video{{width:100%;height:100%;object-fit:contain;background:#000}}
                  body{{background:#000}}
                </style>
                </head>
                <body>
                  <video id='v' src='{safeUrl}' controls autoplay></video>
                  <script>
                    window.replay = function(){{
                      var v = document.getElementById('v');
                      if (!v) return;
                      v.pause(); v.currentTime = 0; v.play();
                    }};
                  </script>
                </body></html>";

                webView.CoreWebView2.NavigateToString(html);
            }
            catch
            {
                // im lặng để không làm phiền user; có thể log nếu cần
            }
        }


        private void btnClear_Click(object? sender, EventArgs e)
        {
            txtImagePath.Clear();
            txtVideoPath.Clear();
            txtPrompt.Clear();
            numSeed.Value = -1;
            txtImageUploadedUrl.Clear();
            txtVideoUploadedUrl.Clear();
            txtGenerateResponse.Clear();
            txtOutputVideoUrl.Clear();
            picPreview.Image = null;
            lblUploadImageStatus.Text = "";
            lblUploadVideoStatus.Text = "";
            if (webView?.CoreWebView2 != null) webView.CoreWebView2.NavigateToString("<html></html>");
        }

        private async Task RefreshBalanceAsync()
        {
            try
            {
                ApplyHeaders();
                var resp = await _http.GetStringAsync("https://console.atlascloud.ai/api/v1/credit/balance");
                var amount = JObject.Parse(resp).SelectToken("data.amount")?.ToString();
                lblBalance.Text = string.IsNullOrWhiteSpace(amount) ? "Số dư: --" : $"Số dư: {amount}";
            }
            catch { lblBalance.Text = "Số dư: --"; }
        }

        private async void btnGenerate_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtImageUploadedUrl.Text) || string.IsNullOrWhiteSpace(txtVideoUploadedUrl.Text))
            { MessageBox.Show("Cần upload ẢNH và VIDEO trước khi tạo."); return; }

            try
            {
                Cursor = Cursors.WaitCursor;
                ApplyHeaders();
                await RefreshBalanceAsync();

                var body = new
                {
                    model = "alibaba/wan-2.2/animate",
                    image = txtImageUploadedUrl.Text.Trim(),
                    mode = "animate",
                    prompt = string.IsNullOrWhiteSpace(txtPrompt.Text) ? null : txtPrompt.Text.Trim(),
                    resolution = "720p",
                    seed = (int)numSeed.Value,
                    video = txtVideoUploadedUrl.Text.Trim()
                };
                var json = JsonConvert.SerializeObject(body, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                var resp = await _http.PostJsonAsync(_config.GenerateUrl, json);
                txtGenerateResponse.Text = resp;

                var getUrl = JObject.Parse(resp).SelectToken("data.urls.get")?.ToString();
                if (!string.IsNullOrWhiteSpace(getUrl))
                {
                    var itemStart = new HistoryItem
                    {
                        Timestamp = DateTime.Now,
                        ImageLocalPath = File.Exists(txtImagePath.Text) ? txtImagePath.Text : null,
                        VideoLocalPath = File.Exists(txtVideoPath.Text) ? txtVideoPath.Text : null,
                        ImageUploadedUrl = txtImageUploadedUrl.Text,
                        VideoUploadedUrl = txtVideoUploadedUrl.Text,
                        Prompt = txtPrompt.Text,
                        Seed = (int)numSeed.Value,
                        GenerateRawJson = resp,
                        UrlIdVideo = getUrl
                    };
                    //_history.AppendIfNotDuplicate(itemStart);
                    StartPolling(getUrl);
                    return;
                }

                var outUrl = JsonUtils.ExtractFirstLikelyVideoUrl(resp);
                txtOutputVideoUrl.Text = outUrl ?? "";
                var itemFallback = new HistoryItem
                {
                    Timestamp = DateTime.Now,
                    ImageLocalPath = File.Exists(txtImagePath.Text) ? txtImagePath.Text : null,
                    VideoLocalPath = File.Exists(txtVideoPath.Text) ? txtVideoPath.Text : null,
                    ImageUploadedUrl = txtImageUploadedUrl.Text,
                    VideoUploadedUrl = txtVideoUploadedUrl.Text,
                    Prompt = txtPrompt.Text,
                    Seed = (int)numSeed.Value,
                    GenerateRawJson = resp,
                    OutputVideoUrl = outUrl,
                    UrlVideo = outUrl
                };
                if (!string.IsNullOrWhiteSpace(outUrl)) await DownloadAndOpenAsync(outUrl, itemFallback);
                else MessageBox.Show("Không tìm thấy URL video trong response.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tạo video lỗi: " + ex.Message);
            }
            finally { Cursor = Cursors.Default; }
        }

        private void isUICreateEnable(bool enable)
        {
            btnBrowseImage.Enabled = enable;
            btnBrowseVideo.Enabled = enable;
            btnUploadImage.Enabled = enable;
            btnUploadVideo.Enabled = enable;
            btnClear.Enabled = enable;
            btnDownloadAndOpen.Enabled = enable;
            btnGenerate.Enabled = enable;
        }

        private async void StartPolling(string url)
        {
            isUICreateEnable(false);
            await RefreshBalanceAsync();
            pollUrl = url; isPolling = true;
            processingStartAt = DateTime.Now;
            elapsedTimer.Start();
            UpdateStatusUI("Đang xử lý…", true);
            pollTimer.Interval = 5000;
            pollTimer.Start();
        }

        private async void StopPolling()
        {
            isUICreateEnable(true);
            pollTimer.Stop();
            isPolling = false;
            UpdateStatusUI("Idle", false);
            processingStartAt = null;
            elapsedTimer.Stop();
            lblElapsed.Text = "Elapsed: 00:00";
            await RefreshBalanceAsync();
        }

        private async void pollTimer_Tick(object? sender, EventArgs e)
        {
            if (!isPolling || string.IsNullOrWhiteSpace(pollUrl)) return;
            try
            {
                ApplyHeaders();
                var resp = await _http.GetStringAsync(pollUrl);
                txtGenerateResponse.Text = resp;

                var root = JObject.Parse(resp);
                var code = root.SelectToken("code")?.Value<int?>();
                var status = root.SelectToken("data.status")?.ToString() ?? "";
                var outputsToken = root.SelectToken("data.outputs");

                if (code != 200)
                {
                    StopPolling();
                    var itemErr = new HistoryItem { Timestamp = DateTime.Now, GenerateRawJson = resp, Error = "Polling lỗi: code != 200" };
                    MessageBox.Show("Polling lỗi: code != 200");
                    return;
                }

                if (status.Equals("processing", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("queued", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("created", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateStatusUI($"Đang xử lý… (status: {status})", true);
                    return;
                }

                if (status.Equals("completed", StringComparison.OrdinalIgnoreCase))
                {
                    StopPolling();
                    string? videoUrl = null;
                    if (outputsToken is JArray arr && arr.Count > 0)
                    {
                        foreach (var el in arr)
                        {
                            if (el.Type == JTokenType.String)
                            {
                                var s = el.ToString();
                                if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) { videoUrl = s; break; }
                            }
                            else
                            {
                                var maybe = el.SelectToken("url")?.ToString() ?? el.SelectToken("download_url")?.ToString();
                                if (!string.IsNullOrWhiteSpace(maybe) && (maybe.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || maybe.StartsWith("https://", StringComparison.OrdinalIgnoreCase))) { videoUrl = maybe; break; }
                            }
                        }
                    }
                    if (string.IsNullOrWhiteSpace(videoUrl)) videoUrl = JsonUtils.ExtractFirstLikelyVideoUrl(resp);
                    txtOutputVideoUrl.Text = videoUrl ?? "";

                    var itemDone = new HistoryItem
                    {
                        Timestamp = DateTime.Now,
                        ImageLocalPath = File.Exists(txtImagePath.Text) ? txtImagePath.Text : null,
                        VideoLocalPath = File.Exists(txtVideoPath.Text) ? txtVideoPath.Text : null,
                        ImageUploadedUrl = txtImageUploadedUrl.Text,
                        VideoUploadedUrl = txtVideoUploadedUrl.Text,
                        Prompt = txtPrompt.Text,
                        Seed = (int)numSeed.Value,
                        GenerateRawJson = resp,
                        OutputVideoUrl = videoUrl,
                        UrlVideo = videoUrl
                    };

                    if (!string.IsNullOrWhiteSpace(videoUrl)) await DownloadAndOpenAsync(videoUrl, itemDone);
                    else MessageBox.Show("Hoàn tất nhưng không tìm thấy video URL trong outputs.");
                    return;
                }

                StopPolling();
                var err = root.SelectToken("data.error")?.ToString();
                var itemErr2 = new HistoryItem
                {
                    Timestamp = DateTime.Now,
                    ImageLocalPath = File.Exists(txtImagePath.Text) ? txtImagePath.Text : null,
                    VideoLocalPath = File.Exists(txtVideoPath.Text) ? txtVideoPath.Text : null,
                    ImageUploadedUrl = txtImageUploadedUrl.Text,
                    VideoUploadedUrl = txtVideoUploadedUrl.Text,
                    Prompt = txtPrompt.Text,
                    Seed = (int)numSeed.Value,
                    GenerateRawJson = resp,
                    Error = string.IsNullOrWhiteSpace(err) ? $"Trạng thái không hợp lệ: {status}" : err
                };
                MessageBox.Show(string.IsNullOrWhiteSpace(err) ? $"Trạng thái không hợp lệ: {status}" : $"Lỗi: {err}");
            }
            catch (Exception ex)
            {
                StopPolling();
                var itemErr = new HistoryItem
                {
                    Timestamp = DateTime.Now,
                    ImageLocalPath = File.Exists(txtImagePath.Text) ? txtImagePath.Text : null,
                    VideoLocalPath = File.Exists(txtVideoPath.Text) ? txtVideoPath.Text : null,
                    ImageUploadedUrl = txtImageUploadedUrl.Text,
                    VideoUploadedUrl = txtVideoUploadedUrl.Text,
                    Prompt = txtPrompt.Text,
                    Seed = (int)numSeed.Value,
                    GenerateRawJson = ex.ToString(),
                    Error = "Polling exception: " + ex.Message
                };
                MessageBox.Show("Polling lỗi: " + ex.Message);
            }
        }

        private void UpdateStatusUI(string text, bool busy) { lblStatus.Text = text; prgStatus.Visible = busy; }

        private void elapsedTimer_Tick(object? sender, EventArgs e)
        {
            string text = "Elapsed: 00:00";
            if (processingStartAt.HasValue)
            {
                var span = DateTime.Now - processingStartAt.Value;
                text = $"Elapsed: {(span.TotalMinutes):00}:{span.Seconds:00}";
            }
            lblElapsed.Text = text;
        }

        private async void btnDownloadAndOpen_Click(object? sender, EventArgs e)
        {
            var url = txtOutputVideoUrl.Text?.Trim();
            if (string.IsNullOrWhiteSpace(url)) { MessageBox.Show("Chưa có Video URL."); return; }
            await DownloadAndOpenAsync(url, null, false);
        }

        private async Task DownloadAndOpenAsync(string url, HistoryItem? existingHistoryItem, bool? saveHis = true)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                var fileName = Path.GetFileName(new Uri(url).AbsolutePath);
                if (string.IsNullOrWhiteSpace(Path.GetExtension(fileName))) fileName += ".mp4";
                var dest = Path.Combine(_config.DownloadPathBill!, $"{DateTime.Now:yyyyMMdd_HHmmss}_{fileName}");
                ApplyHeaders();
                await _http.DownloadToFileAsync(url, dest);

                if (existingHistoryItem != null)
                {
                    if (saveHis == true)
                    {
                        existingHistoryItem.OutputLocalPath = dest;
                    }
                }

                if (File.Exists(dest)) Process.Start(new ProcessStartInfo(dest) { UseShellExecute = true });
                else MessageBox.Show("File tải về không tồn tại sau khi download.");
            }
            catch (Exception ex) { MessageBox.Show("Tải video lỗi: " + ex.Message); }
            finally { Cursor = Cursors.Default; }
        }

        // ===== Marquee Header =====
        //private void BuildMarqueeHeader()
        //{
        //    int marqueeDx = 2;
        //    animTitleTimer.Tick += (s, e) =>
        //    {
        //        //if (lblRuntitle.Parent is null) return;
        //        var p = lblRuntitle.Location;
        //        if (p.X >= headerPanel.Width)
        //            p.X = 0;
        //        else
        //            p.X += marqueeDx;
        //        lblRuntitle.Location = p;
        //    };
        //}


        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnPrev_Click_1(object? sender, EventArgs e)
        {

        }

        private void btnNext_Click_1(object? sender, EventArgs e)
        {
            int maxPage = Math.Max(1, (int)Math.Ceiling(_total / (double)PageSize));
            if (_pageNo < maxPage) _ = LoadPageAsync(_pageNo + 1);
        }

        private void btnReset_Click_1(object? sender, EventArgs e)
        {
            _ = LoadPageAsync(1);
        }

        private void btnPrev_Click_2(object sender, EventArgs e)
        {
            if (_pageNo > 1) _ = LoadPageAsync(_pageNo - 1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    txtDownloadPathBill.Text = fbd.SelectedPath;
                }
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            await RefreshBalanceAsync();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            txtDownloadPathBill.Text = Path.Combine(_config.DownloadPathBill!);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button5_Click(object sender, EventArgs e)
        => Process.Start(new ProcessStartInfo(_config.DownloadPathBill!) { UseShellExecute = true });

        private void button6_Click(object sender, EventArgs e)
        {
            if(btnDisplayAPIkey.Text != "Ẩn")
            {
                btnDisplayAPIkey.Text = "Ẩn";
                txtApiKey.UseSystemPasswordChar = false;
            }
            else
            {
                btnDisplayAPIkey.Text = "Hiện";
                txtApiKey.UseSystemPasswordChar = true;
            }
           

        }
    }
}