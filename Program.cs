using DeviceStatusBeacon.Api;
using DeviceStatusBeacon.Json;
using DeviceStatusBeacon.Routing;

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
try {
	builder.Services.AddReverseProxyForwardedHeaders(builder.Configuration);
} catch (InvalidOperationException e) {
	Console.Error.WriteLine(e.Message);
	return -1;
} catch (FormatException e) {
	Console.Error.WriteLine(e.Message);
	return -1;
}

// 配置路由约束
builder.Services.Configure<RouteOptions>(options =>
	options.ConstraintMap["identityName"] = typeof(IdentityNameRouteConstraint));

// 注册 Razor Pages、认证与授权、验证器、JSON 序列化器和问题详情处理
builder.Services.AddRazorPages(options => options.Conventions.AuthorizeFolder("/", AuthorizationPolicyNames.InteractiveUser));
builder.Services.AddApplicationAuthenticationAndAuthorization();
builder.Services.AddValidation();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new IPAddressJsonConverter()));
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
	context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);

var app = builder.Build();

try {
	await app.Services.MigrateDatabaseAsync();
} catch (Exception e) {
	Console.Error.WriteLine("Failed to apply database migrations: " + e.Message);
	return -1;
}

try {
	var (shouldExit, exitCode) = await ConsoleDispatcher.DispatchAsync(args, app.Services);
	if (shouldExit) {
		return exitCode;
	}
} catch (Exception e) {
	Console.Error.WriteLine("Failed to dispatch console command: " + e.Message);
	return -1;
}

app.UseReverseProxyForwardedHeaders();

// 异常和空响应状态码交给  Error / StatusCode 页面处理
app.UseExceptionHandler("/error");
app.UseStatusCodePagesWithReExecute("/status-code/{0}");

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapApplicationApiEndpoints();
app.MapRazorPages();

app.Run();

return 0;