var builder = WebApplication.CreateBuilder(args);

// 获取或构建数据库连接字符串并配置 DbContext
var (dbConnectionString, dbDirectoryInfo) = DeviceStatusBeaconContext.GetOrBuildConnectionStringFromConfiguration(builder.Configuration);
try {
	dbDirectoryInfo?.Create();
} catch (Exception e) {
	Console.WriteLine($"Failed to create database directory '{dbDirectoryInfo?.FullName}': {e.Message}");
	return -1;
}
Console.WriteLine($"Using Connection String: {dbConnectionString}\n");
builder.Services.AddDbContext<DeviceStatusBeaconContext>(options => options.UseSqlite(dbConnectionString));

// 传递 HTTPS 默认证书配置
builder.Configuration["Kestrel:Certificates:Default:Path"] ??= builder.Configuration["Https:DefaultCertificate:Path"];
builder.Configuration["Kestrel:Certificates:Default:KeyPath"] ??= builder.Configuration["Https:DefaultCertificate:KeyPath"];

var app = builder.Build();

if (app.Environment.IsDevelopment()) {

}

app.Run();

return 0;