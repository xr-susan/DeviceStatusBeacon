using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// 传递 HTTPS 默认证书配置
builder.Configuration["Kestrel:Certificates:Default:Path"] ??= builder.Configuration["Https:DefaultCertificate:Path"];
builder.Configuration["Kestrel:Certificates:Default:KeyPath"] ??= builder.Configuration["Https:DefaultCertificate:KeyPath"];

// 配置数据库上下文和数据保护 API
try {
	builder.Services
		.AddDbContextsWithConfiguration(builder.Configuration, out var dbDirectoryInfo)
		.AddDataProtectionWithConfiguration(builder.Configuration, dbDirectoryInfo);
} catch (InvalidOperationException e) {
	Console.Error.WriteLine(e.Message);
	return -1;
} catch (IOException e) {
	Console.Error.WriteLine("Failed to create necessary directories for database or data protection: " + e.Message);
	return -1;
}

// 注册自定义服务
builder.Services.AddCustomServices();

// 配置身份验证
builder.Services
	.AddAuthentication("BeaconAuthV1")
	.AddScheme<AuthenticationSchemeOptions, AuthenticationHandlerV1>("BeaconAuthV1", null);

// 配置授权
builder.Services.AddAuthorizationBuilder()
	.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(AccountRole.Administrator)))
	.AddPolicy("QueryAccess", policy => policy.RequireAuthenticatedUser())
	.AddPolicy("LogSubmission", policy => policy.RequireRole(nameof(AccountRole.Administrator), "Device"));

var app = builder.Build();

try {
	var (shouldExit, exitCode) = await ConsoleDispatcher.DispatchAsync(args, app.Services);
	if (shouldExit) {
		return exitCode;
	}
} catch (Exception e) {
	Console.Error.WriteLine("Failed to dispatch console command: " + e.Message);
	return -1;
}

if (app.Environment.IsDevelopment()) {

}

app.MapGet("/", () => "Hello World!");

app.Run();

return 0;