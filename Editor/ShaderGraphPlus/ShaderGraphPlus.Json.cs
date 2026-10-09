using System.Text.Json.Nodes;
using static ShaderGraphPlus.ShaderGraphPlus;

namespace ShaderGraphPlus.Internal.JsonConvert;

internal class ShaderGraphPlusConverter : JsonConverter<ShaderGraphPlus>
{
	public override ShaderGraphPlus Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
	{
		throw new NotImplementedException();
	}

	public override void Write( Utf8JsonWriter writer, ShaderGraphPlus graph, JsonSerializerOptions options )
	{
		writer.WriteStartObject();

		var graphType = graph.GetType();
		var properties = graphType.GetProperties( BindingFlags.Instance | BindingFlags.Public )
			.Where( x => x.GetSetMethod() != null );

		foreach ( var property in properties )
		{
			if ( !property.CanRead )
				continue;

			if ( property.PropertyType == typeof( NodeInput ) )
				continue;

			if ( property.Name == JsonKeys.Identifier )
				continue;

			if ( property.IsDefined( typeof( JsonIgnoreAttribute ) ) )
				continue;

			var propertyName = property.Name;
			if ( property.GetCustomAttribute<JsonPropertyNameAttribute>() is { } jpna )
				propertyName = jpna.Name;

			var propertyValue = property.GetValue( graph );

			writer.WritePropertyName( propertyName );
			JsonSerializer.Serialize( writer, propertyValue, options );
		}

		WriteNodesArray( writer, graph.Nodes, options );

		writer.WriteEndObject();
	}

	private void WriteNodesArray( Utf8JsonWriter writer, IEnumerable<IGraphNode> nodes, JsonSerializerOptions options )
	{

	}
}
