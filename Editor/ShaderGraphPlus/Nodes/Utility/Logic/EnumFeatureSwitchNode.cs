namespace ShaderGraphPlus.Nodes;

[Title( "Enum Combo Switch" ), Category( "Utility/Logic" ), Icon( "alt_route" )]
[InternalNode]
public sealed class EnumFeatureSwitchNode : ShaderNodePlus, BaseNodePlus.IInitializeNode, IParameterNode, IErroringNode
{
	[Hide, JsonIgnore, Browsable( false )]
	public override Color NodeTitleColor { get; set; } = ShaderGraphPlusTheme.NodeHeaderColors.LogicNode;

	[Hide, JsonIgnore, Browsable( false )]
	public override string Title => $"F_{_lastName.ToUpper().Replace( " ", "_" )}";

	[Hide, JsonIgnore, Browsable( false )]
	public string Name => $"F_{Feature.Name.ToUpper().Replace( " ", "_" )}";

	[Hide, Browsable( false )]
	public Guid ParameterIdentifier { get; set; }

	[Hide, JsonIgnore, Browsable( false )]
	public ShaderFeatureEnum Feature
	{
		get => GetFeature();
	}

	[Hide]
	private List<IPlugIn> InternalInputs = new();

	[Hide]
	public override IEnumerable<IPlugIn> Inputs => InternalInputs;

	[Hide, JsonIgnore]
	int _lastHashCodeInputs = 0;
	[Hide, JsonIgnore]
	string _lastName = "";

	public override void OnFrame()
	{
		var hashCodeInput = new HashCode();

		hashCodeInput.Add( Feature );

		var hc = hashCodeInput.ToHashCode();

		if ( hc != _lastHashCodeInputs )
		{
			_lastHashCodeInputs = hc;
			_lastName = Feature.Name;

			CreateInputs();
			Update();
		}
	}

	public void InitializeNode()
	{
		OnNodeCreated();
	}

	private void OnNodeCreated()
	{
		CreateInputs();
		Update();
	}

	public void CreateInputs()
	{
		var plugs = new List<IPlugIn>();

		if ( !Feature.IsValid )
			return;

		if ( Feature.Options == null )
		{
			InternalInputs = new();
		}
		else
		{
			foreach ( var option in Feature.Options )
			{
				if ( !option.IsValid ) continue;

				var info = new PlugInfo()
				{
					Id = option.Id,
					Name = option.Name,
					Type = typeof( object ),
					DisplayInfo = new()
					{
						Name = option.Name,
						Fullname = typeof( object ).FullName,
						Description = ""
					}
				};

				var plug = new BasePlugIn( this, info, info.Type );
				//var oldPlug = InternalInputs.FirstOrDefault( x => x is BasePlugIn plugIn && plugIn.Info.Name == info.Name ) as BasePlugIn;
				var oldPlug = InternalInputs.FirstOrDefault( x => x is BasePlugIn plugIn && plugIn.Info.Id == info.Id ) as BasePlugIn;
				if ( oldPlug is not null )
				{
					oldPlug.Info.Name = info.Name;
					oldPlug.Info.Type = info.Type;
					oldPlug.Info.DisplayInfo = info.DisplayInfo;

					if ( oldPlug.Type != plug.Type )
					{
						plugs.Add( plug );
					}
					else
					{
						plugs.Add( oldPlug );
					}
				}
				else
				{
					plugs.Add( plug );
				}
			}

			InternalInputs = plugs;
		}
	}

	private ShaderFeatureEnumParameter GetFeatureParameter()
	{
		if ( Graph is ShaderGraphPlus graph )
		{
			var parameter = graph.FindParameter<ShaderFeatureEnumParameter>( ParameterIdentifier );

			return parameter;
		}

		return new ShaderFeatureEnumParameter();
	}

	private ShaderFeatureEnum GetFeature()
	{
		var parameter = GetFeatureParameter();

		if ( parameter.IsValid )
		{
			var featureEnum = new ShaderFeatureEnum
			{
				Name = parameter.Name,
				Description = parameter.Description,
				HeaderName = parameter.HeaderName,
				Options = parameter.Options,
			};

			return featureEnum;
		}

		return new ShaderFeatureEnum();
	}

	[Output, Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		var inputs = new List<NodeInput>();

		foreach ( var input in Inputs )
		{
			if ( input.ConnectedOutput is null )
			{
				NodeInput nodeInput = default;

				inputs.Add( nodeInput );
			}
			else
			{
				NodeInput nodeInput = new NodeInput { Identifier = input.ConnectedOutput.Node.Identifier, Output = input.ConnectedOutput.Identifier };

				inputs.Add( nodeInput );
			}
		}

		var previewIndex = GetFeatureParameter().PreviewIndex;
		var result = compiler.ResultFeatureSwitch( inputs, Feature, previewIndex );

		return result.IsValid ? result : new NodeResult( ResultType.Float, $"1.0f" );
	};

	public List<string> GetErrors()
	{
		var errors = new List<string>();

		//foreach ( var option in Feature.Options )
		//{
		//	if ( string.IsNullOrWhiteSpace( option ) )
		//	{
		//		errors.Add( $"element \"{Feature.Options.IndexOf( option )}\" of feature \"{Feature.Name}\" cannot have a blank name!" );
		//	}
		//}

		return errors;
	}
}
