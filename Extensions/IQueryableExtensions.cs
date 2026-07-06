namespace DeviceStatusBeacon.Extensions;

/// <summary>
/// 为 <see cref="IQueryable{T}"/> 查询提供常用筛选条件的扩展方法。
/// </summary>
public static class IQueryableExtensions {
	/// <summary>
	/// 为 <see cref="IQueryable{Device}"/> 提供设备筛选相关的扩展方法组
	/// </summary>
	/// <param name="devices">设备查询</param>
	extension(IQueryable<Device> devices) {
		/// <summary>
		/// 按设备 ID 筛选设备查询。
		/// </summary>
		/// <param name="deviceId">设备 ID</param>
		/// <returns>应用筛选后的设备查询</returns>
		public IQueryable<Device> WhereDeviceId(Guid deviceId) =>
			devices.Where(device => device.DeviceId == deviceId);

		/// <summary>
		/// 按设备名称筛选设备查询。
		/// </summary>
		/// <param name="deviceName">设备名称查找条件</param>
		/// <returns>应用筛选后的设备查询</returns>
		public IQueryable<Device> WhereDeviceName(IdentityNameLookup deviceName) {
			ArgumentNullException.ThrowIfNull(deviceName);

			return devices.Where(device => device.NormalizedDeviceName == deviceName.NormalizedName);
		}

		/// <summary>
		/// 按设备搜索条件筛选设备查询。
		/// </summary>
		/// <param name="searchTerm">身份标识搜索条件</param>
		/// <returns>应用筛选后的设备查询</returns>
		public IQueryable<Device> WhereMatches(IdentitySearchTerm searchTerm) {
			ArgumentNullException.ThrowIfNull(searchTerm);

			var normalizedName = searchTerm.NormalizedName;
			var displayName = searchTerm.DisplayName;

			if (normalizedName is null) {
				// 如果没有指定设备名称，则只按显示名称筛选
				return displayName is null
					? devices
					: devices.Where(device => device.DisplayName != null
						&& device.DisplayName.Contains(displayName));
			}

			return displayName is null
				? devices.Where(device => device.NormalizedDeviceName.Contains(normalizedName))
				: devices.Where(device =>
					device.NormalizedDeviceName.Contains(normalizedName)
					|| (device.DisplayName != null && device.DisplayName.Contains(displayName)));
		}
	}

	/// <summary>
	/// 为 <see cref="IQueryable{User}"/> 提供用户筛选相关的扩展方法组
	/// </summary>
	/// <param name="users">用户查询</param>
	extension(IQueryable<User> users) {
		/// <summary>
		/// 按用户 ID 筛选用户查询。
		/// </summary>
		/// <param name="userId">用户 ID</param>
		/// <returns>应用筛选后的用户查询</returns>
		public IQueryable<User> WhereUserId(Guid userId) =>
			users.Where(user => user.Id == userId);

		/// <summary>
		/// 按用户名筛选用户查询。
		/// </summary>
		/// <param name="userName">用户名查找条件</param>
		/// <returns>应用筛选后的用户查询</returns>
		public IQueryable<User> WhereUserName(IdentityNameLookup userName) {
			ArgumentNullException.ThrowIfNull(userName);

			return users.Where(user => user.NormalizedUserName == userName.NormalizedName);
		}

		/// <summary>
		/// 按用户搜索条件筛选用户查询。
		/// </summary>
		/// <param name="searchTerm">身份标识搜索条件</param>
		/// <returns>应用筛选后的用户查询</returns>
		public IQueryable<User> WhereMatches(IdentitySearchTerm searchTerm) {
			ArgumentNullException.ThrowIfNull(searchTerm);

			var normalizedName = searchTerm.NormalizedName;
			var displayName = searchTerm.DisplayName;

			if (normalizedName is null) {
				// 如果没有指定用户名，则只按显示名称筛选
				return displayName is null
					? users
					: users.Where(user => user.DisplayName != null
						&& user.DisplayName.Contains(displayName));
			}

			return displayName is null
				? users.Where(user => user.NormalizedUserName != null
					&& user.NormalizedUserName.Contains(normalizedName))
				: users.Where(user =>
					(user.NormalizedUserName != null && user.NormalizedUserName.Contains(normalizedName))
					|| (user.DisplayName != null && user.DisplayName.Contains(displayName)));
		}
	}

