namespace DeviceStatusBeacon.Extensions;

/// <summary>
/// 为 <see cref="PrincipalRole"/> 提供权限能力判断的扩展方法。
/// </summary>
public static class PrincipalRoleExtensions {
	extension(PrincipalRole? role) {
		/// <summary>
		/// 判断当前角色是否具备设备及日志读取能力。
		/// </summary>
		/// <param name="role">待判断的角色</param>
		/// <returns>如果具备任意设备数据读取能力，则返回 true；否则返回 false</returns>
		public bool CanQueryAnyDevices() =>
			role is PrincipalRole.LimitedQuery or PrincipalRole.FullQuery or PrincipalRole.DeviceManager or PrincipalRole.Administrator;

		/// <summary>
		/// 判断当前角色是否具备全部设备及日志读取能力。
		/// </summary>
		/// <param name="role">待判断的角色</param>
		/// <returns>如果具备全部设备数据读取能力，则返回 true；否则返回 false</returns>
		public bool CanQueryAllDevices() =>
			role is PrincipalRole.FullQuery or PrincipalRole.DeviceManager or PrincipalRole.Administrator;

		/// <summary>
		/// 判断当前角色是否具备设备管理能力。
		/// </summary>
		/// <param name="role">待判断的角色</param>
		/// <returns>如果具备设备管理能力，则返回 true；否则返回 false</returns>
		public bool CanManageDevices() =>
			role is PrincipalRole.DeviceManager or PrincipalRole.Administrator;

		/// <summary>
		/// 判断当前角色是否为管理员。
		/// </summary>
		/// <param name="role">待判断的角色</param>
		/// <returns>如果角色为管理员，则返回 true；否则返回 false</returns>
		public bool IsAdministrator() => role == PrincipalRole.Administrator;
	}
}