using System.Text.Json.Nodes;

namespace ShaderGraphPlus;

public partial class ShaderGraphPlus
{
	private int GetGraphVersion( JsonElement element, bool useCurrentVersion = false )
	{
		return useCurrentVersion ? Version : GetVersion( element );
	}

	/// <summary>
	/// Gets the version of the provided JsonElement. Returns 0 on failure.
	/// </summary>
	private static int GetVersion( JsonElement element )
	{
		if ( element.TryGetProperty( "__version", out var versionElement ) )
		{
			return versionElement.GetInt32();
		}
		else if ( element.TryGetProperty( nameof( Version ), out var oldVersionElement ) )
		{
			return oldVersionElement.GetInt32();
		}

		SGPLogger.Warning( $"JsonElement has no property named \"__version\" or \"Version\". Defaulting to 0...." );

		return 0;
	}

	private static JsonElement UpgradeJsonUpgradeable( int versionNumber, ISGPJsonUpgradeable jsonUpgradeable, Type type, JsonProperty jsonProperty, JsonSerializerOptions serializerOptions )
	{
		ArgumentNullException.ThrowIfNull( jsonUpgradeable );

		var jsonObject = JsonNode.Parse( jsonProperty.Value.GetRawText() ) as JsonObject;

		ShaderGraphPlusJsonUpgrader.Upgrade( versionNumber, jsonObject, type );

		return JsonSerializer.Deserialize<JsonElement>( jsonObject.ToJsonString(), serializerOptions );
	}

	private static bool CheckIfSubgraph( JsonObject obj )
	{
		return obj.TryGetPropertyValue( nameof( ShaderGraphPlus.IsSubgraph ), out var subgraphValue ) ? subgraphValue.GetValue<bool>() : false;
	}
}
