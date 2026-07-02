namespace DeviceStatusBeacon.Extensions;

/// <summary>
/// 为设备查询提供常用筛选条件的扩展方法。
/// </summary>
public static class DeviceQueryableExtensions {
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
		/// 按标准化设备名称筛选设备查询。
		/// </summary>
		/// <param name="normalizedDeviceName">标准化设备名称</param>
		/// <returns>应用筛选后的设备查询</returns>
		public IQueryable<Device> WhereNormalizedDeviceName(string normalizedDeviceName) =>
			devices.Where(device => device.NormalizedDeviceName == normalizedDeviceName);
	}
}