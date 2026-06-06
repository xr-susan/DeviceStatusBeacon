using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

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
				&& string.Equals(identity.AuthenticationType, IdentityConstants.ApplicationScheme, StringComparison.Ordinal));
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