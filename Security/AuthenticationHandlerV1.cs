using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DeviceStatusBeacon.Security;

/// <inheritdoc/>
public class AuthenticationHandlerV1(IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder,
	ISecurityServiceV1 securityService,
	INonceReplayProtectionService nonceReplayProtectionService,
	DeviceStatusBeaconContext dbContext) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder) {

	/// <inheritdoc/>
	protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
		// 尝试从请求头解析出鉴权信息
		var authorizationHeaderValues = Request.Headers.Authorization;
		if (authorizationHeaderValues.Count == 0) {
			return AuthenticateResult.NoResult();
		}

		if (!AuthenticationHeaderV1.TryParse(authorizationHeaderValues: authorizationHeaderValues, out var authHeader)) {
			return AuthenticateResult.Fail("鉴权信息格式不正确，无法解析");
		}

		// 时间戳验证
		if (!ISecurityServiceV1.IsTimestampWithinAllowedDrift(authHeader.Timestamp)) {
			return AuthenticateResult.Fail("鉴权信息时间戳超出允许范围");
		}

		// 解析并查找鉴权实体
		// 使用 AsNoTracking 提高查询性能
		// 即使追踪实体也无法在后续逻辑中 SaveChanges
		// 后续修改实体后调用 Update 方法即可
		IHasProtectedSecretKey? entity = null;
		List<Claim> claims = [
			new(ClaimTypes.NameIdentifier, authHeader.Identity.ToString())
		];

		if (authHeader.Scheme == AuthenticationSchemeV1.Device) {
			// 查询到的设备将被缓存
			var device = await dbContext.Devices.AsNoTracking()
				.SingleOrDefaultAsync(d => d.DeviceId == authHeader.Identity && d.Enabled);

			if (device is null) {
				return AuthenticateResult.Fail("无法找到指定 DeviceId 的设备实体，或找到的设备实体已被禁用");
			}

			entity = device;
			claims.Add(new(ClaimTypes.Role, "Device"));
		} else if (authHeader.Scheme == AuthenticationSchemeV1.ApiCredential) {
			// 查询到的 API 凭据将被缓存
			var apiCredential = await dbContext.ApiCredentials.AsNoTracking()
				.SingleOrDefaultAsync(c => c.ApiCredentialId == authHeader.Identity && c.Enabled);

			if (apiCredential is null) {
				return AuthenticateResult.Fail("无法找到指定 ApiCredentialId 的 API 凭据实体，或找到的 API 凭据实体已被禁用");
			}

			entity = apiCredential;
			claims.Add(new(ClaimTypes.Role, apiCredential.Role.ToString()));
		} else {
			// 代码逻辑上不应到达此处
			throw new InvalidOperationException("不支持的鉴权方案");
		}

		// 签名验证
		var signatureBasis = SignatureBasisV1.FromHttpRequest(Request, authHeader.Timestamp, authHeader.Nonce);
		if (!securityService.VerifySignature(entity, signatureBasis, authHeader.SignatureBase64)) {
			return AuthenticateResult.Fail("鉴权签名验证失败");
		}

		// 在签名正确后再消费 nonce，避免无效请求占用防重放窗口；
		// 一旦 nonce 已经被消费，则说明当前请求极有可能是重放请求
		if (!nonceReplayProtectionService.TryReserve(authHeader)) {
			return AuthenticateResult.Fail("鉴权 nonce 已被使用，当前请求疑似重放");
		}

		// 缓存查询到的实体
		Context.Items[HttpContextAuthenticationExtensions.AuthenticatedEntityItemKey] = entity;

		// 构建认证票据
		var identity = new ClaimsIdentity(claims, Scheme.Name);
		var principal = new ClaimsPrincipal(identity);
		var ticket = new AuthenticationTicket(principal, Scheme.Name);
		return AuthenticateResult.Success(ticket);
	}
}