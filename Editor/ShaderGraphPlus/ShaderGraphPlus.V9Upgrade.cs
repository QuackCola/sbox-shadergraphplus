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

			var typeName = classValue.GetValue<string>();
			var typeDesc = EditorTypeLibrary.GetType<BlackboardParameter>( typeName );
			var type = new ClassBlackboardParameterType( typeDesc );

			var newParameterObj = jsonNode.DeepClone().AsObject();

			if ( isSubgraph & typeDesc.TargetType.IsAssignableTo( typeof( IBlackboardSubgraphParameter ) ) )
			{
				JsonUtils.UpdatePropertyKey( newParameterObj, typeDesc.TargetType.IsAssignableTo( typeof( IBlackboardSubgraphInputParameter ) ) ? "InputDescription" : "OutputDescription", "Description" );

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
