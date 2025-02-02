# 使用官方 .NET Core SDK 建立應用程式
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# 複製專案檔案並還原相依性
COPY . ./
RUN dotnet restore

# 建立應用程式
RUN dotnet publish -c Release -o out

# 使用輕量級 ASP.NET Core Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# 設定應用程式執行方式
CMD ["dotnet", "ChatAPI.dll"]