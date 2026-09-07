# 變更紀錄

本文件依據 [Keep a Changelog](https://keepachangelog.com/zh-TW/1.1.0/) 格式維護，版本號遵循[語意化版本](https://semver.org/lang/zh-TW/)。

## [0.2.0] - 2026-09-07

### 新增

- GUI 與 CLI 支援 Markdown 轉 DOCX。
- DOCX 支援標準 Markdown 結構、本機圖片、靜態目錄、字型、A4 方向及邊界。
- CLI `--format` 支援 `docx`、逗號組合及 `all`。

### 變更

- CLI `--format` 改為必填並移除 `both`。
- 縮放比例明確限制為 HTML 與 PDF 選項。
- PDF 產生失敗時會改用相容的 Edge Headless 模式重試，並提供更完整診斷。
- 提高主介面次要文字與停用按鈕的色彩對比。
- 主介面頁尾顯示目前 WinMdConverter 軟體版本。

## [0.1.0] - 2026-09-05

### 新增

- WPF 繁體中文圖形介面。
- `mdconvert` CLI 與 JSON 輸出。
- Markdig Advanced Extensions。
- HTML、PDF 或同時輸出。
- A4 直向、橫向、縮放及邊界設定。
- 文件開頭階層式目錄與可點擊錨點。
- Windows 已安裝字型與常用中文字型別名。
- 本機圖片 Data URI 內嵌。
- Microsoft Edge Headless PDF 轉換。
- 轉換進度、取消、覆寫確認及完成連結。
- 多解析度 Windows 圖示。
- Windows x64 可攜版與繁體中文安裝程式。

[0.2.0]: https://github.com/mark216tw/WinMdConverter/releases/tag/v0.2.0
[0.1.0]: https://github.com/mark216tw/WinMdConverter/releases/tag/v0.1.0
