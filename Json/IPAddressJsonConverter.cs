using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeviceStatusBeacon.Json;

/// <summary>
/// 将 JSON 字符串与 <see cref="IPAddress"/> 互相转换。
/// </summary>
internal sealed class IPAddressJsonConverter : JsonConverter<IPAddress> {
	/// <inheritdoc/>
	public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		if (reader.TokenType != JsonTokenType.String) {
			throw new JsonException("IP 地址必须是字符串。");
		}

		var value = reader.GetString();
		return string.IsNullOrWhiteSpace(value) || !IPAddress.TryParse(value, out var address)
			? throw new JsonException("IP 地址格式无效。")
			: address;
	}

	/// <inheritdoc/>
	public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.ToString());
}