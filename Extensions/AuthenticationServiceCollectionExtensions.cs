using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace DeviceStatusBeacon.Extensions;

/// <summary>
/// 认证与授权配置扩展方法。
/// </summary>
public static class AuthenticationServiceCollectionExtensions {
	/// <summary>
	/// 为 <see cref="IServiceCollection"/> 提供认证与授权配置相关的扩展方法组
	/// </summary>
	/// <param name="services">将要写入认证与授权配置的 <see cref="IServiceCollection"/></param>
	extension(IServiceCollection services) {
		/// <summary>
		/// 配置交互式用户认证、签名式 API 认证和对应授权策略。
		/// </summary>
		/// <returns>当前服务集合</returns>
		public IServiceCollection AddApplicationAuthenticationAndAuthorization() {
			// 配置交互式后台用户所需的 Identity 核心服务和用户存储
			services.AddIdentityCore<User>(options => {
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

			// 配置默认认证入口
			// 浏览器页面请求优先走 Identity Cookie
			// 带 Authorization 头的程序化请求则转发到自定义签名认证方案
			var authenticationBuilder = services.AddAuthentication(options => {
				options.DefaultScheme = AuthenticationSchemeNames.Hybrid;
				options.DefaultAuthenticateScheme = AuthenticationSchemeNames.Hybrid;
				// 认证挑战默认交回 Identity Cookie，这样页面未登录时才能进入登录跳转分流
				options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
			})
				.AddPolicyScheme(AuthenticationSchemeNames.Hybrid, null, options => options.ForwardDefaultSelector = context =>
					context.Request.Headers.Authorization.Count > 0
						? AuthenticationSchemeNames.Signature
						: IdentityConstants.ApplicationScheme)
				.AddScheme<AuthenticationSchemeOptions, AuthenticationHandlerV1>(AuthenticationSchemeNames.Signature, null);

			// 挂接 Identity Cookie，并收紧安全戳校验间隔，避免角色或账户状态变更长期滞后
			authenticationBuilder.AddIdentityCookies();
			services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(5));

			// 统一 Cookie 登录跳转和拒绝访问行为，使页面请求与 API 请求保持分流
			services.ConfigureApplicationCookie(options => {
				options.LoginPath = "/login";
				options.Events.OnRedirectToLogin = context => {
					// 只有显式接受 HTML 的交互式页面请求才允许跳去登录页
					if (context.Request.GetFailureResponseMode() is FailureResponseMode.ExplicitHtml) {
						context.Response.Redirect(context.RedirectUri);
						return Task.CompletedTask;
					}

					context.Response.StatusCode = StatusCodes.Status401Unauthorized;
					return Task.CompletedTask;
				};

				options.Events.OnRedirectToAccessDenied = context => {
					// 403 不跳转，交给后续状态码处理流程统一输出
					context.Response.StatusCode = StatusCodes.Status403Forbidden;
					return Task.CompletedTask;
				};
			});

			// 显式拆分交互式用户、签名式 ApiCredential、设备主体，以及混合可访问的 API 策略
			// 查询页本身只要求交互式登录，具体可见数据范围继续由服务层按角色和授权关系过滤
			services.AddAuthorizationBuilder()
				// 任意已登录后台用户都可进入受保护页面的基础层
				.AddPolicy(AuthorizationPolicyNames.InteractiveUser, policy => policy
					.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme)
					.RequireAuthenticatedUser())
				// 仅允许交互式后台管理员进入管理员页面
				.AddPolicy(AuthorizationPolicyNames.InteractiveAdminOnly, policy => policy
					.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme)
					.RequireRole(nameof(PrincipalRole.Administrator)))
				// 仅允许交互式设备管理员和管理员进入设备管理页面
				.AddPolicy(AuthorizationPolicyNames.InteractiveDeviceManagement, policy => policy
					.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme)
					.RequireRole(
						nameof(PrincipalRole.DeviceManager),
						nameof(PrincipalRole.Administrator)))
				// 仅允许签名式 ApiCredential 以查询身份访问相应 API
				// NameIdentifier 在这里用于排除未绑定具体凭据标识的异常主体
				.AddPolicy(AuthorizationPolicyNames.ApiCredentialQueryAccess, policy => policy
					.AddAuthenticationSchemes(AuthenticationSchemeNames.Signature)
					.RequireClaim(ClaimTypes.NameIdentifier)
					.RequireRole(
						nameof(PrincipalRole.LimitedQuery),
						nameof(PrincipalRole.FullQuery),
						nameof(PrincipalRole.DeviceManager),
						nameof(PrincipalRole.Administrator)))
				// 仅允许签名式管理员凭据访问管理类 API
				.AddPolicy(AuthorizationPolicyNames.ApiCredentialAdminOnly, policy => policy
					.AddAuthenticationSchemes(AuthenticationSchemeNames.Signature)
					.RequireClaim(ClaimTypes.NameIdentifier)
					.RequireRole(nameof(PrincipalRole.Administrator)))
				// 允许交互式用户和签名式 ApiCredential 共享查询类 API
				.AddPolicy(AuthorizationPolicyNames.UserOrApiCredentialQueryAccess, policy => policy
					.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, AuthenticationSchemeNames.Signature)
					.RequireRole(
						nameof(PrincipalRole.LimitedQuery),
						nameof(PrincipalRole.FullQuery),
						nameof(PrincipalRole.DeviceManager),
						nameof(PrincipalRole.Administrator)))
				// 允许交互式管理员用户和签名式管理员凭据共享管理员 API
				.AddPolicy(AuthorizationPolicyNames.UserOrApiCredentialAdminOnly, policy => policy
					.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, AuthenticationSchemeNames.Signature)
					.RequireRole(nameof(PrincipalRole.Administrator)))
				// 允许交互式设备管理员 / 管理员与对应的签名式凭据共享设备管理 API
				.AddPolicy(AuthorizationPolicyNames.UserOrApiCredentialDeviceManagement, policy => policy
					.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, AuthenticationSchemeNames.Signature)
					.RequireRole(
						nameof(PrincipalRole.DeviceManager),
						nameof(PrincipalRole.Administrator)))
				// 统一日志提交入口，不区分“设备主体直接提交”和“高权限主体代提交”两类场景。
				// 只要主体满足管理员、设备管理员或 Device 角色之一，即可进入日志写入流程。
				.AddPolicy(AuthorizationPolicyNames.LogSubmission, policy => policy
					.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, AuthenticationSchemeNames.Signature)
					.RequireRole(
						nameof(PrincipalRole.Administrator),
						nameof(PrincipalRole.DeviceManager),
						"Device"));

			return services;
		}
	}
}

