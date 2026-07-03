using System.Security.Claims;

namespace DeviceStatusBeacon.Extensions;

/// <summary>
/// 为角色值和当前登录主体提供权限相关的扩展方法。
/// </summary>
public static class PrincipalExtensions {
	/// <summary>
	/// 为 <see cref="PrincipalRole"/> 值提供权限语义判断相关的扩展方法组
	/// </summary>
	/// <param name="role">当前要判定能力边界的角色值</param>
	extension(PrincipalRole? role) {
		/// <summary>
		/// 判断当前角色是否具备设备及日志读取能力。
		/// </summary>
		/// <returns>如果具备任意设备数据读取能力，则返回 true；否则返回 false</returns>
		public bool CanQueryAnyDevices() =>
			role is PrincipalRole.LimitedQuery or PrincipalRole.FullQuery or PrincipalRole.DeviceManager or PrincipalRole.Administrator;

		/// <summary>
		/// 判断当前角色是否具备全部设备及日志读取能力。
		/// </summary>
		/// <returns>如果具备全部设备数据读取能力，则返回 true；否则返回 false</returns>
		public bool CanQueryAllDevices() =>
			role is PrincipalRole.FullQuery or PrincipalRole.DeviceManager or PrincipalRole.Administrator;

		/// <summary>
		/// 判断当前角色是否具备设备管理能力。
		/// </summary>
		/// <returns>如果具备设备管理能力，则返回 true；否则返回 false</returns>
		public bool CanManageDevices() =>
			role is PrincipalRole.DeviceManager or PrincipalRole.Administrator;

		/// <summary>
		/// 判断当前角色是否为管理员。
		/// </summary>
		/// <returns>如果角色为管理员，则返回 true；否则返回 false</returns>
		public bool IsAdministrator() => role == PrincipalRole.Administrator;

		/// <summary>
		/// 获取当前角色对应的设备查询范围枚举值。
		/// </summary>
		/// <returns>当前角色对应的设备查询范围枚举值</returns>
		public PrincipalQueryScope GetDeviceQueryScope() =>
			role switch {
				PrincipalRole.FullQuery or PrincipalRole.DeviceManager or PrincipalRole.Administrator => PrincipalQueryScope.Full,
				PrincipalRole.LimitedQuery => PrincipalQueryScope.Limited,
				_ => PrincipalQueryScope.None
			};

		/// <summary>
		/// 获取当前角色对应的设备查询范围文本。
		/// </summary>
		/// <returns>用于页面展示的设备查询范围文本</returns>
		public string GetDeviceQueryScopeText() => role.GetDeviceQueryScope().GetText();
	}

	/// <summary>
	/// 为 <see cref="ClaimsPrincipal"/> 提供交互式后台会话判定相关的扩展方法组
	/// </summary>
	/// <param name="principal">当前待分析认证身份的主体对象</param>
	extension(ClaimsPrincipal principal) {
		/// <summary>
		/// 判断当前主体是否持有 Identity Cookie 交互式登录会话。
		/// </summary>
		/// <returns>如果当前主体持有交互式登录会话，则返回 true；否则返回 false</returns>
		public bool HasInteractiveUserSession() =>
			principal.Identities.Any(identity =>
				identity.IsAuthenticated
				&& string.Equals(identity.AuthenticationType, AuthenticationSchemeNames.IdentityCookie, StringComparison.Ordinal));

