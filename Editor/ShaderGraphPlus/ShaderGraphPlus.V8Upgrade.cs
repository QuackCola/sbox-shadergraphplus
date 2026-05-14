using System.Text.Json.Nodes;

namespace ShaderGraphPlus;

public partial class ShaderGraphPlus
{
	[SGPJsonUpgrader( typeof( ShaderGraphPlus ), 8 )]
	internal static void Upgrader_v8( JsonObject obj )
	{
		static bool ParameterNameExists( string name, JsonArray parameterArray )
		{
			foreach ( var jsonNode in parameterArray )
			{
				if ( jsonNode["Name"] is not JsonValue nameValue )
					continue;

				if ( nameValue.GetValue<string>() == name )
				{
					return true;
				}
			}

			return false;
		}

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

			var typeName = classValue.GetValue<string>();
			var typeDesc = EditorTypeLibrary.GetType<BaseNodePlus>( typeName );
			var type = new ClassNodeType( typeDesc );

			if ( typeName == "SamplerNode" )
			{
				var updatedNodeObject = jsonNode.DeepClone().AsObject();

				if ( !isSubgraph && JsonUtils.UpdatePropertyValue( updatedNodeObject, JsonKeys.Class, "SamplerStateParameterNode", SerializerOptions() ) )
				{
				}

				if ( JsonUtils.GetPropertyValue<Sampler>( updatedNodeObject, "SamplerState", SerializerOptions(), new Sampler(), out Sampler sampler ) )
				{
					updatedNodeObject.Remove( "SamplerState" );
				}

				BlackboardParameter parameter;
				if ( isSubgraph )
				{
					parameter = new SamplerStateSubgraphInputParameter()
					{
						Identifier = Guid.NewGuid()
					};
				}
				else
				{
					parameter = new SamplerStateParameter()
					{
						Identifier = Guid.NewGuid()
					};
				}

				parameter.Name = sampler.Name;

				if ( string.IsNullOrWhiteSpace( parameter.Name ) )
				{
					var name = $"{(isSubgraph ? "SubgraphInput" : "MaterialParameter")}";
					var id = name;
					int count = 0;

					while ( ParameterNameExists( id, newParameterArray ) )
					{
						id = $"{name}_{count++}";
					}

					parameter.Name = id;
				}

				parameter.SetValue( sampler );

				updatedNodeObject.Add( "ParameterIdentifier", parameter.Identifier );

				var parameterType = parameter.GetType();
				var parameterObject = new JsonObject { { JsonKeys.Class, parameterType.Name } };

				SerializeObject( parameter, parameterObject, SerializerOptions() );

				newParameterArray.Add( parameterObject );

				if ( isSubgraph )
				{
					JsonUtils.GetPropertyValue<string>( updatedNodeObject, "Identifier", SerializerOptions(), null, out var nodeID );

					var subgraphInput = new SubgraphInput()
					{
						Identifier = nodeID,
						ParameterIdentifier = parameter.Identifier
					};

					var newNodeObject = new JsonObject { { JsonKeys.Class, "SubgraphInput" } };

					SerializeObject( subgraphInput, newNodeObject, SerializerOptions(), null );

					newNodeArray.Add( newNodeObject );
				}
				else
				{
					newNodeArray.Add( updatedNodeObject );
				}
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
