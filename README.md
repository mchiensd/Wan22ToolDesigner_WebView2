# Wan22ToolDesigner WebView2.3

## 🧩 Tổng quan
**Wan22ToolDesigner_WebView2.3** là phiên bản nâng cấp của tool AI video generator, kết hợp toàn bộ tính năng gốc (tạo video, upload, hiển thị WebView2, lưu lịch sử nội bộ, cài đặt API) và bổ sung thêm **tab “Lịch sử Bill API”** để truy cập lịch sử từ hệ thống Atlas Cloud.

Ứng dụng viết bằng **.NET 8 WinForms**, sử dụng `WebView2` cho giao diện web, `HttpClient` cho gọi API và `Newtonsoft.Json` để xử lý JSON.

---

## ⚙️ Các tab và chức năng chính

### 1️⃣ Tab “Tạo video” (`tabCreate`)
- Nhập prompt, chọn ảnh và video đầu vào.
- Upload ảnh/video qua API upload của Atlas.
- Gửi yêu cầu tạo video qua API Generate.
- Theo dõi trạng thái xử lý, hiển thị WebView2 kết quả và thời gian chạy.
- Tự lưu lịch sử lệnh gọi trong file `history.jsonl` tại thư mục Documents/Wan22ToolDesigner.

### 2️⃣ Tab “Lịch sử” (`tabHistory`)
- Hiển thị danh sách các video đã tạo trong lịch sử nội bộ (từ file `history.jsonl`).
- Có nút **“Tải & Xem”** để mở lại file video đã lưu.
- Có thể **mở thư mục lịch sử** hoặc **làm mới** danh sách.

### 3️⃣ Tab “Cài đặt (API Setting)” (`tabSettings`)
- Cho phép nhập và lưu thông tin API:
  - `API Key` – dùng để xác thực các yêu cầu (không public).
  - `AccountId`, `AccessToken`, `UploadUrl`, `GenerateUrl`.
- Thêm 2 trường mới cho tính năng Bill API:
  - **Đường dẫn download (Bill)** – chọn thư mục lưu video tải xuống.
  - **Số dòng/trang (Bill API)** – xác định số bản ghi hiển thị mỗi trang khi gọi API Bill.
- Tất cả cài đặt được lưu tại `Documents/Wan22ToolDesigner/config.json`.

### 4️⃣ Tab “Lịch sử Bill API” (`tabBillApiHistory`)
> ✅ *Tính năng mới trong bản 2.3*

- **Mục đích:** Lấy lịch sử các yêu cầu model từ API của Atlas Cloud.
- Gọi API:
  ```http
  GET https://console.atlascloud.ai/api/v1/model/history?no={page}&size={pageSize}
  Authorization: Bearer <API_KEY>
  ```
- Hiển thị các cột: **ID**, **Model**, **Status**, **CreatedAt**, **Hành động (Xem và tải xuống)**.
- Có các nút:
  - **Reset:** làm mới danh sách từ trang 1.
  - **Prev / Next:** phân trang dữ liệu.
- Khi bấm **“Xem và tải xuống”**:
  - Gọi tiếp API:
    ```http
    GET https://api.atlascloud.ai/api/v1/model/prediction/{ID}
    Authorization: Bearer <API_KEY>
    ```
  - Trích `response.data.outputs` → lấy link video.
  - Tải video về thư mục “Đường dẫn download (Bill)” đã chọn.
  - Mở video sau khi tải hoàn tất.

---

## 💾 Cấu trúc file lưu
| File | Mục đích |
|------|-----------|
| `Documents/Wan22ToolDesigner/config.json` | Lưu cấu hình (API key, URLs, thư mục download, page size) |
| `Documents/Wan22ToolDesigner/history.jsonl` | Lưu lịch sử các yêu cầu tạo video cục bộ |
| `AppConfig.cs` | Model cấu hình |
| `ConfigService.cs` | Dịch vụ load/save cấu hình |
| `HistoryService.cs` | Dịch vụ quản lý lịch sử nội bộ |
| `MainForm.cs` / `.Designer.cs` | Form chính – chứa toàn bộ giao diện & logic chính |

---

## 🚀 Hướng dẫn sử dụng

### Cài đặt
1. Cài .NET 8 SDK (Windows 10 trở lên).
2. Giải nén file `Wan22ToolDesigner_WebView2.3.zip`.
3. Mở project trong **Visual Studio 2022** hoặc dùng CLI:
   ```bash
   dotnet build Wan22ToolDesigner_WebView2.3.sln
   dotnet run --project Wan22ToolDesigner_WebView2/Wan22ToolDesigner_WebView2.csproj
   ```

### Thiết lập API
1. Vào tab **Cài đặt (API Setting)**.
2. Nhập API Key (Bearer từ Atlas Cloud).
3. Chọn **Đường dẫn download (Bill)** bằng nút **Chọn...**.
4. Đặt số dòng/trang theo ý (mặc định 10).
5. Nhấn **Lưu cài đặt**.

### Sử dụng “Lịch sử Bill API”
1. Chuyển qua tab **Lịch sử Bill API**.
2. Ứng dụng tự động tải danh sách từ Atlas.
3. Dùng **Prev / Next** để xem các trang.
4. Bấm **“Xem và tải xuống”** để lưu video về thư mục chỉ định.

---

## 🛠️ Kỹ thuật
- **Ngôn ngữ:** C# (.NET 8 WinForms)
- **Thư viện chính:**
  - `Newtonsoft.Json` – parse JSON
  - `System.Net.Http` – gọi API
  - `Microsoft.Web.WebView2` – hiển thị video & nội dung web
- **Lưu trữ cục bộ:** JSON trong Documents/Wan22ToolDesigner
- **Phân trang:** tính tự động theo `total`, `pageNo`, `pageSize`

---

## 🔒 Lưu ý bảo mật
- Không commit hay chia sẻ file `config.json` chứa API Key.
- Nếu cần chia sẻ project, hãy xóa dòng `"ApiKey": ...` khỏi config.

---

## 📦 Phiên bản
**Wan22ToolDesigner_WebView2.3**
- Dựa trên checkpoint `WebView2.2`
- Thêm tab “Lịch sử Bill API”
- Giữ nguyên toàn bộ giao diện và tính năng cũ
- Cải thiện lưu config & cấu hình tải video

---

© 2025 – MC Solutions | Dev: Chien Tran