/// <summary>
/// 认证方案名称。
/// </summary>
public static class AuthenticationSchemeNames {
	/// <summary>
	/// 应用默认混合认证入口。
	/// </summary>
	public const string Hybrid = "BeaconOrIdentity";

	/// <summary>
	/// 自定义签名认证方案。
	/// </summary>
	public const string Signature = "BeaconAuthV1";
}

/// <summary>
/// 授权策略名称。
/// </summary>
public static class AuthorizationPolicyNames {
	/// <summary>
	/// 任意交互式后台用户。
	/// </summary>
	public const string InteractiveUser = "InteractiveUser";

	/// <summary>
	/// 仅交互式后台管理员。
	/// </summary>
	public const string InteractiveAdminOnly = "InteractiveAdminOnly";

	/// <summary>
	/// 仅交互式设备管理员和管理员。
	/// </summary>
	public const string InteractiveDeviceManagement = "InteractiveDeviceManagement";

	/// <summary>
	/// 仅签名式 ApiCredential 查询主体。
	/// </summary>
	public const string ApiCredentialQueryAccess = "ApiCredentialQueryAccess";

	/// <summary>
	/// 仅签名式 ApiCredential 管理主体。
	/// </summary>
	public const string ApiCredentialAdminOnly = "ApiCredentialAdminOnly";

	/// <summary>
	/// 允许 User 或 ApiCredential 的查询策略。
	/// </summary>
	public const string UserOrApiCredentialQueryAccess = "UserOrApiCredentialQueryAccess";

	/// <summary>
	/// 允许 User 或 ApiCredential 的管理员策略。
	/// </summary>
	public const string UserOrApiCredentialAdminOnly = "UserOrApiCredentialAdminOnly";

	/// <summary>
	/// 允许 User 或 ApiCredential 的设备管理策略。
	/// </summary>
	public const string UserOrApiCredentialDeviceManagement = "UserOrApiCredentialDeviceManagement";

	/// <summary>
	/// 允许设备或高权限主体提交日志。
	/// </summary>
	public const string LogSubmission = "LogSubmission";
}