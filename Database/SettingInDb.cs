namespace DeviceStatusBeacon.Database;

public enum SettingInDbKey {
	/// <summary>
	/// 应用程序版本，对应值类型应当为 <see cref="Version"/>
	/// </summary>
	AppVersion,

	/// <summary>
	/// 实体鉴权信息上次修改时间，用于在数据库中的数据被外部修改后能够及时更新缓存，应当为毫秒时间戳的字符串表示
	/// </summary>
	EntityAuthInfoLastModifiedTime
}


[PrimaryKey(nameof(Key))]
public class SettingInDb {
	/// <summary>
	/// 设置键，同时作为数据表的主键
	/// </summary>
	public required string Key { get; set; }

	/// <summary>
	/// 设置值的字符串表示，可为 null
	/// </summary>
	public string? Value { get; set; }


	/// <summary>
	/// 从数据库中获取指定键的设置值的字符串表示，如果不存在则返回 null
	/// </summary>
	/// <param name="context">数据库上下文</param>
	/// <param name="key">设置键</param>
	/// <returns>获取到的设置值</returns>
	public static async Task<string?> GetValueAsync(DeviceStatusBeaconContext context, SettingInDbKey key) =>
		(await context.Settings.FindAsync(key.ToString()))?.Value;

	/// <summary>
	/// 从数据库中获取指定键的设置值，并使用提供的解析函数将其转换为目标类型，如果设置不存在则返回默认值
	/// </summary>
	/// <typeparam name="T">设置值的类型</typeparam>
	/// <param name="context">数据库上下文</param>
	/// <param name="key">设置键</param>
	/// <param name="parseFunc">解析函数</param>
	/// <returns>获取到的设置值</returns>
	public static async Task<T?> GetValueAsync<T>(DeviceStatusBeaconContext context, SettingInDbKey key, Func<string, T> parseFunc) =>
		await GetValueAsync(context, key) is string value ? parseFunc(value) : default;

	/// <summary>
	/// 向数据库中设置指定键的设置值的字符串表示，如果该键已存在则更新其值，否则插入新的记录
	/// </summary>
	/// <param name="context">数据库上下文</param>
	/// <param name="key">设置键</param>
	/// <param name="value">目标值</param>
	/// <returns>一个表示异步操作的任务</returns>
	public static async Task SetValueAsync(DeviceStatusBeaconContext context, SettingInDbKey key, string? value) {
		var keyString = key.ToString();

		if (await context.Settings.FindAsync(keyString) is { } entry) {
			entry.Value = value;
		} else {
			context.Settings.Add(new SettingInDb { Key = keyString, Value = value });
		}

		await context.SaveChangesAsync();
	}

	/// <summary>
	/// 向数据库中设置指定键的设置值，内部自动将目标值转换为字符串表示，如果该键已存在则更新其值，否则插入新的记录
	/// </summary>
	/// <typeparam name="T">设置值的类型</typeparam>
	/// <param name="context">数据库上下文</param>
	/// <param name="key">设置键</param>
	/// <param name="value">目标值</param>
	/// <returns>一个表示异步操作的任务</returns>
	public static Task SetValueAsync<T>(DeviceStatusBeaconContext context, SettingInDbKey key, T value) =>
		SetValueAsync(context, key, value?.ToString());


	/// <summary>
	/// 从数据库中获取 <see cref="SettingInDbKey.AppVersion"/> 设置值，如果设置不存在则返回 null
	/// </summary>
	/// <param name="context">数据库上下文</param>
	/// <returns>获取到的 AppVersion 对象</returns>
	public static Task<Version?> GetAppVersionAsync(DeviceStatusBeaconContext context) =>
		GetValueAsync(context, SettingInDbKey.AppVersion, Version.Parse);

	/// <summary>
	/// 向数据库中设置 <see cref="SettingInDbKey.AppVersion"/> 设置值，如果该键已存在则更新其值，否则插入新的记录
	/// </summary>
	/// <param name="context">数据库上下文</param>
	/// <param name="version">目标值</param>
	/// <returns>一个表示异步操作的任务</returns>
	public static Task SetAppVersionAsync(DeviceStatusBeaconContext context, Version version) =>
		SetValueAsync(context, SettingInDbKey.AppVersion, version);


	/// <summary>
	/// 对比数据库中 <see cref="SettingInDbKey.EntityAuthInfoLastModifiedTime"/> 设置值与提供的时间戳字符串是否相等，如果设置不存在则返回 false
	/// </summary>
	/// <param name="context">数据库上下文</param>
	/// <param name="lastModifiedTime">要对比的时间戳字符串</param>
	/// <returns>是否相等</returns>
	public static async Task<bool> CompareEntityAuthInfoLastModifiedTimeAsync(DeviceStatusBeaconContext context, string lastModifiedTime) =>
		await GetValueAsync(context, SettingInDbKey.EntityAuthInfoLastModifiedTime) == lastModifiedTime;

	/// <summary>
	/// 更新数据库中 <see cref="SettingInDbKey.EntityAuthInfoLastModifiedTime"/> 设置值为当前时间的毫秒时间戳字符串表示，当该设置项不存在时，将会被创建并设置值
	/// </summary>
	/// <param name="context">数据库上下文</param>
	/// <returns>更新后的时间戳字符串</returns>
	public static async Task<string> UpdateEntityAuthInfoLastModifiedTimeAsync(DeviceStatusBeaconContext context) {
		var newLastModifiedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
		await SetValueAsync(context, SettingInDbKey.EntityAuthInfoLastModifiedTime, newLastModifiedTime);
		return newLastModifiedTime;
	}
}