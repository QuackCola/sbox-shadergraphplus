namespace ShaderGraphPlus;

public class ClassBlackboardParameterType : IBlackboardParameterType
{
	public virtual string Identifier => Type.FullName;
	public TypeDescription Type { get; }

	public DisplayInfo DisplayInfo { get; protected set; }

	public ClassBlackboardParameterType( TypeDescription type )
	{
		Type = type;

		if ( Type is not null )
			DisplayInfo = DisplayInfo.ForType( Type.TargetType );
		else
			DisplayInfo = new DisplayInfo();
	}

	public virtual IBlackboardParameter CreateParameter( INodeGraph graph, string name = "" )
	{
		var sg = graph as ShaderGraphPlus;

		if ( string.IsNullOrWhiteSpace( name ) )
		{
			string baseName;
			if ( Type.TargetType.IsAssignableTo( typeof( IBlackboardSubgraphInputParameter ) ) )
			{
				baseName = "SubgraphInput";
			}
			else if ( Type.TargetType.IsAssignableTo( typeof( IBlackboardSubgraphOutputParameter ) ) )
			{
				baseName = "SubgraphOutput";
			}
			else if ( Type.TargetType == typeof( ShaderFeatureBooleanParameter ) || Type.TargetType == typeof( ShaderFeatureEnumParameter ) )
			{
				baseName = "ShaderFeature";
			}
			else
			{
				baseName = "MaterialParameter";
			}

			var id = 0;
			while ( sg.HasParameterWithName( $"{baseName}{id}" ) )
			{
				id++;
			}

			name = $"{baseName}{id}";
		}

		if ( EditorTypeLibrary.Create( Type.Name, Type.TargetType ) is BlackboardParameter parameter )
		{
			parameter.Name = name;
			parameter.Graph = graph;

			if ( parameter is ShaderFeatureEnumParameter shaderFeatureEnumParameter )
			{
				shaderFeatureEnumParameter.Options.Add( new ShaderFeatureEnumOption() { Name = "A" } );
				shaderFeatureEnumParameter.Options.Add( new ShaderFeatureEnumOption() { Name = "B" } );
			}

			return parameter;
		}
		else
		{
			throw new Exception( $"Failed to create parameter instance of type \"{Type.Name}\"" );
		}
	}
}
