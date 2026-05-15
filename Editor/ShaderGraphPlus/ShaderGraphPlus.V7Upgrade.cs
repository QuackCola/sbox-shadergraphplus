using ShaderGraphPlus.Nodes;
using System.Text.Json.Nodes;
using JsonUtilities;


namespace ShaderGraphPlus;

public partial class ShaderGraphPlus
{
	[SGPJsonUpgrader( typeof( ShaderGraphPlus ), 7 )]
	internal static void Upgrader_v7( JsonObject obj )
	{
		if ( obj[JsonKeys.ParameterArray] is not JsonArray oldParameterArray )
			throw new Exception( $"Cannot find jsonArray \'{JsonKeys.ParameterArray}\'" );

		if ( obj[JsonKeys.NodeArray] is not JsonArray oldNodeArray )
			throw new Exception( $"Cannot find jsonArray \'{JsonKeys.NodeArray}\'" );

		//
		// Upgrade Parameters
		//
		var newParameterArray = new JsonArray();

		if ( !CheckIfSubgraph( obj ) )
		{
			foreach ( var jsonNode in oldParameterArray )
			{
				if ( jsonNode[JsonKeys.Class] is not JsonValue classValue )
					continue;

				var parameterElement = JsonSerializer.Deserialize<JsonElement>( jsonNode.AsObject().ToJsonString() );
				var typeName = classValue.GetValue<string>();

				if ( typeName == "ShaderFeatureEnumParameter" )
				{
					var updatedParameterObject = jsonNode.DeepClone().AsObject();

					if ( updatedParameterObject.ContainsKey( "Options" ) )
					{
						var options = updatedParameterObject["Options"].Deserialize<List<string>>( SerializerOptions() );
						var newOptions = new List<ShaderFeatureEnumOption>();

						foreach ( var option in options )
						{
							newOptions.Add( new ShaderFeatureEnumOption() { Name = option } );
						}

						updatedParameterObject.Remove( "Options" );
						updatedParameterObject["Options"] = JsonSerializer.SerializeToNode( newOptions, SerializerOptions() );
					}
					else
					{
						updatedParameterObject["Options"] = JsonSerializer.SerializeToNode( new List<ShaderFeatureEnumOption>(), SerializerOptions() );
					}

					newParameterArray.Add( updatedParameterObject );
				}
				else
				{
					newParameterArray.Add( jsonNode.DeepClone() );
				}
			}
		}
		else
		{
			foreach ( var jsonNode in oldParameterArray )
			{
				if ( jsonNode[JsonKeys.Class] is not JsonValue classValue )
					continue;

				var typeName = classValue.GetValue<string>();
				var typeDesc = EditorTypeLibrary.GetType<BlackboardParameter>( typeName );
				var type = new ClassBlackboardParameterType( typeDesc );

				newParameterArray.Add( jsonNode.DeepClone() );
			}
		}

		//
		// Upgrade Nodes
		//

		var newNodeArray = new JsonArray();

		foreach ( var jsonNode in oldNodeArray )
		{
			if ( jsonNode[JsonKeys.Class] is not JsonValue classValue )
				continue;

			var typeName = classValue.GetValue<string>();
			var typeDesc = EditorTypeLibrary.GetType<BaseNodePlus>( typeName );
			var type = new ClassNodeType( typeDesc );

			if ( typeName == "SubgraphOutput" )
			{
				var updatedNodeObject = jsonNode.DeepClone().AsObject();

				if ( JsonUtils.GetPropertyValue<Guid>( updatedNodeObject, "OutputIdentifier", SerializerOptions(), Guid.NewGuid(), out Guid parameterIdentifier ) )
				{
					updatedNodeObject["ParameterIdentifier"] = JsonSerializer.SerializeToNode( parameterIdentifier, SerializerOptions() );
				}

				JsonUtils.GetPropertyValue<string>( updatedNodeObject, "OutputName", SerializerOptions(), "OutputA", out var outputName );
				JsonUtils.GetPropertyValue<string>( updatedNodeObject, "OutputDescription", SerializerOptions(), "", out var outputDescription );
				JsonUtils.GetPropertyValue<SubgraphPortType>( updatedNodeObject, "OutputType", SerializerOptions(), SubgraphPortType.Vector3, out var outputType );
				JsonUtils.GetPropertyValue<SubgraphOutputPreviewType>( updatedNodeObject, "Preview", SerializerOptions(), SubgraphOutputPreviewType.None, out var previewType );
				JsonUtils.GetPropertyValue<int>( updatedNodeObject, "PortOrder", SerializerOptions(), 0, out var portOrder );

				string parameterTypeName = outputType switch
				{
					SubgraphPortType.Bool => "BoolSubgraphOutputParameter",
					SubgraphPortType.Int => "IntSubgraphOutputParameter",
					SubgraphPortType.Float => "FloatSubgraphOutputParameter",
					SubgraphPortType.Vector2 => "Float2SubgraphOutputParameter",
					SubgraphPortType.Vector3 => "Float3SubgraphOutputParameter",
					SubgraphPortType.Vector4 => "Float4SubgraphOutputParameter",
					SubgraphPortType.Color => "ColorSubgraphOutputParameter",
					SubgraphPortType.Float2x2 => "Float2x2SubgraphOutputParameter",
					SubgraphPortType.Float3x3 => "Float3x3SubgraphOutputParameter",
					SubgraphPortType.Float4x4 => "Float4x4SubgraphOutputParameter",
					SubgraphPortType.Gradient => "GradientSubgraphOutputParameter",
					SubgraphPortType.SamplerState => "SamplerStateSubgraphOutputParameter",
					SubgraphPortType.Texture2DObject => "Texture2DSubgraphOutputParameter",
					SubgraphPortType.TextureCubeObject => "TextureCubeSubgraphOutputParameter",
					_ => throw new NotImplementedException( $"Unknown OutputType \'{outputType}\'" ),
				};

				var parameterObject = new JsonObject
				{
					{ JsonKeys.Class, JsonSerializer.SerializeToNode( parameterTypeName, SerializerOptions() ) },
					{ "Identifier", JsonSerializer.SerializeToNode( parameterIdentifier, SerializerOptions() ) },
					{ "Name", JsonSerializer.SerializeToNode( outputName, SerializerOptions() ) },
					{ "OutputDescription", JsonSerializer.SerializeToNode( outputDescription, SerializerOptions() ) },
					{ "PortOrder", JsonSerializer.SerializeToNode( portOrder, SerializerOptions() ) },
					{ "Preview", JsonSerializer.SerializeToNode( previewType, SerializerOptions() ) },
				};

				newParameterArray.Add( parameterObject );

				newNodeArray.Add( updatedNodeObject );
			}
			else
			{
				newNodeArray.Add( jsonNode.DeepClone() );
			}
		}

		obj.Remove( JsonKeys.ParameterArray );
		obj.Add( JsonKeys.ParameterArray, newParameterArray );

		obj.Remove( JsonKeys.NodeArray );
		obj.Add( JsonKeys.NodeArray, newNodeArray );
	}
}
