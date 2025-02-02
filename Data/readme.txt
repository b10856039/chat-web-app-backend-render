terminal遷移資料庫基本流程:

dotnet ef migrations add InitialCreated(資料名稱) --output-dir Data\Migrations(存放位置)

dotnet ef database update

dotnet ef migrations remove