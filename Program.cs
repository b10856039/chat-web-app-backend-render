using ChatAPI.Data;
using static ChatAPI.Extensions.SignalRHandler;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using ChatAPI.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);




// 添加 CORS 服務
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "https://chat-web-app-vercel.vercel.app",
            "https://chat-web-app-vercel-ptik9j4yt-changshengs-projects.vercel.app",
            "https://chat-web-app-vercel-git-main-changshengs-projects.vercel.app"
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});


// 設定資料庫連線字串 (PostgreSQL DEV)
// 從環境變數讀取資料庫連線字串
// DotNetEnv.Env.Load();
// var connstring = builder.Configuration.GetConnectionString("CHATSTORE_CONNECTION_STRING");
// Console.WriteLine("Connection string: " + connstring);

// online version
var connstring = Environment.GetEnvironmentVariable("CHATSTORE_CONNECTION_STRING");

Console.WriteLine("Connection string: " + connstring);
if (string.IsNullOrEmpty(connstring))
{
    throw new InvalidOperationException("Connection string is not set in environment variables.");
}

// var connstring = builder.Configuration.GetConnectionString("ChatStore");

// 註冊 Sqlite 資料庫服務 (DEV)
// builder.Services.AddSqlite<ChatAPIContext>(connstring);


// 註冊 PostgreSQL 資料庫服務
builder.Services.AddDbContext<ChatAPIContext>(options =>
    options.UseNpgsql(connstring)
);

// 註冊 MVC 服務，包括控制器
builder.Services.AddControllers();

// 註冊 SignalR 服務
builder.Services.AddSignalR();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://chat-web-app-backend-render.onrender.com", //後端 API

            ValidateAudience = true,
            ValidAudience = "https://chat-web-app-vercel.vercel.app", // 前端

            ValidateLifetime = true, // 確保 Token 未過期
            ValidateIssuerSigningKey = true, // 確保 Token 簽名正確
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("s3cr3t_k3y_!@#_$tr0ng_AND_R@nd0m")) // 密鑰
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 設定路由
app.UseRouting();  // 確保 SignalR 和控制器都能正確處理請求


// OPTIONS 請求檢查 (預檢 403/404)
app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Options)
    {
        context.Response.Headers.Append("Access-Control-Allow-Origin", context.Request.Headers["Origin"]);
        context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
        context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
    }
    else
    {
        await next();
    }
});

// 設置 CORS 策略
// 在 UseRouting 之前設置 CORS 策略
app.UseCors("AllowFrontend");  

app.UseAuthentication();
app.UseAuthorization();

// 使用 MVC 路由
app.MapControllers();

// 設置 SignalR Hub 路由
app.MapHub<ChatHub>("/api/chatHub");

// 使用錯誤處理MiddleWare
app.UseMiddleware<ExceptionMiddleware>(); // 註冊全域錯誤處理

// 如果有需要，可以在此執行資料庫遷移
await app.MigrateDBAsync();

// 啟動應用
app.Run();


