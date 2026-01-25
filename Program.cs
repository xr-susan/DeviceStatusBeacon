var builder = WebApplication.CreateBuilder(args);

// 尝试直接获取自定义连接字符串
var dbConnectionString = builder.Configuration["Database:CustomConnectionString"];

if (string.IsNullOrWhiteSpace(dbConnectionString)) {
	// 尝试从配置中获取数据库目录
	var dbDirectory = builder.Configuration["Database:Directory"];
	if (string.IsNullOrWhiteSpace(dbDirectory)) {
		if (builder.Configuration.GetValue("Database:UsingSubDirectory", false)) {
			dbDirectory = Path.Join(AppContext.BaseDirectory, "data");
		} else {
			// 尝试使用 LocalApplicationData/DeviceStatusBeacon 目录
			var baseDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var subDirectoryName = "DeviceStatusBeacon";
			if (string.IsNullOrWhiteSpace(baseDataPath)) {
				// 尝试使用 UserProfile/.device_status_beacon 目录
				baseDataPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				subDirectoryName = ".device_status_beacon";
				if (string.IsNullOrWhiteSpace(baseDataPath)) {
					// 使用应用程序基目录下的 data 目录
					baseDataPath = AppContext.BaseDirectory;
					subDirectoryName = "data";
				}
			}
			dbDirectory = Path.Join(baseDataPath, subDirectoryName);
		}
	}

	try {
		Directory.CreateDirectory(dbDirectory);
	} catch (Exception e) {
		Console.Error.WriteLine($"Cannot create directory {dbDirectory} of database, exception: {e}");
	}

	dbConnectionString = new SqliteConnectionStringBuilder {
		DataSource = Path.Join(dbDirectory, builder.Configuration["Database:FileName"] ?? "beacon.db")
	}.ToString();
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