	/// <summary>
	/// 为 <see cref="IQueryable{ApiCredential}"/> 提供 API 凭据筛选相关的扩展方法组
	/// </summary>
	/// <param name="apiCredentials">API 凭据查询</param>
	extension(IQueryable<ApiCredential> apiCredentials) {
		/// <summary>
		/// 按 API 凭据 ID 筛选 API 凭据查询。
		/// </summary>
		/// <param name="apiCredentialId">API 凭据 ID</param>
		/// <returns>应用筛选后的 API 凭据查询</returns>
		public IQueryable<ApiCredential> WhereApiCredentialId(Guid apiCredentialId) =>
			apiCredentials.Where(credential => credential.ApiCredentialId == apiCredentialId);

		/// <summary>
		/// 按所属用户 ID 筛选 API 凭据查询。
		/// </summary>
		/// <param name="ownerUserId">所属用户 ID</param>
		/// <returns>应用筛选后的 API 凭据查询</returns>
		public IQueryable<ApiCredential> WhereOwnerUserId(Guid ownerUserId) =>
			apiCredentials.Where(credential => credential.UserId == ownerUserId);

		/// <summary>
		/// 按所属用户名筛选 API 凭据查询。
		/// </summary>
		/// <param name="ownerUserName">所属用户名查找条件</param>
		/// <returns>应用筛选后的 API 凭据查询</returns>
		public IQueryable<ApiCredential> WhereOwnerUserName(IdentityNameLookup ownerUserName) {
			ArgumentNullException.ThrowIfNull(ownerUserName);

			return apiCredentials.Where(credential => credential.User.NormalizedUserName == ownerUserName.NormalizedName);
		}
	}

	/// <summary>
	/// 为 <see cref="IQueryable{OnlineLog}"/> 提供日志筛选相关的扩展方法组
	/// </summary>
	/// <param name="logs">日志查询</param>
	extension(IQueryable<OnlineLog> logs) {
		/// <summary>
		/// 按关联设备 ID 筛选日志查询。
		/// </summary>
		/// <param name="deviceId">设备 ID</param>
		/// <returns>应用筛选后的日志查询</returns>
		public IQueryable<OnlineLog> WhereDeviceId(Guid deviceId) =>
			logs.Where(log => log.DeviceId == deviceId);

		/// <summary>
		/// 按关联设备的设备名称筛选日志查询。
		/// </summary>
		/// <param name="deviceName">设备名称查找条件</param>
		/// <returns>应用筛选后的日志查询</returns>
		public IQueryable<OnlineLog> WhereDeviceName(IdentityNameLookup deviceName) {
			ArgumentNullException.ThrowIfNull(deviceName);

			return logs.Where(log => log.Device.NormalizedDeviceName == deviceName.NormalizedName);
		}

		/// <summary>
		/// 按关联设备搜索条件筛选日志查询。
		/// </summary>
		/// <param name="searchTerm">身份标识搜索条件</param>
		/// <returns>应用筛选后的日志查询</returns>
		public IQueryable<OnlineLog> WhereDeviceMatches(IdentitySearchTerm searchTerm) {
			ArgumentNullException.ThrowIfNull(searchTerm);

			var normalizedName = searchTerm.NormalizedName;
			var displayName = searchTerm.DisplayName;
			if (normalizedName is null) {
				// 如果没有指定设备名称，则只按显示名称筛选
				return displayName is null
					? logs
					: logs.Where(log => log.Device.DisplayName != null
						&& log.Device.DisplayName.Contains(displayName));
			}

			return displayName is null
				? logs.Where(log => log.Device.NormalizedDeviceName.Contains(normalizedName))
				: logs.Where(log =>
					log.Device.NormalizedDeviceName.Contains(normalizedName)
					|| (log.Device.DisplayName != null && log.Device.DisplayName.Contains(displayName)));
		}
	}
}