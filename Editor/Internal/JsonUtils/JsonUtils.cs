using System.Text.Json.Nodes;

#nullable enable

namespace JsonUtilities;

/// <summary>
/// Helper functions to assist with json stuff.
/// </summary>
public static class JsonUtils
{
	private static JsonSerializerOptions SerializerOptions( bool indented = false )
	{
		var options = new JsonSerializerOptions
		{
			WriteIndented = indented,
			PropertyNameCaseInsensitive = true,
			NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
			DefaultIgnoreCondition = JsonIgnoreCondition.Never,
			ReadCommentHandling = JsonCommentHandling.Skip,
		};

		options.Converters.Add( new JsonStringEnumConverter( null, true ) );

		return options;
	}

	public static bool RenamePropertyKey( JsonObject obj, string oldKey, string newKey )
	{
		if ( obj.TryGetPropertyValue( oldKey, out var jsonNode ) )
		{
			var oldKeyIndex = obj.IndexOf( oldKey );

			obj.Remove( oldKey );
			obj.Insert( oldKeyIndex, newKey, jsonNode );

			return true;
		}

		return false;
	}

	public static bool UpdatePropertyValue<T>( JsonObject obj, string targetKey, T newData, JsonSerializerOptions? options = null )
	{
		var targetKeyIndex = obj.IndexOf( targetKey );

		if ( obj.Remove( targetKey ) )
		{
			obj.Insert( targetKeyIndex, targetKey, JsonSerializer.SerializeToNode( newData, options ?? SerializerOptions() ) );

			return true;
		}

		return false;
	}

#nullable disable

	public static bool GetPropertyValue<T>( JsonObject obj, string targetKey, JsonSerializerOptions options, T defaultValue, out T data )
	{
		data = defaultValue;

		if ( obj.TryGetPropertyValue( targetKey, out var jsonNode ) )
		{
			data = JsonSerializer.Deserialize<T>( jsonNode, options ?? SerializerOptions() );

			return true;
		}

		return false;
	}


	public static bool CopyPropertyValueToNewProperty( JsonObject obj, string oldKey, string newKey )
	{
		if ( obj.TryGetPropertyValue( oldKey, out var jsonNode ) )
		{
			obj.Remove( oldKey );
			obj.Add( newKey, jsonNode.DeepClone() );

			return true;
		}

		return false;
	}
}
