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

			WriteProperty( writer, propertyName, propertyValue, options );
		}

		//WriteNodesArray( writer, graph.Nodes, options );

		var identifiers = new Dictionary<string, string>();
		foreach ( var node in graph.Nodes )
		{
			identifiers.Add( node.Identifier, $"{identifiers.Count}" );
		}

		WriteArray( writer, JsonKeys.NodeArray, graph.Nodes, ( item ) => WriteNodeArrayEntry( writer, (IGraphNode)item, options, identifiers ) );
		WriteArray( writer, JsonKeys.ParameterArray, graph.Parameters, ( item ) => WriteParameterArrayEntry( writer, (IBlackboardParameter)item, options ) );
		WriteArray( writer, JsonKeys.GroupDataArray, graph.GroupData, ( item ) => WriteGroupDataArrayEntry( writer, (GroupData)item, options ) );

		WriteProperty( writer, JsonKeys.Version, graph.Version, options );

		writer.WriteEndObject();
	}

	private void WriteProperty( Utf8JsonWriter writer, string propertyName, object propertyValue, JsonSerializerOptions options )
	{
		writer.WritePropertyName( propertyName );
		JsonSerializer.Serialize( writer, propertyValue, options );
	}

	private void WriteArray( Utf8JsonWriter writer, string propertyName, IEnumerable<object> items, Action<object> entryWrite )
	{
		writer.WritePropertyName( propertyName );
		writer.WriteStartArray();

		foreach ( var item in items )
		{
			entryWrite?.Invoke( item );
		}

		writer.WriteEndArray();
	}

	private void WriteNodeArrayEntry( Utf8JsonWriter writer, IGraphNode node, JsonSerializerOptions options, Dictionary<string, string> identifiers = null )
	{
		var type = node.GetType();
	
		writer.WriteStartObject();
	
		WriteProperty( writer, JsonKeys.Class, type.Name, options );
	
		if ( identifiers.TryGetValue( node.Identifier, out var newIdentifier ) )
		{
			WriteProperty( writer, JsonKeys.Identifier, newIdentifier, options );
		}
	
		SerializeObject( node, writer, options, identifiers );
	
		writer.WriteEndObject();
	}

	private void WriteParameterArrayEntry( Utf8JsonWriter writer, IBlackboardParameter parameter, JsonSerializerOptions options )
	{
		var type = parameter.GetType();

		writer.WriteStartObject();

		WriteProperty( writer, JsonKeys.Class, type.Name, options );
		WriteProperty( writer, JsonKeys.Identifier, parameter.Identifier, options );

		SerializeObject( parameter, writer, options );

		writer.WriteEndObject();
	}

	private void WriteGroupDataArrayEntry( Utf8JsonWriter writer, GroupData groupData, JsonSerializerOptions options )
	{
		var type = groupData.GetType();

		writer.WriteStartObject();

		WriteProperty( writer, JsonKeys.Class, type.Name, options );
		WriteProperty( writer, JsonKeys.Identifier, groupData.Identifier, options );

		SerializeObject( groupData, writer, options );

		writer.WriteEndObject();
	}

	private static void SerializeObject( object obj, Utf8JsonWriter writer, JsonSerializerOptions options, Dictionary<string, string> identifiers = null )
	{
		var type = obj.GetType();
		var properties = type.GetProperties( BindingFlags.Instance | BindingFlags.Public )
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

			var propertyValue = property.GetValue( obj );

			writer.WritePropertyName( propertyName );
			JsonSerializer.Serialize( writer, propertyValue, options );
		}

		if ( obj is IGraphNode node )
		{
			foreach ( var input in node.Inputs )
			{
				if ( input.ConnectedOutput is not { } output )
					continue;

				writer.WritePropertyName( input.Identifier );
				JsonSerializer.Serialize( writer, JsonSerializer.SerializeToNode( new NodeInput
				{
					Identifier = identifiers?.TryGetValue( output.Node.Identifier, out var newIdent ) ?? false ? newIdent : output.Node.Identifier,
					Output = output.Identifier,
				} ), options );
			}
		}
	}
}
