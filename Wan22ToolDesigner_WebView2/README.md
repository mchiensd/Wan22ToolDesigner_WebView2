# Wan22ToolDesigner_WebView2
- **Không dùng TableLayoutPanel/SplitContainer**: toàn bộ layout bằng **Panel/GroupBox + Anchor** → bạn mở Designer và kéo thả dễ.
- **Thay MediaElement** bằng **WebView2 (WinForms)** + nút **Replay** (gọi JS `replay()`).
- **UploadFileMultipartAsync**: tự set Content-Type theo đuôi file, báo lỗi chi tiết.
- **API Settings** là tab riêng, có **Update (Save)** → lưu `Documents/Wan22ToolDesigner/config.json`.
- Các yêu cầu về: Số dư, Polling 5s, Elapsed time, 2 lần lưu lịch sử (URL_ID_VIDEO khi tạo + URL_VIDEO khi completed), chống trùng, nút Tải & Xem — đều đã có.

Mở **Wan22ToolDesigner_WebView2.sln** bằng Visual Studio 2022 (.NET 6), vào tab **API Settings** để nhập thông tin rồi dùng.