		/// <summary>
		/// 获取当前已认证主体的业务类型、主体 ID 与管理角色。
		/// </summary>
		/// <returns>当前已认证主体的业务类型、主体 ID 与管理角色</returns>
		public (PrincipalKind Kind, Guid? PrincipalId, PrincipalRole? Role) GetAuthenticatedPrincipalInfo() {
			var signatureIdentity = principal.Identities.FirstOrDefault(identity =>
				identity.IsAuthenticated
				&& string.Equals(identity.AuthenticationType, AuthenticationSchemeNames.Signature, StringComparison.Ordinal));

			if (signatureIdentity is not null) {
				var principalId = signatureIdentity.ReadPrincipalId();
				var roleClaims = signatureIdentity.FindAll(ClaimTypes.Role);
				if (roleClaims.Any(claim => string.Equals(claim.Value, "Device", StringComparison.Ordinal))) {
					return (PrincipalKind.Device, principalId, null);
				}

				// 如果主体是签名式 API 凭据，则读取其管理角色，设计上其不可能为 null
				var role = signatureIdentity.ReadPrincipalRole()
					?? throw new InvalidOperationException("签名式 API 凭据主体缺少合法的管理角色声明。");
				return (PrincipalKind.ApiCredential, principalId, role);
			}

			var userIdentity = principal.Identities.FirstOrDefault(identity =>
				identity.IsAuthenticated
				&& string.Equals(identity.AuthenticationType, AuthenticationSchemeNames.IdentityCookie, StringComparison.Ordinal));

			return userIdentity is null
				? (PrincipalKind.Unknown, null, null)
				: (PrincipalKind.User, userIdentity.ReadPrincipalId(), userIdentity.ReadPrincipalRole(PrincipalRole.LimitedQuery));
		}
	}

	extension(ClaimsIdentity identity) {
		/// <summary>
		/// 从认证身份中读取主体 ID。
		/// </summary>
		/// <param name="identity">认证身份</param>
		/// <returns>主体 ID；如果身份中不包含合法主体 ID，则返回 null</returns>
		private Guid? ReadPrincipalId() {
			var rawPrincipalId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			return Guid.TryParse(rawPrincipalId, out var principalId) ? principalId : null;
		}

		/// <summary>
		/// 从认证身份中读取管理角色。
		/// </summary>
		/// <param name="identity">认证身份</param>
		/// <param name="defaultRole">未找到合法管理角色时使用的默认角色，默认为 null</param>
		/// <returns>管理角色；如果未找到合法管理角色，则返回默认角色</returns>
		private PrincipalRole? ReadPrincipalRole(PrincipalRole? defaultRole = null) {
			PrincipalRole? role = null;

			var roleClaims = identity.FindAll(ClaimTypes.Role);
			foreach (var claim in roleClaims) {
				if (!Enum.TryParse<PrincipalRole>(claim.Value, true, out var parsedRole)) {
					continue;
				}

				if (role is not null) {
					throw new InvalidOperationException($"当前登录主体包含多个管理角色声明，无法确定最终角色：{string.Join(", ", roleClaims.Select(claim => claim.Value))}");
				}
				role = parsedRole;
			}

			return role ?? defaultRole;
		}
	}

	/// <summary>
	/// 为 <see cref="PrincipalQueryScope"/> 提供设备查询范围文本描述的扩展方法组
	/// </summary>
	/// <param name="scope">当前要获取文本描述的设备查询范围枚举值</param>
	extension(PrincipalQueryScope scope) {
		/// <summary>
		/// 获取当前设备查询范围对应的文本描述。
		/// </summary>
		/// <returns>用于页面展示的设备查询范围文本</returns>
		public string GetText() =>
			scope switch {
				PrincipalQueryScope.Full => "全部设备",
				PrincipalQueryScope.Limited => "部分设备",
				_ => "无查询权限"
			};
	}
}

/// <summary>
/// 主体类型。
/// </summary>
public enum PrincipalKind {
	/// <summary>
	/// 交互式后台用户。
	/// </summary>
	User,

	/// <summary>
	/// 签名式 API 凭据。
	/// </summary>
	ApiCredential,

	/// <summary>
	/// 签名式设备主体。
	/// </summary>
	Device,

	/// <summary>
	/// 未识别的认证主体。
	/// </summary>
	Unknown
}

/// <summary>
/// 定义设备查询范围的枚举类型，用于描述不同角色在设备数据访问方面的权限范围。
/// </summary>
public enum PrincipalQueryScope {
	/// <summary>
	/// 无设备查询权限
	/// </summary>
	None,

	/// <summary>
	/// 仅限查询部分设备
	/// </summary>
	Limited,

	/// <summary>
	/// 可查询全部设备
	/// </summary>
	Full
}