using System.Text.Json.Nodes;

namespace ShaderGraphPlus;

public partial class ShaderGraphPlus
{
	[SGPJsonUpgrader( typeof( ShaderGraphPlus ), 9 )]
	internal static void Upgrader_v9( JsonObject obj )
	{
		if ( obj[JsonKeys.ParameterArray] is not JsonArray oldParameterArray )
			throw new Exception( $"Cannot find jsonArray \'{JsonKeys.ParameterArray}\'" );

		if ( obj[JsonKeys.NodeArray] is not JsonArray oldNodeArray )
			throw new Exception( $"Cannot find jsonArray \'{JsonKeys.NodeArray}\'" );

		var isSubgraph = CheckIfSubgraph( obj );

		//
		// Upgrade Parameters
		//
		var newParameterArray = new JsonArray();

		foreach ( var jsonNode in oldParameterArray )
		{
			if ( jsonNode[JsonKeys.Class] is not JsonValue classValue )
				continue;
			
			var parameterElement = JsonSerializer.Deserialize<JsonElement>( jsonNode.AsObject().ToJsonString() );
			var typeName = classValue.GetValue<string>();

			var newParameterObj = jsonNode.DeepClone().AsObject();

			if ( isSubgraph && typeName == "SubgraphInput" || typeName == "SubgraphOutput" )
			{
				JsonUtils.UpdatePropertyKey( newParameterObj, typeName == "SubgraphInput" ? "InputDescription" : "OutputDescription", "Description" );

				newParameterArray.Add( newParameterObj );
			}
			else
			{
				newParameterArray.Add( jsonNode.DeepClone() );
			}
		}

		obj.Remove( JsonKeys.ParameterArray );
		obj.Add( JsonKeys.ParameterArray, newParameterArray );
	}
}
