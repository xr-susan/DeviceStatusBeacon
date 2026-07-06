using System.Security.Claims;

namespace DeviceStatusBeacon.Services;

public sealed partial class AccountSettingsService {
	/// <inheritdoc/>
	public async Task SetDisplayNameAsync(ClaimsPrincipal principal, SetCurrentUserDisplayNameCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new AccountSettingsException(StatusCodes.Status422UnprocessableEntity, message));

		// 当前用户只能修改自己的显示名称，使用主体 ID 收窄更新范围
		var target = await GetCurrentUserTargetAsync(principal, cancellationToken);
		var updatedCount = await dbContext.Users
			.WhereUserId(target.UserId)
			.ExecuteUpdateAsync(
				user => user.SetProperty(entity => entity.DisplayName, command.DisplayName),
				cancellationToken);

		if (updatedCount == 0) {
			throw new AccountSettingsException(StatusCodes.Status404NotFound, "未找到当前用户");
		}
	}

	/// <inheritdoc/>
	public async Task ChangePasswordAsync(ClaimsPrincipal principal, ChangeCurrentUserPasswordCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new AccountSettingsException(StatusCodes.Status422UnprocessableEntity, message));

		var target = await GetCurrentUserTargetAsync(principal, cancellationToken);
		var user = await userManager.FindByIdAsync(target.UserId.ToString())
			?? throw new AccountSettingsException(StatusCodes.Status404NotFound, "未找到当前用户");

		// 当前用户修改密码必须提供旧密码，交给 Identity 统一执行密码校验和哈希更新
		var result = await userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);

		if (!result.Succeeded) {
			throw new AccountSettingsException(
				StatusCodes.Status422UnprocessableEntity,
				string.Join(Environment.NewLine, result.Errors.Select(error => error.Description)));
		}
	}
}