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
builder.Services.AddRazorPages(options => options.Conventions.AuthorizeFolder("/"));

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
	.AddClaimsPrincipalFactory<DisplayNameUserClaimsPrincipalFactory>()
	.AddSignInManager()
	.AddEntityFrameworkStores<DeviceStatusBeaconContext>()
	.AddDefaultTokenProviders();

// 配置身份验证
var authenticationBuilder = builder.Services.AddAuthentication(options => {
	options.DefaultScheme = "BeaconOrIdentity";
	options.DefaultAuthenticateScheme = "BeaconOrIdentity";
	options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
	.AddPolicyScheme("BeaconOrIdentity", null, options => options.ForwardDefaultSelector = context =>
			context.Request.Headers.Authorization.Count > 0 ? "BeaconAuthV1" : IdentityConstants.ApplicationScheme)
	.AddScheme<AuthenticationSchemeOptions, AuthenticationHandlerV1>("BeaconAuthV1", null);

// 配置安全戳验证和应用 Cookie 行为
authenticationBuilder.AddIdentityCookies();
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(5));

builder.Services.ConfigureApplicationCookie(options => {
	options.LoginPath = "/login";
	options.Events.OnRedirectToLogin = context => {
		// 只有站点页面请求才允许走浏览器登录跳转；API 和非 HTML 请求都应诚实返回 401
		if (context.Request.GetErrorResponseMode() is ErrorResponseMode.Html) {
			context.Response.Redirect(context.RedirectUri);
			return Task.CompletedTask;
		}

		context.Response.StatusCode = StatusCodes.Status401Unauthorized;
		return Task.CompletedTask;
	};

	options.Events.OnRedirectToAccessDenied = context => {
		// 403 不做跳转，统一交给后续状态码处理中间件与 Razor Page 输出
		context.Response.StatusCode = StatusCodes.Status403Forbidden;
		return Task.CompletedTask;
	};
});

// 配置授权
// "Device" 角色由 AuthenticationHandlerV1 直接颁发，有且仅有代表设备本身提交新日志的能力
builder.Services.AddAuthorizationBuilder()
	.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(PrincipalRole.Administrator)))
	.AddPolicy("DeviceManagement", policy => policy.RequireRole(
		nameof(PrincipalRole.DeviceManager),
		nameof(PrincipalRole.Administrator)))
	.AddPolicy("QueryAccess", policy => policy.RequireRole(
		nameof(PrincipalRole.LimitedQuery),
		nameof(PrincipalRole.FullQuery),
		nameof(PrincipalRole.DeviceManager),
		nameof(PrincipalRole.Administrator)))
	.AddPolicy("LogSubmission", policy => policy.RequireRole(
		nameof(PrincipalRole.Administrator),
		nameof(PrincipalRole.DeviceManager),
		"Device"));

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

// 异常和空响应状态码交给  Error / StatusCode 页面处理
app.UseExceptionHandler("/error");
app.UseStatusCodePagesWithReExecute("/status-code/{0}");

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();

return 0;