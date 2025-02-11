using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatAPI.Data;

public class ChatAPIContextFactory : IDesignTimeDbContextFactory<ChatAPIContext>
{
    public ChatAPIContext CreateDbContext(string[] args)
    {
        // 設定資料庫連線PostgreSQL (DEV)
        DotNetEnv.Env.Load();
        // 讀取環境變數中的資料庫連線字串（在Render上通常會設置這些環境變數）
        var connectionString = Environment.GetEnvironmentVariable("CHATSTORE_CONNECTION_STRING");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string is not set in environment variables.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ChatAPIContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ChatAPIContext(optionsBuilder.Options);

        // 設定資料庫連線Sqlite (DEV)
        // var configuration = new ConfigurationBuilder()
        //     .SetBasePath(Directory.GetCurrentDirectory())
        //     .AddJsonFile("appsettings.json")
        //     .Build();

        // var optionsBuilder = new DbContextOptionsBuilder<ChatAPIContext>();
        // optionsBuilder.UseSqlite(configuration.GetConnectionString("ChatAPI"));


        // return new ChatAPIContext(optionsBuilder.Options);
    }
}
