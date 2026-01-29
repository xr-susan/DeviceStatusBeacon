var builder = WebApplication.CreateBuilder(args);

// 传递 HTTPS 默认证书配置
builder.Configuration["Kestrel:Certificates:Default:Path"] ??= builder.Configuration["Https:DefaultCertificate:Path"];
builder.Configuration["Kestrel:Certificates:Default:KeyPath"] ??= builder.Configuration["Https:DefaultCertificate:KeyPath"];

// 获取或构建数据库连接字符串并配置 DbContext
var (dbConnectionString, dbDirectoryInfo) = DeviceStatusBeaconContext.GetOrBuildConnectionStringFromConfiguration(builder.Configuration);
try {
	dbDirectoryInfo?.Create();
} catch (Exception e) {
	Console.WriteLine($"Failed to create database directory '{dbDirectoryInfo?.FullName}': {e.Message}");
	return -1;
}
builder.Services.AddDbContext<DeviceStatusBeaconContext>(options => options.UseSqlite(dbConnectionString));

// 配置数据保护 API
var dpBuilder = builder.Services.AddDataProtection();
if (builder.Configuration.GetValue("DataProtection:OverrideDiscovery", true)) {
	var dpKeysDirectoryParent = builder.Configuration["DataProtection:KeysDirectoryParent"];
	var dpKeysDirectoryParentInfo = string.IsNullOrWhiteSpace(dpKeysDirectoryParent) ? dbDirectoryInfo : new DirectoryInfo(dpKeysDirectoryParent);

	// 确保数据保护密钥目录的确被指定或成功使用了数据库目录
	if (dpKeysDirectoryParentInfo is null) {
		Console.Error.WriteLine("Data protection keys directory parent is not specified and database directory is also unavailable.");
		return -1;
	}

	try {
		var dpKeysDirectoryInfo = dpKeysDirectoryParentInfo.CreateSubdirectory("DataProtection-Keys");
		dpBuilder.PersistKeysToFileSystem(dpKeysDirectoryInfo);
	} catch (Exception e) {
		Console.Error.WriteLine($"Failed to create data protection keys directory '{Path.Join(dpKeysDirectoryParentInfo.FullName, "DataProtection-Keys")}': {e.Message}");
		return -1;
	}
}

var app = builder.Build();

if (app.Environment.IsDevelopment()) {

}

app.Run();

return 0;