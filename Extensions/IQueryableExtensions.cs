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
		/// <param name="searchTerm">设备搜索条件</param>
		/// <returns>应用筛选后的设备查询</returns>
		public IQueryable<Device> WhereMatches(DeviceSearchTerm searchTerm) {
			ArgumentNullException.ThrowIfNull(searchTerm);

			var normalizedDeviceName = searchTerm.NormalizedDeviceName;
			var displayName = searchTerm.DisplayName;

			if (normalizedDeviceName is null) {
				// 如果没有指定设备名称，则只按显示名称筛选
				return displayName is null
					? devices
					: devices.Where(device => device.DisplayName != null
						&& device.DisplayName.Contains(displayName));
			}

			return displayName is null
				? devices.Where(device => device.NormalizedDeviceName.Contains(normalizedDeviceName))
				: devices.Where(device =>
					device.NormalizedDeviceName.Contains(normalizedDeviceName)
					|| (device.DisplayName != null && device.DisplayName.Contains(displayName)));
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
		/// <param name="searchTerm">设备搜索条件</param>
		/// <returns>应用筛选后的日志查询</returns>
		public IQueryable<OnlineLog> WhereDeviceMatches(DeviceSearchTerm searchTerm) {
			ArgumentNullException.ThrowIfNull(searchTerm);

			var normalizedDeviceName = searchTerm.NormalizedDeviceName;
			var displayName = searchTerm.DisplayName;
			if (normalizedDeviceName is null) {
				return displayName is null
					? logs
					: logs.Where(log => log.Device.DisplayName != null
						&& log.Device.DisplayName.Contains(displayName));
			}

			return displayName is null
				? logs.Where(log => log.Device.NormalizedDeviceName.Contains(normalizedDeviceName))
				: logs.Where(log =>
					log.Device.NormalizedDeviceName.Contains(normalizedDeviceName)
					|| (log.Device.DisplayName != null && log.Device.DisplayName.Contains(displayName)));
		}
	}
}