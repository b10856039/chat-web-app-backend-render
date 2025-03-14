# Side Project 多人聊天應 - 後端伺服器
此專案為透過ASP.NET進行使用者資料、朋友、群組、聊天訊息等資料處理，並搭配PostgreSQL儲存。
再以Restful API形式提供給前端使用。
目前已透過Docker將後端伺服器打包在Render進行上線，可透過以下網址前往前端網頁進行使用。

[多人聊天應用](https://chat-web-app-vercel.vercel.app/ "link")

[前端伺服器Github](https://github.com/b10856039/chat-web-app-vercel "link")

### 開發環境
 * 作業系統 Window 11
 * 開發工具 Visual Studio Code 1.97.2
 * 環境框架 Dotnet 9.0.101

### 系統語言
 * C#
 * ASP.NET

### 系統套件
 *  DotNetEnv 3.1.1
 *  Microsoft.AspNetCore.SignalR 1.2.0
 *  Microsoft.EntityFrameworkCore.Design 9.0.0
 *  Npgsql.EntityFrameworkCore.PostgreSQL 9.0.3
 *  Microsoft.EntityFrameworkCore.Sqlite 9.0.1
 *  MinimalApis.Extensions  0.11.0
 *  System.IdentityModel.Tokens.Jwt 8.3.1

## 建置流程
1. 使用 git clone 或是 直接下載github的檔案。
2. 檢查 .NET 版本 確保已安裝 .Net SDK。
  ``` XML
  dotnet --version
  ```
3. 安裝 NuGet 套件。
  ``` XML
  dotnet restore
  ```
4.設定資料庫，執行資料庫遷移。
  ``` XML
  dotnet ef database update
  ```
4.1 若上方指令無效，請安裝dotnet-ef工具。
  ``` XML
  dotnet tool install --global dotnet-ef
  ```
5.編譯程式。
  ``` XML
  dotnet build
  ```
6.啟動應用程式。
  ``` XML
  dotnet run
  ```
## Web API服務使用範例

### API 基本網址
API的請求都需使用以下的基本URL:
``` XML
  http伺服器位址/api/
```

### 使用方式
---

## **帳號驗證 (Auth)**
**URL路徑: `/api/auth`**

| Method | Endpoint      | Description       | Request Body |
|--------|-------------|-------------------|--------------|
| `POST` | `/register` | 註冊新使用者 | `CreateUserDTO` |
| `POST` | `/login` | 使用者登入 | `LoginAuthUserDTO` |

---

## **聊天室**
**URL路徑: `/api/chatroom`**

| Method | Endpoint | Description | Request Body / Query Params |
|--------|----------|-------------|------------------------------|
| `GET`  | `/` | 取得聊天室列表 | `userId` (required), `roomtype` (optional), `hasjoin` (optional) |
| `GET`  | `/{id}` | 取得特定聊天室 | `id` (聊天室 ID), `userId` (required) |
| `POST` | `/` | 建立新聊天室 | `CreateChatroomDTO` |
| `POST` | `/{id}/join` | 加入聊天室 | `id` (聊天室 ID), `JoinChatroomDTO` |
| `PATCH` | `/{id}` | 更新聊天室資訊 | `id` (聊天室 ID), `UpdatePatchChatroom` |
| `DELETE` | `/{id}` | 刪除聊天室 (軟刪除) | `id` (聊天室 ID), `userId` (required) |
| `DELETE` | `/{id}/leave` | 離開聊天室 | `id` (聊天室 ID), `LeaveChatroomDTO` |

---

## **朋友關係**
**URL路徑: `/api/friendships`**

| Method | Endpoint | Description | Request Body / Query Params |
|--------|----------|-------------|------------------------------|
| `GET`  | `/{userId}` | 取得好友列表 | `userId` (required) |
| `GET`  | `/non-friends/{userId}` | 取得非好友列表 (可選搜尋) | `userId` (required), `search` (optional) |
| `POST` | `/request` | 送出好友邀請 | `SendFriendShipRequestDTO` |
| `POST` | `/respond` | 回應好友邀請 | `RespoendTpFriendRequestDTO` |
| `PATCH` | `/` | 更新好友狀態 | `FriendStatusUpdateDTO` |

---

## **訊息**
**URL路徑: `/api/message`**

| Method | Endpoint | Description | Request Body / Query Params |
|--------|----------|-------------|------------------------------|
| `GET`  | `/` | 取得聊天歷史訊息 | `userId` (required), `chatroomId` (required), `latestOne` (optional) |

---

## **使用者**
**URL路徑: `/api/user`**

| Method | Endpoint | Description | Request Body / Query Params |
|--------|----------|-------------|------------------------------|
| `GET`  | `/` | 取得所有使用者 (可選關鍵字查詢) | `query` (optional) |
| `GET`  | `/{id}` | 取得特定使用者 | `id` (使用者 ID) |
| `GET`  | `/username/{username}` | 透過使用者名稱取得使用者 | `username` (required) |
| `PUT`  | `/{id}` | 更新使用者資訊 (完整更新) | `id` (使用者 ID), `UpdatePutUserDTO` |
| `PATCH` | `/{id}` | 更新使用者資訊 (部分更新) | `id` (使用者 ID), `UpdatePatchUserDTO` |
| `DELETE` | `/{id}` | 刪除使用者 (軟刪除) | `id` (使用者 ID) |

---

### **DTO 說明**
以下 DTO 定義了請求API時所需的Formbody資料格式，可前往/DTO查看。

1. `CreateUserDTO` - 用於註冊新使用者
2. `LoginAuthUserDTO` - 用於登入
3. `CreateChatroomDTO` - 用於創建聊天室
4. `JoinChatroomDTO` - 用於加入聊天室
5. `UpdatePatchChatroom` - 用於部分更新聊天室資訊
6. `LeaveChatroomDTO` - 用於離開聊天室
7. `SendFriendShipRequestDTO` - 用於發送好友邀請
8. `RespoendTpFriendRequestDTO` - 用於回應好友邀請
9. `FriendStatusUpdateDTO` - 用於更新好友狀態
10. `UpdatePutUserDTO` - 用於完整更新使用者資訊
11. `UpdatePatchUserDTO` - 用於部分更新使用者資訊

---

