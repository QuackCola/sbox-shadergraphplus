
using System.Text.Json.Nodes;
using static ShaderGraphPlus.ShaderGraphPlus;

namespace ShaderGraphPlus;

public sealed partial class ShaderTemplateResource
{
	/// <summary>
	/// Gets the version of the provided JsonElement. Returns -1 on failure.
	/// </summary>
	private static int GetVersion( JsonElement element )
	{
		if ( element.TryGetProperty( "__version", out var versionElement ) )
		{
			return versionElement.GetInt32();
		}

		return -1;
	}

	public string Serialize()
	{
		var doc = new JsonObject();
		var options = SerializerOptions( true );

		SerializeObject( this, doc, options );

		doc.Add( "__version", JsonSerializer.SerializeToNode( ResourceVersion, options ) );

		return doc.ToJsonString( options );
	}

	public bool Deserialize( string json, string fileName = "" )
	{
		using var doc = JsonDocument.Parse( json );
		var root = doc.RootElement;
		var options = SerializerOptions();
		var fileVersion = GetVersion( root );

		if ( fileVersion < ResourceVersion )
			return false;

		DeserializeObject( this, root, options );

		return true;
	}

	private static void SerializeObject( object obj, JsonObject doc, JsonSerializerOptions options, Dictionary<string, string> identifiers = null )
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

			if ( property.IsDefined( typeof( JsonIgnoreAttribute ) ) )
				continue;

			var propertyName = property.Name;
			if ( property.GetCustomAttribute<JsonPropertyNameAttribute>() is { } jpna )
				propertyName = jpna.Name;

			var propertyValue = property.GetValue( obj );

			if ( propertyName != "ResourceVersion" )
			{
				doc.Add( propertyName, JsonSerializer.SerializeToNode( propertyValue, options ) );
			}
		}
	}

	private static void DeserializeObject( object obj, JsonElement doc, JsonSerializerOptions options )
	{
		var type = obj.GetType();
		var properties = type.GetProperties( BindingFlags.Instance | BindingFlags.Public )
			.Where( x => x.GetSetMethod() != null );

		foreach ( var jsonProperty in doc.EnumerateObject() )
		{
			var propertyInfo = properties.FirstOrDefault( x =>
			{
				var propName = x.Name;

				if ( x.GetCustomAttribute<JsonPropertyNameAttribute>() is JsonPropertyNameAttribute jpna )
					propName = jpna.Name;

				return string.Equals( propName, jsonProperty.Name, StringComparison.OrdinalIgnoreCase );
			} );

			if ( propertyInfo == null )
				continue;

			if ( propertyInfo.CanWrite == false )
				continue;

			if ( propertyInfo.IsDefined( typeof( JsonIgnoreAttribute ) ) )
				continue;

			propertyInfo.SetValue( obj, JsonSerializer.Deserialize( jsonProperty.Value.GetRawText(), propertyInfo.PropertyType, options ) );
		}
	}
}
