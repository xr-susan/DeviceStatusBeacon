namespace DeviceStatusBeacon.Extensions;

/// <summary>
/// 为 <see cref="DateTime"/> 提供 SQLite 时间存储相关的扩展方法。
/// </summary>
internal static class DateTimeExtensions {
	/// <summary>
	/// 为 <see cref="DateTime"/> 提供 SQLite UTC 时间规范化相关的扩展方法组
	/// </summary>
	/// <param name="value">当前要规范化的 <see cref="DateTime"/> 值</param>
	extension(DateTime value) {
		/// <summary>
		/// 将当前时间值规范化为适合写入或读取 SQLite 的 UTC 时间。
		/// </summary>
		/// <returns>语义明确为 UTC 的时间值</returns>
		public DateTime NormalizeToUtcForSQLite() =>
			value.Kind switch {
				DateTimeKind.Utc => value,
				DateTimeKind.Local => value.ToUniversalTime(),
				_ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
			};
	}

	/// <summary>
	/// 为可空 <see cref="DateTime"/> 提供 SQLite UTC 时间规范化相关的扩展方法组
	/// </summary>
	/// <param name="value">当前要规范化的可空 <see cref="DateTime"/> 值</param>
	extension(DateTime? value) {
		/// <summary>
		/// 将当前可空时间值规范化为适合写入或读取 SQLite 的 UTC 时间。
		/// </summary>
		/// <returns>如果当前值存在，则返回语义明确为 UTC 的时间值；否则返回 null</returns>
		public DateTime? NormalizeToUtcForSQLite() =>
			value.HasValue
				? value.Value.NormalizeToUtcForSQLite()
				: null;
	}
}