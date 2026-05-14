using ShaderGraphPlus.Nodes;
using System.Text.Json.Nodes;

namespace ShaderGraphPlus;

public partial class ShaderGraphPlus
{
	[SGPJsonUpgrader( typeof( ShaderGraphPlus ), 6 )]
	internal static void Upgrader_v6( JsonObject obj )
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

			if ( typeName == "CustomFunctionNode" )
			{
				var updatedNodeObject = jsonNode.DeepClone().AsObject();

				if ( updatedNodeObject.ContainsKey( "Type" ) )
				{
					var customFunctionType = updatedNodeObject["Type"].Deserialize<string>( SerializerOptions() );

					if ( customFunctionType == "Inline" )
					{
						updatedNodeObject.Remove( "Type" );
						updatedNodeObject["Mode"] = "Generate";
					}
				}
				else
				{
					updatedNodeObject["Mode"] = "Generate";
				}

				JsonUtils.UpdatePropertyKey( updatedNodeObject, "Body", "Code" );
				JsonUtils.UpdatePropertyKey( updatedNodeObject, "ExpressionInputs", "FunctionInputs" );
				JsonUtils.UpdatePropertyKey( updatedNodeObject, "ExpressionOutputs", "FunctionOutputs" );

				if ( updatedNodeObject.ContainsKey( "PixelStageOnly" ) )
				{
					var pixelStageOnly = updatedNodeObject["PixelStageOnly"].Deserialize<bool>( SerializerOptions() );

					if ( pixelStageOnly )
					{
						updatedNodeObject.Remove( "PixelStageOnly" );
						updatedNodeObject["CompatableStage"] = JsonSerializer.SerializeToNode( CompatableShaderStage.Pixel, SerializerOptions() );
					}
				}
				else
				{
					updatedNodeObject["CompatableStage"] = JsonSerializer.SerializeToNode( CompatableShaderStage.Pixel, SerializerOptions() );
				}

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
}
