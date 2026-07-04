namespace ShaderGraphPlus;

public interface IParameterNode
{
	Guid ParameterIdentifier { get; set; }
	string Name { get; }
}

public abstract class ParameterNode<T, Y> : BlackboardNode<Y>, IParameterNode where Y : BlackboardParameter
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.ParameterNode;

	[JsonIgnore, Hide, Browsable( false )]
	public override bool AutoSize => true;

	[Hide]
	protected bool IsSubgraph => (Graph is ShaderGraphPlus shaderGraph && shaderGraph.IsSubgraph);

	[JsonIgnore, Hide, Browsable( false )]
	public override string Title => string.IsNullOrWhiteSpace( Name ) ?
		$"{DisplayInfo.For( this ).Name}" :
		$"{Name}";

	[Hide]
	public string Name => GetParameter().Name;

	[Hide, JsonIgnore]
	public T Value
	{
		get => (T)GetParameter().GetValue();
		set
		{
			if ( Graph is ShaderGraphPlus graph )
			{
				graph.UpdateParameterValue( ParameterIdentifier, value );

				Update();
			}
		}
	}

	[Hide, JsonIgnore]
	int _lastNameHashCode = 0;

	public ParameterNode()
	{
		ExpandSize = new Vector2( 56, 0 );
	}

	public override void OnFrame()
	{
		var nameHashCode = 0;
		nameHashCode += GetParameter().Name.GetHashCode();

		if ( nameHashCode != _lastNameHashCode )
		{
			_lastNameHashCode = nameHashCode;
			Update();
		}
	}

	protected NodeResult Component( string component, float value, GraphCompiler compiler )
	{
		if ( compiler.IsPreview )
			return compiler.ResultValue( value );

		var result = compiler.Result( new NodeInput { Identifier = Identifier, Output = nameof( Result ) } );
		return new( ResultType.Float, $"{result}.{component}", true );
	}
}
