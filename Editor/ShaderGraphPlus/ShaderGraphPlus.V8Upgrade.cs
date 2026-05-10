using JsonUtilities;
using Sandbox.Rendering;
using ShaderGraphPlus.Nodes;
using System.Text.Json.Nodes;


namespace ShaderGraphPlus;

public partial class ShaderGraphPlus
{
	[SGPJsonUpgrader( typeof( ShaderGraphPlus ), 8 )]
	internal static void Upgrader_v8( JsonObject obj )
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

			newParameterArray.Add( jsonNode.DeepClone() );
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

			if ( !isSubgraph && typeName == "SamplerNode" )
			{
				var updatedNodeObject = jsonNode.DeepClone().AsObject();

				if ( JsonUtils.UpdatePropertyValue( updatedNodeObject, JsonKeys.Class, "SamplerStateParameterNode", SerializerOptions() ) )
				{
				}

				if ( JsonUtils.GetPropertyValue<Sampler>( updatedNodeObject, "SamplerState", SerializerOptions(), new Sampler(), out Sampler sampler ) )
				{
					updatedNodeObject.Remove( "SamplerState" );
				}

				var parameter = new SamplerStateParameter()
				{
					Identifier = Guid.NewGuid()
				};

				updatedNodeObject.Add( "ParameterIdentifier", parameter.Identifier );

				parameter.Name = !string.IsNullOrWhiteSpace( sampler.Name ) ? sampler.Name : "MaterialSampler0";
				parameter.Value = sampler;

				var parameterType = parameter.GetType();
				var parameterObject = new JsonObject { { JsonKeys.Class, parameterType.Name } };

				SerializeObject( parameter, parameterObject, SerializerOptions() );

				newParameterArray.Add( parameterObject );

				newNodeArray.Add( updatedNodeObject );
			}
			else if ( isSubgraph && typeName == "SamplerNode" )
			{
				throw new NotImplementedException( "TODO" );
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
