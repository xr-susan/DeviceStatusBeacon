namespace DeviceStatusBeacon.Services;

public sealed partial class AccessAdministrationQueryService {
	/// <inheritdoc/>
	public async Task<IReadOnlyCollection<AccessUserSummary>> GetUsersForConsoleAsync(string? nameKeyword = null, CancellationToken cancellationToken = default) =>
		await QueryUsersPageAsync(
			BuildUsersQuery(NormalizeIdentityNameKeyword(nameKeyword)),
			0,
			MaxAccessQueryCount + 1,
			cancellationToken);

	/// <inheritdoc/>
	public async Task<UserListData> GetUsersAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default) {
		// 用户列表按标准化用户名匹配
		var normalizedNameKeyword = NormalizeIdentityNameKeyword(searchTerm);
		var normalizedPageNumber = NormalizePageNumber(pageNumber);
		var normalizedPageSize = NormalizePageSize(pageSize);

		var usersQuery = BuildUsersQuery(normalizedNameKeyword);

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
				.Where(user => user.NormalizedUserName == userNameLookup.NormalizedName))
			.SingleOrDefaultAsync(cancellationToken);
	}

	/// <summary>
	/// 构建访问管理用户查询。
	/// </summary>
	/// <param name="normalizedNameKeyword">已经归一化的用户名筛选关键字</param>
	/// <returns>访问管理用户查询</returns>
	private IQueryable<User> BuildUsersQuery(string? normalizedNameKeyword) {
		var usersQuery = dbContext.Users.AsNoTracking();
		return normalizedNameKeyword is null
			? usersQuery
			: usersQuery.Where(user => user.NormalizedUserName != null
				&& user.NormalizedUserName.Contains(normalizedNameKeyword));
	}

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

	/// <summary>
	/// 归一化身份标识名称搜索关键字。
	/// </summary>
	/// <param name="nameKeyword">身份标识名称搜索关键字</param>
	/// <returns>归一化后的身份标识名称搜索关键字；没有有效关键字时返回 null</returns>
	private string? NormalizeIdentityNameKeyword(string? nameKeyword) {
		if (string.IsNullOrWhiteSpace(nameKeyword)) {
			return null;
		}

		var normalizedNameKeyword = lookupNormalizer.NormalizeName(nameKeyword.Trim());
		return string.IsNullOrEmpty(normalizedNameKeyword) ? null : normalizedNameKeyword;
	}
}