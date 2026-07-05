using System.Security.Claims;

namespace DeviceStatusBeacon.Services.Interfaces;

/// <summary>
/// 为 Web 管理页面和 CLI 管理命令提供共享查询能力的服务。
/// </summary>
/// <remarks>
/// 该服务聚焦于“读取”侧能力：
/// 统一查询范围判断、设备列表读取、日志读取和首页摘要构建。
/// 这样可以避免 Web 与 CLI 各自维护一套相似但逐渐偏离的查询逻辑。
/// </remarks>
public interface IManagementQueryService {
	/// <summary>
	/// 基于当前登录主体构建一个受权限约束的查询会话。
	/// </summary>
	/// <remarks>
	/// 返回结果只保留角色这一份源数据。
	/// 具体读取 / 管理能力应通过角色扩展方法即时推导，
	/// 不在会话记录中重复存储角色投影。
	/// </remarks>
	/// <param name="principal">当前登录主体</param>
	/// <returns>构建完成的查询会话</returns>
	ManagementQuerySession CreateQuerySessionAsync(ClaimsPrincipal principal);

	/// <summary>
	/// 创建一个拥有全部设备与日志读取能力的特权查询会话。
	/// </summary>
	/// <param name="userName">会话显示用名称</param>
	/// <returns>一个可直接读取全部设备和日志的查询会话</returns>
	ManagementQuerySession CreatePrivilegedQuerySession(string userName = "CLI");

	/// <summary>
	/// 获取 Dashboard 首屏摘要数据。
	/// </summary>
	/// <param name="principal">当前登录主体</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为 Dashboard 首屏摘要数据</returns>
	Task<DashboardOverviewData> GetDashboardOverviewAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

	/// <summary>
	/// 获取 Dashboard 首屏摘要数据。
	/// </summary>
	/// <param name="session">查询会话</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为 Dashboard 首屏摘要数据</returns>
	Task<DashboardOverviewData> GetDashboardOverviewAsync(ManagementQuerySession session, CancellationToken cancellationToken = default);

	/// <summary>
	/// 获取 Dashboard 中按需加载的补充摘要与最近活动数据。
	/// </summary>
	/// <param name="principal">当前登录主体</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为补充摘要与最近活动数据</returns>
	Task<DashboardActivityData> GetDashboardActivityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

	/// <summary>
	/// 获取 Dashboard 中按需加载的补充摘要与最近活动数据。
	/// </summary>
	/// <param name="session">查询会话</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为补充摘要与最近活动数据</returns>
	Task<DashboardActivityData> GetDashboardActivityAsync(ManagementQuerySession session, CancellationToken cancellationToken = default);

	/// <summary>
	/// 获取设备列表页数据。
	/// </summary>
	/// <param name="principal">当前登录主体</param>
	/// <param name="searchTerm">设备筛选关键字</param>
	/// <param name="pageNumber">页码</param>
	/// <param name="pageSize">每页数量</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为设备列表页数据</returns>
	Task<DeviceListData> GetDevicesAsync(ClaimsPrincipal principal, string? searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

	/// <summary>
	/// 获取设备列表页数据。
	/// </summary>
	/// <param name="session">查询会话</param>
	/// <param name="searchTerm">设备筛选关键字</param>
	/// <param name="pageNumber">页码</param>
	/// <param name="pageSize">每页数量</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为设备列表页数据</returns>
	Task<DeviceListData> GetDevicesAsync(ManagementQuerySession session, string? searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

	/// <summary>
	/// 获取日志列表页数据。
	/// </summary>
	/// <param name="principal">当前登录主体</param>
	/// <param name="deviceKeyword">设备筛选关键字</param>
	/// <param name="pageNumber">页码</param>
	/// <param name="pageSize">每页数量</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为日志列表页数据</returns>
	Task<LogListData> GetLogsAsync(ClaimsPrincipal principal, string? deviceKeyword, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

	/// <summary>
	/// 获取日志列表页数据。
	/// </summary>
	/// <param name="session">查询会话</param>
	/// <param name="deviceKeyword">设备筛选关键字</param>
	/// <param name="pageNumber">页码</param>
	/// <param name="pageSize">每页数量</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为日志列表页数据</returns>
	Task<LogListData> GetLogsAsync(ManagementQuerySession session, string? deviceKeyword, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

	/// <summary>
	/// 查询设备列表片段。
	/// </summary>
	/// <param name="session">查询会话</param>
	/// <param name="searchTerm">设备筛选关键字</param>
	/// <param name="take">返回条数</param>
	/// <param name="sortByNormalizedDeviceName">是否按标准化设备名称升序排序</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为设备列表片段</returns>
	Task<IReadOnlyCollection<DeviceSummary>> GetDeviceSliceAsync(ManagementQuerySession session, string? searchTerm, int take, bool sortByNormalizedDeviceName = false, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按设备名称获取单个设备的最新摘要。
	/// </summary>
	/// <param name="session">查询会话</param>
	/// <param name="deviceName">设备名称</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为设备摘要；如果未找到，则返回 null</returns>
	Task<DeviceSummary?> GetDeviceByNameAsync(ManagementQuerySession session, string deviceName, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按设备 ID 获取单个设备的最新摘要。
	/// </summary>
	/// <param name="session">查询会话</param>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为设备摘要；如果未找到，则返回 null</returns>
	Task<DeviceSummary?> GetDeviceByIdAsync(ManagementQuerySession session, Guid deviceId, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按设备名称获取单设备详情数据。
	/// </summary>
	/// <param name="session">查询会话</param>
	/// <param name="deviceName">设备名称</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为设备详情；如果未找到，则返回 null</returns>
	Task<DeviceDetailsData?> GetDeviceDetailsByNameAsync(ManagementQuerySession session, string deviceName, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按设备 ID 获取单设备详情数据。
	/// </summary>
	/// <param name="session">查询会话</param>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为设备详情；如果未找到，则返回 null</returns>
	Task<DeviceDetailsData?> GetDeviceDetailsByIdAsync(ManagementQuerySession session, Guid deviceId, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按设备名称获取最近日志列表。
	/// </summary>
	/// <param name="session">查询会话</param>
	/// <param name="deviceName">设备名称</param>
	/// <param name="take">返回条数</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为日志列表</returns>
	Task<IReadOnlyCollection<OnlineLogSummary>> GetLogsByDeviceNameAsync(ManagementQuerySession session, string deviceName, int take, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按设备 ID 获取最近日志列表。
	/// </summary>
	/// <param name="session">查询会话</param>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="take">返回条数</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为日志列表</returns>
	Task<IReadOnlyCollection<OnlineLogSummary>> GetLogsByDeviceIdAsync(ManagementQuerySession session, Guid deviceId, int take, CancellationToken cancellationToken = default);
}