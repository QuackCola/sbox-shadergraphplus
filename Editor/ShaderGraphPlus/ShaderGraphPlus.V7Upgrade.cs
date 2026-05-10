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

			var nodeElement = JsonSerializer.Deserialize<JsonElement>( jsonNode.AsObject().ToJsonString() );
			var typeName = classValue.GetValue<string>();

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

				IBlackboardSubgraphOutputParameter parameter = outputType switch
				{
					SubgraphPortType.Bool => new BoolSubgraphOutputParameter()
					{
						Identifier = parameterIdentifier
					},
					SubgraphPortType.Int => new IntSubgraphOutputParameter()
					{
						Identifier = parameterIdentifier
					},
					SubgraphPortType.Float => new FloatSubgraphOutputParameter()
					{
						Identifier = parameterIdentifier
					},
					SubgraphPortType.Vector2 => new Float2SubgraphOutputParameter()
					{
						Identifier = parameterIdentifier
					},
					SubgraphPortType.Vector3 => new Float3SubgraphOutputParameter()
					{
						Identifier = parameterIdentifier
					},
					SubgraphPortType.Vector4 => new Float4SubgraphOutputParameter()
					{
						Identifier = parameterIdentifier
					},
					SubgraphPortType.Color => new ColorSubgraphOutputParameter()
					{
						Identifier = parameterIdentifier
					},
					SubgraphPortType.Float2x2 => new Float2x2SubgraphOutputParameter()
					{
						Identifier = parameterIdentifier
					},
					SubgraphPortType.Float3x3 => new Float3x3SubgraphOutputParameter()
					{
						Identifier = parameterIdentifier
					},
					SubgraphPortType.Float4x4 => new Float4x4SubgraphOutputParameter()
					{
						Identifier = parameterIdentifier
					},
					SubgraphPortType.Gradient => new GradientSubgraphOutputParameter()
					{
						Identifier = parameterIdentifier
					},
					SubgraphPortType.SamplerState => new SamplerStateSubgraphOutputParameter()
					{
						Identifier = parameterIdentifier
					},
					SubgraphPortType.Texture2DObject => new Texture2DSubgraphOutputParameter()
					{
						Identifier = parameterIdentifier
					},
					SubgraphPortType.TextureCubeObject => new TextureCubeSubgraphOutputParameter()
					{
						Identifier = parameterIdentifier
					},
					_ => throw new NotImplementedException( $"Unknown OutputType \'{outputType}\'" ),
				};

				parameter.Name = outputName;
				parameter.OutputDescription = outputDescription;
				parameter.Preview = previewType;
				parameter.PortOrder = portOrder;

				var parameterType = parameter.GetType();
				var parameterObject = new JsonObject { { JsonKeys.Class, parameterType.Name } };

				SerializeObject( parameter, parameterObject, SerializerOptions() );

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
