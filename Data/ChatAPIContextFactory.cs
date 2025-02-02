using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatAPI.Data;

public class ChatAPIContextFactory : IDesignTimeDbContextFactory<ChatAPIContext>
{
    public ChatAPIContext CreateDbContext(string[] args)
    {
        // 設定資料庫連線
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ChatAPIContext>();
        optionsBuilder.UseSqlite(configuration.GetConnectionString("ChatAPI"));


        return new ChatAPIContext(optionsBuilder.Options);
    }
}
