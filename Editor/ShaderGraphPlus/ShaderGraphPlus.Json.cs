using System.Text.Json.Nodes;

namespace ShaderGraphPlus.Internal.JsonConvert;

internal class ShaderGraphPlusConverter : JsonConverter<ShaderGraphPlus>
{
	public override ShaderGraphPlus Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
	{
		throw new NotImplementedException();
	}

	public override void Write( Utf8JsonWriter writer, ShaderGraphPlus graph, JsonSerializerOptions options )
	{
		throw new NotImplementedException();
	}
}
