using DeviceStatusBeacon.Handlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

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

// 配置 Identity
builder.Services.AddIdentityCore<User>(options => {
	options.User.RequireUniqueEmail = false;
	options.Password.RequireDigit = true;
	options.Password.RequireLowercase = true;
	options.Password.RequireUppercase = true;
	options.Password.RequireNonAlphanumeric = true;
	options.Password.RequiredLength = 12;
	options.Password.RequiredUniqueChars = 8;
})
	.AddRoles<IdentityRole<Guid>>()
	.AddSignInManager()
	.AddEntityFrameworkStores<DeviceStatusBeaconContext>()
	.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options => {
	options.LoginPath = "/login";
	// TODO: 后续补充 AccessDenied 的同类分流逻辑：
	// API 请求应返回 403，浏览器请求则可重定向到专门的拒绝访问页面。
	options.Events.OnRedirectToLogin = context => {
		if (ShouldRespondWithUnauthorized(context.Request)) {
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			return Task.CompletedTask;
		}

		context.Response.Redirect(context.RedirectUri);
		return Task.CompletedTask;
	};
});

// 配置身份验证
var authenticationBuilder = builder.Services.AddAuthentication(options => {
	options.DefaultScheme = "BeaconOrIdentity";
	options.DefaultAuthenticateScheme = "BeaconOrIdentity";
	options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
	.AddPolicyScheme("BeaconOrIdentity", null, options => options.ForwardDefaultSelector = context =>
			context.Request.Headers.Authorization.Count > 0 ? "BeaconAuthV1" : IdentityConstants.ApplicationScheme)
	.AddScheme<AuthenticationSchemeOptions, AuthenticationHandlerV1>("BeaconAuthV1", null);

authenticationBuilder.AddIdentityCookies();

// 配置授权
builder.Services.AddAuthorizationBuilder()
	.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(PrincipalRole.Administrator)))
	.AddPolicy("QueryAccess", policy => policy.RequireRole(
		nameof(PrincipalRole.LimitedQuery),
		nameof(PrincipalRole.FullQuery),
		nameof(PrincipalRole.DeviceManager),
		nameof(PrincipalRole.Administrator)))
	.AddPolicy("LogSubmission", policy => policy.RequireRole(nameof(PrincipalRole.Administrator), nameof(PrincipalRole.DeviceManager), "Device"));

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

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");
app.MapGet("/login", () => Results.Content("""
<!DOCTYPE html>
<html lang="zh-CN">
<head>
	<meta charset="utf-8">
	<title>登录</title>
</head>
<body>
	<p>登录页尚未实现。</p>
</body>
</html>
""", "text/html; charset=utf-8"));

app.Run();

return 0;

static bool ShouldRespondWithUnauthorized(HttpRequest request) {
	var acceptHeader = request.Headers.Accept.ToString();
	var acceptsJson = acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase);
	var acceptsHtml = acceptHeader.Contains("text/html", StringComparison.OrdinalIgnoreCase);
	return PathInvolvesApi(request.Path) || (acceptsJson && !acceptsHtml);
}

static bool PathInvolvesApi(PathString path) =>
	path.HasValue
	&& path.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries)
		.Any(segment => segment.Equals("api", StringComparison.OrdinalIgnoreCase));
