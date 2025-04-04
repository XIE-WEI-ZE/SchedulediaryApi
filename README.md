# SchedulediaryApi 🗓️  
**行事曆與待辦清單後端系統**  
_A simple but powerful calendar & to-do RESTful API built with ASP.NET Core._

---

## 📌 專案介紹 | Project Introduction

SchedulediaryApi 是一個使用 ASP.NET Core 建置的後端 API，搭配 Angular 前端，可實現個人化的行事曆與待辦事項管理系統，支援第三方登入、JWT 驗證與每日完成率統計功能，適合做為全端練習或履歷作品集展示。

SchedulediaryApi is a RESTful backend API built with ASP.NET Core. It powers a personalized scheduling and task management system, featuring user authentication (JWT & Google OAuth), CRUD operations, and task completion statistics. Ideal for fullstack practice or resume portfolio.

---

## 🔧 使用技術 | Technologies Used

- ASP.NET Core Web API (.NET 6)
- ADO.NET + SQL Server
- JWT 驗證（Token-based Authentication）
- Google 第三方登入（OAuth 2.0）
- 密碼加密：BCrypt
- 前端搭配：Angular (另有獨立 repo)

---

## 💡 功能特色 | Core Features

- ✅ 使用者註冊 / 登入 / 登出（帳號密碼 + Google 登入）
- ✅ 新增 / 編輯 / 刪除 / 查詢 待辦事項
- ✅ 完成狀態切換（已完成 / 未完成）
- ✅ 標籤分類、關鍵字模糊搜尋
- ✅ 月曆事件 API（顯示當月事項、點擊查詢）
- ✅ 每日完成率統計回傳（完成 / 全部）
- ✅ JWT Token 保護路由（需登入才能操作）

---

## 🚀 使用說明 | Getting Started

### 📦 安裝依賴套件（Install dependencies）

請使用 Visual Studio 開啟專案，確保有連接 SQL Server，並修改 `appsettings.json` 內的連線字串設定：

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=SchedulediaryDB;User Id=sa;Password=你的密碼;"
}
