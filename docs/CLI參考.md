# mdconvert CLI 參考

## 基本語法

```text
mdconvert <input.md> [options]
mdconvert --list-fonts
```

若使用可攜版但尚未加入 PATH，請在解壓縮資料夾內執行：

```powershell
.\mdconvert.exe document.md --format html,pdf,docx
```

## 參數

| 參數 | 說明 | 預設值 |
|---|---|---|
| `<input>` | 單一 `.md` 或 `.markdown` 文件 | 必填 |
| `--format <formats>` | 必填；`html`、`pdf`、`docx`、逗號組合或 `all` | 必填 |
| `-o, --output <directory>` | 輸出資料夾 | 來源文件資料夾 |
| `--orientation portrait\|landscape` | A4 紙張方向 | `portrait` |
| `--scale fit\|50-200` | HTML/PDF 符合頁面或自訂百分比 | `fit` |
| `--margin default\|none\|minimum\|custom` | 邊界模式 | `default` |
| `--margin-top <mm>` | 自訂上邊界 | 無 |
| `--margin-right <mm>` | 自訂右邊界 | 無 |
| `--margin-bottom <mm>` | 自訂下邊界 | 無 |
| `--margin-left <mm>` | 自訂左邊界 | 無 |
| `--font <family>` | Windows 字型家族名稱 | 系統預設 |
| `--toc` | 在文件開頭產生目錄 | 關閉 |
| `--overwrite` | 覆寫現有輸出 | 關閉 |
| `--open` | 完成後使用預設程式開啟輸出 | 關閉 |
| `--quiet` | 隱藏進度與成功輸出 | 關閉 |
| `--json` | 輸出機器可讀 JSON | 關閉 |
| `--list-fonts` | 列出 Windows 已安裝字型 | 無 |
| `--version` | 顯示版本 | 無 |
| `-h, --help` | 顯示命令說明 | 無 |

自訂邊界必須同時提供上、右、下、左四個數值。`--format` 不接受重複格式，且 `all` 不可與其他格式混用。`--help`、`--version` 與 `--list-fonts` 不需要指定格式。

## 使用範例

同時輸出 HTML 與 PDF：

```powershell
mdconvert document.md --format html,pdf
```

建立橫向 PDF、目錄及最小邊界：

```powershell
mdconvert document.md --format pdf --orientation landscape --margin minimum --toc
```

指定微軟正黑體及 90% 縮放：

```powershell
mdconvert document.md --format pdf --font "Microsoft JhengHei" --scale 90
```

使用自訂邊界：

```powershell
mdconvert document.md `
  --format html,pdf,docx `
  --margin custom `
  --margin-top 10 `
  --margin-right 15 `
  --margin-bottom 10 `
  --margin-left 15
```

列出可用字型：

```powershell
mdconvert --list-fonts
```

## JSON 輸出

```powershell
mdconvert document.md --format all --json
```

成功輸出範例：

```json
{
  "success": true,
  "partial": false,
  "htmlPath": "C:\\Documents\\document.html",
  "pdfPath": "C:\\Documents\\document.pdf",
  "docxPath": "C:\\Documents\\document.docx",
  "warnings": [],
  "errors": []
}
```

JSON 模式不輸出進度訊息，適合 PowerShell、批次檔及 CI 使用。

## Exit Code

| Code | 意義 |
|---:|---|
| `0` | 全部成功 |
| `1` | 參數或輸入錯誤 |
| `2` | HTML 產生失敗，保留供未來細分使用 |
| `3` | PDF 或一般轉換失敗 |
| `4` | 部分完成 |
| `5` | 輸出檔案或權限錯誤 |
| `6` | 找不到 Microsoft Edge |
| `130` | 使用者取消 |

CLI 不讀取 GUI 儲存的上次設定，確保相同命令具有固定結果。
