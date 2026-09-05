# 貢獻指南

感謝協助改善 WinMdConverter。提交程式碼前，請先確認變更符合 Windows 桌面工具簡單、離線及輸出一致的產品方向。

## 回報問題

- 先搜尋現有 Issue，避免重複回報。
- 清楚描述預期行為與實際行為。
- 提供 Windows、Edge 與 WinMdConverter 版本。
- 若問題與 Markdown 有關，提供可重現的最小範例。
- 請先移除文件中的個人資料及敏感內容。

## 開發流程

1. Fork repository 並建立功能分支。
2. 保持變更範圍小且目的明確。
3. 為行為變更新增或更新測試。
4. 執行 `dotnet build WinMdConverter.sln`。
5. 執行 `dotnet test WinMdConverter.sln`。
6. 建立 Pull Request，說明變更原因與驗證方式。

## 程式碼原則

- 使用 C# Nullable Reference Types。
- 不忽略編譯警告，專案將警告視為錯誤。
- GUI 與 CLI 的轉換行為應放在 Core 共用層。
- 不加入需要上傳文件的服務。
- 不提交密碼、Token、簽章憑證或個人文件。
- 新增使用者可見文字時使用繁體中文。

提交貢獻即表示您同意以本專案的 MIT License 授權該貢獻。
