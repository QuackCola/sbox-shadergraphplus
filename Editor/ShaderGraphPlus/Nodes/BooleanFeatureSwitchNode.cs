namespace ShaderGraphPlus.Nodes;

[Title( "Boolean Combo Switch" ), Category( "Utility/Logic" ), Icon( "alt_route" )]
[InternalNode]
public sealed class BooleanFeatureSwitchNode : ShaderNodePlus, IParameterNode, IBlackboardNode
{
	[Hide, JsonIgnore, Browsable( false )]
	public override Color NodeTitleColor { get; set; } = ShaderGraphPlusTheme.NodeHeaderColors.LogicNode;

	[Hide, JsonIgnore, Browsable( false )]
	public override string Title => $"F_{Feature.Name.ToUpper().Replace( " ", "_" )}";

	[Hide, JsonIgnore, Browsable( false )]
	public string Name => $"F_{Feature.Name.ToUpper().Replace( " ", "_" )}";

	[Hide, Browsable( false )]
	public Guid ParameterIdentifier { get; set; }

	[Hide, JsonIgnore, Browsable( false )]
	public ShaderFeatureBoolean Feature => GetFeature();

	[Input, Hide]
	[Title( "True" )]
	public NodeInput InputTrue { get; set; }

	[Input, Hide]
	[Title( "False" )]
	public NodeInput InputFalse { get; set; }

	private ShaderFeatureBooleanParameter GetFeatureParameter()
	{
		if ( Graph is ShaderGraphPlus graph )
		{
			var parameter = graph.FindParameter<ShaderFeatureBooleanParameter>( ParameterIdentifier );

			return parameter;
		}

		return new ShaderFeatureBooleanParameter();
	}

	private ShaderFeatureBoolean GetFeature()
	{
		var parameter = GetFeatureParameter();

		if ( parameter.IsValid )
		{
			var featureBoolean = new ShaderFeatureBoolean
			{
				Name = parameter.Name,
				Description = parameter.Description,
				HeaderName = parameter.HeaderName,
			};

			return featureBoolean;
		}

		return new ShaderFeatureBoolean();
	}

	[Output, Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		var inputs = new List<NodeInput>
		{
			InputTrue,
			InputFalse
		};

		var preview = GetFeatureParameter().Preview;
		var result = compiler.ResultFeatureSwitch( inputs, Feature, preview ? 1 : 0 );

		return result.IsValid ? result : new NodeResult( ResultType.Float, $"1.0f" );
	};
}
