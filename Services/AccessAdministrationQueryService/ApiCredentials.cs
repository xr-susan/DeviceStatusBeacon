namespace DeviceStatusBeacon.Services;

public sealed partial class AccessAdministrationQueryService {
	/// <inheritdoc/>
	public async Task<IReadOnlyCollection<AccessApiCredentialSummary>> GetApiCredentialsForConsoleAsync(string ownerUserName, CancellationToken cancellationToken = default) {
		var ownerNameLookup = IdentityNameLookup.TryCreate(ownerUserName, lookupNormalizer);
		if (ownerNameLookup is null) {
			return [];
		}

		// 控制台只允许从所属用户视角列出凭据，避免 API 凭据表现为脱离用户的全局资源
		return await QueryApiCredentialsPageAsync(
			dbContext.ApiCredentials
				.AsNoTracking()
				.WhereOwnerUserName(ownerNameLookup),
			0,
			MaxAccessQueryCount + 1,
			cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ApiCredentialListData> GetApiCredentialsByOwnerUserIdAsync(Guid ownerUserId, int pageNumber, int pageSize, CancellationToken cancellationToken = default) {
		var normalizedPageNumber = NormalizePageNumber(pageNumber);
		var normalizedPageSize = NormalizePageSize(pageSize);

		// 单用户维护入口已经由用户名路由锁定用户，凭据读取只按所属用户 ID 限定范围
		var credentialsQuery = dbContext.ApiCredentials
			.AsNoTracking()
			.WhereOwnerUserId(ownerUserId);

		// 先统计当前用户关联的凭据总数，再查询当前页
		var totalCount = await credentialsQuery.CountAsync(cancellationToken);
		normalizedPageNumber = NormalizePageNumberForTotalCount(normalizedPageNumber, normalizedPageSize, totalCount);

		var apiCredentials = await QueryApiCredentialsPageAsync(
			credentialsQuery,
			CalculateSkipCount(normalizedPageNumber, normalizedPageSize),
			normalizedPageSize,
			cancellationToken);

		return new(
			new(totalCount, normalizedPageNumber, normalizedPageSize),
			apiCredentials);
	}

	/// <summary>
	/// 查询访问管理 API 凭据分页数据。
	/// </summary>
	/// <param name="apiCredentials">已应用全部过滤的 API 凭据查询</param>
	/// <param name="skip">已经规范化的跳过数量</param>
	/// <param name="take">已经规范化的查询数量</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为 API 凭据列表</returns>
	private static async Task<IReadOnlyCollection<AccessApiCredentialSummary>> QueryApiCredentialsPageAsync(
		IQueryable<ApiCredential> apiCredentials, int skip, int take, CancellationToken cancellationToken) =>
		await apiCredentials
			.OrderBy(credential => credential.ApiCredentialId)
			.Skip(skip)
			.Take(take)
			.Select(credential => new AccessApiCredentialSummary(
				credential.ApiCredentialId,
				credential.DisplayName ?? string.Empty,
				credential.Enabled,
				credential.Role,
				credential.UserId,
				credential.User.UserName ?? string.Empty,
				credential.User.DisplayName,
				credential.User.UserRoles.Select(userRole => userRole.Role.Name).SingleOrDefault(),
				credential.AuthorizedDeviceLinks.Count))
			.ToListAsync(cancellationToken);
}