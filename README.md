<p align="center">
  <img src="assets/WinMdConverter.png" width="128" alt="WinMdConverter 圖示">
</p>

# WinMdConverter

[![版本](https://img.shields.io/github/v/release/mark216tw/WinMdConverter?display_name=tag)](https://github.com/mark216tw/WinMdConverter/releases/latest)
[![授權](https://img.shields.io/github/license/mark216tw/WinMdConverter)](LICENSE)
[![平台](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-176b63)](https://github.com/mark216tw/WinMdConverter)

WinMdConverter 是一套簡單、離線且實用的 Windows Markdown 文件轉換工具，可透過圖形介面或命令列將單一 Markdown 文件輸出為 HTML、PDF，或同時輸出兩種格式。

軟體不提供 Markdown 預覽，操作流程專注於選取文件、設定輸出及完成轉換。Markdown 內容全程在本機處理，不會上傳到網路服務。

## 下載

- [下載 Windows 安裝版](https://github.com/mark216tw/WinMdConverter/releases/download/v0.1.0/WinMdConverter-Setup-0.1.0-x64.exe)
- [下載 Windows 可攜版](https://github.com/mark216tw/WinMdConverter/releases/download/v0.1.0/WinMdConverter-0.1.0-win-x64.zip)
- [查看所有版本](https://github.com/mark216tw/WinMdConverter/releases)

目前提供 Windows x64 版本。安裝檔尚未進行程式碼簽章，Windows SmartScreen 可能在第一次執行時顯示提醒。

## 主要功能

- 使用 Markdig `UseAdvancedExtensions()` 解析 Markdown
- 支援 HTML、PDF 或同時輸出
- 支援 A4 直向與橫向
- 支援符合可列印區域及 50%～200% 自訂縮放
- 支援預設、無、最小值及四邊自訂邊界
- 可在文件開頭建立可點擊目錄
- 可選擇 Windows 已安裝字型
- 自動將本機圖片內嵌到 HTML
- 透過系統 Microsoft Edge Headless 產生 PDF
- 提供轉換進度、取消、覆寫確認及完成連結
- 提供 `mdconvert` CLI、JSON 輸出及明確 Exit Code
- 提供繁體中文介面與多解析度 Windows 圖示

## 系統需求

| 項目 | 需求 |
|---|---|
| 作業系統 | Windows 10 或 Windows 11 64 位元 |
| .NET | 安裝版與可攜版皆已內附 .NET 8 Runtime |
| PDF 輸出 | 需要 Microsoft Edge |
| HTML 輸出 | 不需要 Microsoft Edge |

## 快速使用

圖形介面：

1. 選擇 `.md` 或 `.markdown` 文件。
2. 選擇輸出資料夾與 HTML、PDF 格式。
3. 設定字型、方向、縮放、邊界及目錄。
4. 按下「開始轉換」。
5. 完成後直接開啟 HTML、PDF 或輸出資料夾。

CLI：

```powershell
mdconvert document.md --format both --toc
```

```powershell
mdconvert document.md --format pdf --orientation landscape --font "Microsoft JhengHei"
```

## 專案文件

| 文件 | 說明 |
|---|---|
| [使用指南](docs/使用指南.md) | 安裝、GUI 操作、輸出規則及常見問題 |
| [CLI 參考](docs/CLI參考.md) | 完整命令、參數、JSON 與 Exit Code |
| [開發與建置](docs/開發與建置.md) | 架構、開發環境、測試及 Windows 發佈 |
| [變更紀錄](CHANGELOG.md) | 各版本新增及調整內容 |
| [貢獻指南](CONTRIBUTING.md) | Issue、分支、測試及 Pull Request 規則 |
| [安全政策](SECURITY.md) | 安全問題回報方式及支援版本 |
| [第三方授權](THIRD-PARTY-NOTICES.md) | 使用的第三方元件與授權資訊 |

## 開發

```powershell
dotnet build WinMdConverter.sln
dotnet test WinMdConverter.sln
dotnet run --project src/WinMdConverter.App
```

建立 Windows Release：

```powershell
pwsh -File scripts/Publish.ps1
```

完整步驟請參閱[開發與建置文件](docs/開發與建置.md)。

## 隱私與安全

- Markdown 與本機圖片只在本機處理。
- 遠端圖片在轉換時可能由 Microsoft Edge 連線載入。
- Markdown 內嵌 HTML 預設停用，避免執行不受信任的腳本。
- 若文件含敏感資訊，請確認輸出資料夾與產生檔案的存取權限。

## 授權

本專案採用 [MIT License](LICENSE)，Copyright (c) 2026 Mark Lin。
