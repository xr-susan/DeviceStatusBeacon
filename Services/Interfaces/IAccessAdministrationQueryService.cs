namespace DeviceStatusBeacon.Services.Interfaces;

/// <summary>
/// 访问管理查询服务。
/// </summary>
public interface IAccessAdministrationQueryService {
	/// <summary>
	/// 查询用户列表片段，默认返回前 <see cref="AccessAdministrationQueryService.MaxAccessQueryCount"/> + 1 条结果，专为控制台查询设计。
	/// </summary>
	/// <param name="nameKeyword">用户名筛选关键字</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为用户列表片段</returns>
	Task<IReadOnlyCollection<AccessUserSummary>> GetUsersForConsoleAsync(string? nameKeyword = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按所属用户名查询 API 凭据列表片段。
	/// </summary>
	/// <param name="ownerUserName">所属用户名</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为 API 凭据列表片段</returns>
	Task<IReadOnlyCollection<AccessApiCredentialSummary>> GetApiCredentialsForConsoleAsync(string ownerUserName, CancellationToken cancellationToken = default);

	/// <summary>
	/// 获取访问管理用户列表页数据。
	/// </summary>
	/// <param name="searchTerm">用户筛选关键字</param>
	/// <param name="pageNumber">页码</param>
	/// <param name="pageSize">每页数量</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为访问管理用户列表页数据</returns>
	Task<UserListData> GetUsersAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按用户名获取访问管理用户摘要。
	/// </summary>
	/// <param name="userName">用户名</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为访问管理用户摘要；如果未找到用户，则返回 null</returns>
	Task<AccessUserSummary?> GetUserByNameAsync(string userName, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按设备 ID 获取访问管理设备授权用户数据。
	/// </summary>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="pageNumber">页码</param>
	/// <param name="pageSize">每页数量</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为访问管理设备授权用户数据；如果未找到设备，则返回 null</returns>
	Task<DeviceAuthorizedUsersData?> GetDeviceAuthorizedUsersAsync(Guid deviceId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按设备名称获取访问管理设备授权用户数据。
	/// </summary>
	/// <param name="deviceName">设备名称</param>
	/// <param name="pageNumber">页码</param>
	/// <param name="pageSize">每页数量</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为访问管理设备授权用户数据；如果未找到设备，则返回 null</returns>
	Task<DeviceAuthorizedUsersData?> GetDeviceAuthorizedUsersAsync(string deviceName, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按所属用户 ID 获取访问管理 API 凭据列表页数据。
	/// </summary>
	/// <param name="ownerUserId">所属用户 ID</param>
	/// <param name="pageNumber">页码</param>
	/// <param name="pageSize">每页数量</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为访问管理 API 凭据列表页数据</returns>
	Task<ApiCredentialListData> GetApiCredentialsByOwnerUserIdAsync(Guid ownerUserId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}