namespace DeviceStatusBeacon.Services;

public sealed partial class AccessAdministrationQueryService {
	/// <inheritdoc/>
	public async Task<IReadOnlyCollection<AccessUserSummary>> GetUsersForConsoleAsync(string? nameKeyword = null, CancellationToken cancellationToken = default) =>
		await QueryUsersPageAsync(
			BuildUsersQuery(IdentitySearchTerm.Create(nameKeyword, lookupNormalizer)),
			0,
			MaxAccessQueryCount + 1,
			cancellationToken);

	/// <inheritdoc/>
	public async Task<UserListData> GetUsersAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default) {
		// 标准化分页选项
		var normalizedPageNumber = NormalizePageNumber(pageNumber);
		var normalizedPageSize = NormalizePageSize(pageSize);

		// 构建用户查询，并应用关键字筛选
		var userSearchTerm = IdentitySearchTerm.Create(searchTerm, lookupNormalizer);
		var usersQuery = BuildUsersQuery(userSearchTerm);

		// 先统计总数，再根据总页数修正页码，保持分页行为与设备列表一致
		var totalCount = await usersQuery.CountAsync(cancellationToken);
		normalizedPageNumber = NormalizePageNumberForTotalCount(normalizedPageNumber, normalizedPageSize, totalCount);

		var users = await QueryUsersPageAsync(
			usersQuery,
			CalculateSkipCount(normalizedPageNumber, normalizedPageSize),
			normalizedPageSize,
			cancellationToken);

		return new(
			new(totalCount, normalizedPageNumber, normalizedPageSize),
			users);
	}

	/// <inheritdoc/>
	public async Task<AccessUserSummary?> GetUserByNameAsync(string userName, CancellationToken cancellationToken = default) {
		var userNameLookup = IdentityNameLookup.TryCreate(userName, lookupNormalizer);
		if (userNameLookup is null) {
			return null;
		}

		// 单用户维护入口使用人类可读用户名路由，查询时仍严格按标准化用户名命中
		return await MapUsersToAccessSummary(dbContext.Users
				.AsNoTracking()
				.WhereUserName(userNameLookup))
			.SingleOrDefaultAsync(cancellationToken);
	}

	/// <summary>
	/// 构建访问管理用户查询。
	/// </summary>
	/// <param name="searchTerm">身份标识搜索条件</param>
	/// <returns>访问管理用户查询</returns>
	private IQueryable<User> BuildUsersQuery(IdentitySearchTerm searchTerm) =>
		dbContext.Users.AsNoTracking().WhereMatches(searchTerm);

	/// <summary>
	/// 查询访问管理用户分页数据。
	/// </summary>
	/// <param name="users">已应用全部过滤的用户查询</param>
	/// <param name="skip">已经规范化的跳过数量</param>
	/// <param name="take">已经规范化的查询数量</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为用户列表</returns>
	private static async Task<IReadOnlyCollection<AccessUserSummary>> QueryUsersPageAsync(
		IQueryable<User> users,
		int skip,
		int take,
		CancellationToken cancellationToken) =>
		await MapUsersToAccessSummary(users
				.OrderBy(user => user.NormalizedUserName)
				.Skip(skip)
				.Take(take))
			.ToListAsync(cancellationToken);

	/// <summary>
	/// 将用户查询投影为访问管理用户摘要查询。
	/// </summary>
	/// <param name="users">用户查询</param>
	/// <returns>访问管理用户摘要查询</returns>
	private static IQueryable<AccessUserSummary> MapUsersToAccessSummary(IQueryable<User> users) =>
		users.Select(user => new AccessUserSummary(
			user.Id,
			user.UserName ?? string.Empty,
			user.DisplayName,
			user.UserRoles.Select(userRole => userRole.Role.Name).SingleOrDefault(),
			user.AuthorizedDeviceLinks.Count,
			user.ApiCredentials.Count));
}