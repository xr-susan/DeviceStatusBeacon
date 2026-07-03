using DeviceStatusBeacon.Services;

namespace DeviceStatusBeacon.Api;

/// <summary>
/// 设备列表 API 响应。
/// </summary>
/// <param name="Pagination">分页数据</param>
/// <param name="Devices">设备摘要列表</param>
public sealed record DeviceListApiResponse(
	PaginationData Pagination,
	IReadOnlyCollection<DeviceSummary> Devices
);