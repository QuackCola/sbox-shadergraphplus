using System.Text.Json.Nodes;

namespace ShaderGraphPlus;

public partial class ShaderGraphPlus
{
	[SGPJsonUpgrader( typeof( ShaderGraphPlus ), 5 )]
	internal static void Upgrader_v5( JsonObject obj )
	{
		if ( obj[JsonKeys.NodeArray] is not JsonArray oldNodeArray )
			return;

		//var identifiers = new Dictionary<string, string>();
		//foreach ( var node in oldNodeArray )
		//{
		//	if ( node[nameof( BaseNodePlus.Identifier )] is not JsonValue identifierValue )
		//		continue;
		//
		//	identifiers.Add( identifierValue.GetValue<string>(), $"{identifiers.Count}" );
		//}

		var newNodeArray = new JsonArray();

		foreach ( var jsonNode in oldNodeArray )
		{
			if ( jsonNode[JsonKeys.Class] is not JsonValue classValue )
				continue;

			var typeName = classValue.GetValue<string>();
			var typeDesc = EditorTypeLibrary.GetType<BaseNodePlus>( typeName );
			var type = new ClassNodeType( typeDesc );

			if ( ShouldUpgradeParameterNode_v5( typeName ) )
			{
				var updatedNodeObject = jsonNode.DeepClone().AsObject();

				if ( updatedNodeObject.ContainsKey( "BlackboardParameterIdentifier" ) )
				{
					var parameterIdentifier = updatedNodeObject["BlackboardParameterIdentifier"].Deserialize<Guid>( SerializerOptions() );
					updatedNodeObject.Remove( "BlackboardParameterIdentifier" );
					updatedNodeObject["ParameterIdentifier"] = parameterIdentifier;

					newNodeArray.Add( updatedNodeObject );
				}
				else
				{
					throw new Exception( $"Cannot find Json Key with the name : 'BlackboardParameterIdentifier'" );
				}
			}
			else if ( typeName == "SwitchNode" )
			{
				var updatedNodeObject = jsonNode.DeepClone().AsObject();

				updatedNodeObject["_class"] = "BranchNode";

				newNodeArray.Add( updatedNodeObject );
			}
			else
			{
				newNodeArray.Add( jsonNode.DeepClone() );
			}
		}

		obj.Remove( JsonKeys.NodeArray );
		obj.Add( JsonKeys.NodeArray, newNodeArray );
	}

	private static bool ShouldUpgradeParameterNode_v5( string typeName )
	{

		if ( typeName == "BoolParameterNode" ||
			 typeName == "IntParameterNode" ||
			 typeName == "FloatParameterNode" ||
			 typeName == "Float2ParameterNode" ||
			 typeName == "Float3ParameterNode" ||
			 typeName == "Float4ParameterNode" ||
			 typeName == "ColorParameterNode" ||
			 typeName == "Texture2DParameterNode" ||
			 typeName == "TextureCubeParameterNode" ||
			 typeName == "TextureCubeParameterNode" ||
			 typeName == "SubgraphInput" )
		{
			return true;
		}

		return false;
	}
}
