namespace ShaderGraphPlus;

public class ClassBlackboardParameterType : IBlackboardParameterType
{
	public virtual string Identifier => Type.FullName;
	public TypeDescription Type { get; }

	public DisplayInfo DisplayInfo { get; protected set; }

	protected virtual string DefaultBaseName => "Parameter";

	public ClassBlackboardParameterType( TypeDescription type )
	{
		Type = type;

		if ( Type is not null )
			DisplayInfo = DisplayInfo.ForType( Type.TargetType );
		else
			DisplayInfo = new DisplayInfo();
	}

	private string CheckName( ShaderGraphPlus graph, string name )
	{
		if ( string.IsNullOrWhiteSpace( name ) )
		{
			var id = 0;
			while ( graph.HasParameterWithName( $"{DefaultBaseName}{id}" ) )
			{
				id++;
			}

			return $"{DefaultBaseName}{id}";
		}
		else
		{
			return name;
		}
	}

	public virtual IBlackboardParameter CreateParameter( INodeGraph graph, string name = "" )
	{
		var sg = graph as ShaderGraphPlus;

		var parameter = Type.Create<BlackboardParameter>();
		parameter.Name = CheckName( sg, name );
		parameter.Graph = sg;

		return parameter;
	}
}

public sealed class MaterialParameterType : ClassBlackboardParameterType
{
	protected override string DefaultBaseName => "MaterialParameter";

	public MaterialParameterType( TypeDescription type ) : base( type )
	{
	}
}

public sealed class SubgraphParameterType : ClassBlackboardParameterType
{
	protected override string DefaultBaseName => Type.TargetType.IsAssignableTo( typeof( IBlackboardSubgraphInputParameter ) ) ? "SubgraphInput" : "SubgraphOutput";

	public SubgraphParameterType( TypeDescription type ) : base( type )
	{
	}
}

public sealed class ShaderFeatureParameterType : ClassBlackboardParameterType
{
	protected override string DefaultBaseName => "ShaderFeature";

	public ShaderFeatureParameterType( TypeDescription type ) : base( type )
	{
	}

	public override IBlackboardParameter CreateParameter( INodeGraph graph, string name = "" )
	{
		var sg = graph as ShaderGraphPlus;
		var parameter = base.CreateParameter( graph );

		if ( parameter is ShaderFeatureEnumParameter shaderFeatureEnum )
		{
			shaderFeatureEnum.Options.Add( new ShaderFeatureEnumOption() { Name = "A" } );
			shaderFeatureEnum.Options.Add( new ShaderFeatureEnumOption() { Name = "B" } );
		}

		return parameter;
	}
}
