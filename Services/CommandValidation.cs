using System.ComponentModel.DataAnnotations;

namespace DeviceStatusBeacon.Services;

/// <summary>
/// 服务层命令校验辅助方法。
/// </summary>
public static class CommandValidation {
	/// <summary>
	/// 显示名称最大长度。
	/// </summary>
	public const int DisplayNameMaximumLength = 64;

	/// <summary>
	/// 校验命令对象。
	/// </summary>
	/// <typeparam name="TException">校验失败时抛出的异常类型</typeparam>
	/// <param name="command">命令对象</param>
	/// <param name="exceptionFactory">异常工厂</param>
	/// <exception cref="TException">命令对象校验失败</exception>
	public static void EnsureValid<TException>(object command, Func<string, TException> exceptionFactory)
		where TException : Exception {
		List<ValidationResult> validationResults = [];
		if (Validator.TryValidateObject(command, new(command), validationResults, true)) {
			return;
		}

		throw exceptionFactory(string.Join(Environment.NewLine, validationResults.Select(result => result.ErrorMessage)));
	}

	/// <summary>
	/// 校验可选显示名称。
	/// </summary>
	/// <param name="displayName">显示名称</param>
	/// <param name="memberName">成员名称</param>
	/// <param name="displayNameText">显示名称文本</param>
	/// <returns>校验结果</returns>
	public static IEnumerable<ValidationResult> ValidateOptionalDisplayName(string? displayName, string memberName, string displayNameText) {
		if (displayName is null) {
			yield break;
		}

		foreach (var result in ValidateDisplayNameValue(displayName, memberName, displayNameText)) {
			yield return result;
		}
	}

	/// <summary>
	/// 校验必填显示名称。
	/// </summary>
	/// <param name="displayName">显示名称</param>
	/// <param name="memberName">成员名称</param>
	/// <param name="displayNameText">显示名称文本</param>
	/// <returns>校验结果</returns>
	public static IEnumerable<ValidationResult> ValidateRequiredDisplayName(string? displayName, string memberName, string displayNameText) {
		if (displayName is null) {
			yield return new($"{displayNameText}不能为空。", [memberName]);
			yield break;
		}

		foreach (var result in ValidateDisplayNameValue(displayName, memberName, displayNameText)) {
			yield return result;
		}
	}

	/// <summary>
	/// 校验非 null 显示名称。
	/// </summary>
	/// <param name="displayName">显示名称</param>
	/// <param name="memberName">成员名称</param>
	/// <param name="displayNameText">显示名称文本</param>
	/// <returns>校验结果</returns>
	private static IEnumerable<ValidationResult> ValidateDisplayNameValue(string displayName, string memberName, string displayNameText) {
		if (displayName.Length == 0) {
			yield return new($"{displayNameText}不能为空字符串。", [memberName]);
			yield break;
		}

		if (displayName.Length > DisplayNameMaximumLength) {
			yield return new($"{displayNameText}长度不能超过 {DisplayNameMaximumLength} 个字符。", [memberName]);
			yield break;
		}

		if (char.IsWhiteSpace(displayName[0]) || char.IsWhiteSpace(displayName[^1])) {
			yield return new($"{displayNameText}不能以空白字符开头或结尾。", [memberName]);
			yield break;
		}
	}
}