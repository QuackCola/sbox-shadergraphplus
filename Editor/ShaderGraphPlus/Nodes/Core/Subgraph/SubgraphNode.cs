using Editor;
using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlus;

public sealed class SubgraphNode : ShaderNodePlus, IErroringNode, IWarningNode
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.SubgraphNode;

	[JsonIgnore, Hide, Browsable( false )]
	public override bool AutoSize => true;

	[Hide]
	public bool IsSubgraph => (Graph is ShaderGraphPlus shaderGraph && shaderGraph.IsSubgraph);

	[Hide]
	public string SubgraphPath { get; set; }

	[Hide, JsonIgnore]
	public ShaderGraphPlus Subgraph { get; set; }

	[Hide]
	private List<IPlugIn> InternalInputs = new();

	[Hide]
	public override IEnumerable<IPlugIn> Inputs => InternalInputs;

	[Hide]
	private List<IPlugOut> InternalOutputs = new();

	[Hide]
	public override IEnumerable<IPlugOut> Outputs => InternalOutputs;

	[JsonIgnore, Hide]
	public override bool CanPreview => false;

	[Editor( "subgraphplus.defaultvalues" ), WideMode( HasLabel = false )]
	public Dictionary<string, object> DefaultValues { get; set; } = new();

	[Hide]
	public override DisplayInfo DisplayInfo => new()
	{
		Name = Subgraph?.Title ?? (string.IsNullOrEmpty( Subgraph.Title ) ? "Untitled Subgraph" : Subgraph.Title),
		Description = Subgraph?.Description ?? "",
		Icon = Subgraph?.Icon ?? ""
	};

	public void OnNodeCreated()
	{
		if ( Subgraph is not null ) return;

		if ( SubgraphPath != null )
		{
			Subgraph = new ShaderGraphPlus();
			if ( !Editor.FileSystem.Content.FileExists( SubgraphPath ) ) return;
			var json = Editor.FileSystem.Content.ReadAllText( SubgraphPath );
			Subgraph.Deserialize( json, SubgraphPath, SubgraphPath );
			Subgraph.Path = SubgraphPath;

			CreateInputs();
			CreateOutputs();

			Update();
		}
	}

	[Hide, JsonIgnore]
	internal Dictionary<IPlugIn, (IBlackboardSubgraphInputParameter inputParameter, Type inputValueType)> InputReferences = new();

	public void CreateInputs()
	{
		var plugs = new List<IPlugIn>();
		var defaults = new Dictionary<Type, int>();
		InputReferences.Clear();

		// Get all Subgraph input parameters
		var subgraphInputs = Subgraph.Parameters.OfType<IBlackboardSubgraphInputParameter>()
			.Where( x => !string.IsNullOrWhiteSpace( x.Name ) );

		foreach ( var subgraphInput in subgraphInputs )
		{
			var inputType = subgraphInput.PortType switch
			{
				SubgraphPortType.Bool => typeof( bool ),
				SubgraphPortType.Int => typeof( int ),
				SubgraphPortType.Float => typeof( float ),
				SubgraphPortType.Vector2 => typeof( Vector2 ),
				SubgraphPortType.Vector3 => typeof( Vector3 ),
				SubgraphPortType.Vector4 => typeof( Vector4 ),
				SubgraphPortType.Color => typeof( Color ),
				SubgraphPortType.Float2x2 => typeof( Float2x2 ),
				SubgraphPortType.Float3x3 => typeof( Float3x3 ),
				SubgraphPortType.Float4x4 => typeof( Float4x4 ),
				SubgraphPortType.Gradient => typeof( Gradient ),
				SubgraphPortType.SamplerState => typeof( Sampler ),
				SubgraphPortType.Texture2DObject => typeof( Texture ),
				SubgraphPortType.TextureCubeObject => typeof( Texture ),
				_ => throw new NotImplementedException( $"Unknown PortType \'{subgraphInput.PortType}\'" )
			};

			var info = new PlugInfo()
			{
				Name = subgraphInput.Name,
				Type = inputType,
				DisplayInfo = new DisplayInfo()
				{
					Name = subgraphInput.Name,
					Fullname = inputType.FullName
				}
			};

			var plug = new BasePlugIn( this, info, inputType );
			var oldPlug = InternalInputs.FirstOrDefault( x => x is BasePlugIn plugIn && plugIn.Info.Name == info.Name && plugIn.Info.Type == info.Type ) as BasePlugIn;
			if ( oldPlug is not null )
			{
				oldPlug.Info.Name = info.Name;
				oldPlug.Info.Type = info.Type;
				oldPlug.Info.DisplayInfo = info.DisplayInfo;
				plug = oldPlug;
			}
			plugs.Add( plug );
			InputReferences[plug] = (subgraphInput, inputType);

			if ( !DefaultValues.ContainsKey( plug.Identifier ) )
			{
				DefaultValues[plug.Identifier] = subgraphInput.GetValue();
			}
		}

		InternalInputs = plugs;
	}

	public void CreateOutputs()
	{
		var plugs = new List<IPlugOut>();

		// Get all Subgraph output parameters
		var subgraphOutputs = Subgraph.Parameters.OfType<IBlackboardSubgraphOutputParameter>()
			.Where( x => !string.IsNullOrWhiteSpace( x.Name ) );

		foreach ( var subgraphOutput in subgraphOutputs )
		{
			var outputType = subgraphOutput.PortType switch
			{
				SubgraphPortType.Bool => typeof( bool ),
				SubgraphPortType.Int => typeof( int ),
				SubgraphPortType.Float => typeof( float ),
				SubgraphPortType.Vector2 => typeof( Vector2 ),
				SubgraphPortType.Vector3 => typeof( Vector3 ),
				SubgraphPortType.Vector4 => typeof( Vector4 ),
				SubgraphPortType.Color => typeof( Color ),
				SubgraphPortType.Float2x2 => typeof( Float2x2 ),
				SubgraphPortType.Float3x3 => typeof( Float3x3 ),
				SubgraphPortType.Float4x4 => typeof( Float4x4 ),
				SubgraphPortType.Gradient => typeof( Gradient ),
				SubgraphPortType.SamplerState => typeof( Sampler ),
				SubgraphPortType.Texture2DObject => typeof( Texture ),
				SubgraphPortType.TextureCubeObject => typeof( Texture ),
				_ => throw new NotImplementedException( $"Unknown PortType \'{subgraphOutput.PortType}\'" )
			};

			if ( outputType is null ) continue;
			var info = new PlugInfo()
			{
				Name = subgraphOutput.Name,
				Type = outputType,
				DisplayInfo = new DisplayInfo()
				{
					Name = subgraphOutput.Name,
					Fullname = outputType.FullName,
					Description = subgraphOutput.Description
				}
			};

			var plug = new BasePlugOut( this, info, outputType );
			var oldPlug = InternalOutputs.FirstOrDefault( x => x is BasePlugOut plugOut && plugOut.Info.Name == info.Name && plugOut.Info.Type == info.Type ) as BasePlugOut;

			if ( oldPlug is not null )
			{
				oldPlug.Info.Name = info.Name;
				oldPlug.Info.Type = info.Type;
				oldPlug.Info.DisplayInfo = info.DisplayInfo;

				plugs.Add( oldPlug );
			}
			else
			{
				plugs.Add( plug );
			}
		}

		InternalOutputs = plugs;
	}

	public List<string> GetWarnings()
	{
		var warnings = new List<string>();

		foreach ( var node in Subgraph.Nodes )
		{
			if ( node is IWarningNode warningNode )
			{
				warnings.AddRange( warningNode.GetWarnings().Select( x => $"[{DisplayInfo.Name}] {x}" ) );
			}
		}

		return warnings;
	}

	public List<string> GetErrors()
	{
		OnNodeCreated();
		if ( Subgraph is null )
		{
			return new List<string> { $"Cannot find subgraph at \"{SubgraphPath}\"" };
		}

		var errors = new List<string>();

		foreach ( var node in Subgraph.Nodes )
		{
			if ( node is IErroringNode erroringNode )
			{
				errors.AddRange( erroringNode.GetErrors().Select( x => $"[{DisplayInfo.Name}] {x}" ) );
			}
		}

		foreach ( var input in InputReferences )
		{
			var plug = input.Key;
			var parameterNode = input.Value.inputParameter;
			var inputName = parameterNode.Name;
			if ( string.IsNullOrWhiteSpace( inputName ) ) inputName = input.Key.DisplayInfo.Name;
			if ( IsSubgraph && plug.Type == typeof( Texture ) && plug.ConnectedOutput is null )
			{
				errors.Add( $"Required Input \"{inputName}\" is missing on Node \"{Subgraph.Title}\"" );
			}
			if ( parameterNode.IsRequired && plug.ConnectedOutput is null )
			{
				errors.Add( $"Required Input \"{inputName}\" is missing on Node \"{Subgraph.Title}\"" );
			}
		}

		return errors;
	}

	public override void OnDoubleClick( MouseEvent e )
	{
		base.OnDoubleClick( e );

		if ( string.IsNullOrEmpty( SubgraphPath ) ) return;

		var shader = AssetSystem.FindByPath( SubgraphPath );
		if ( shader is null ) return;

		shader.OpenInEditor();
	}

}

[CustomEditor( typeof( Dictionary<string, object> ), NamedEditor = "subgraphplus.defaultvalues", WithAllAttributes = [typeof( WideModeAttribute )] )]
internal class SubgraphNodeControlWidget : ControlWidget
{
	public override bool SupportsMultiEdit => false;

	SubgraphNode Node;
	ControlSheet Sheet;

	public SubgraphNodeControlWidget( SerializedProperty property ) : base( property )
	{
		Node = property.Parent.Targets.First() as SubgraphNode;

		Layout = Layout.Column();
		Layout.Spacing = 2;
		Sheet = new ControlSheet();
		Layout.Add( Sheet );

		Rebuild();
	}

	protected override void OnPaint()
	{

	}

	private void Rebuild()
	{
		Sheet.Clear( true );

		var groups = new Dictionary<string, List<SerializedProperty>>();

		foreach ( var inputRef in Node.InputReferences )
		{
			var name = inputRef.Key.Identifier;
			var type = inputRef.Value.inputValueType;
			var groupName = inputRef.Value.inputParameter.GetGroupTitle();

			if ( !groups.ContainsKey( groupName ) )
			{
				groups.Add( groupName, new List<SerializedProperty>() );
			}

			var inputType = inputRef.Value.inputParameter.PortType;
			var getter = () =>
			{
				if ( Node.DefaultValues.ContainsKey( name ) )
				{
					return Node.DefaultValues[name];
				}
				else
				{
					var val = inputRef.Value.inputParameter.GetValue();
					if ( val is JsonElement el ) return el.GetDouble();
					return val;
				}
			};

			var attributes = new List<Attribute>();
			var displayName = $"Default {name}";

			SerializedProperty property = null;

			if ( type == typeof( bool ) )
			{
				property = TypeLibrary.CreateProperty<bool>(
					displayName, () =>
					{
						var val = getter();

						if ( val is JsonElement el )
						{
							return bool.Parse( el.GetRawText() );
						}

						return (bool)val;
					}, x => SetDefaultValue( name, x ),
					attributes.ToArray()
				);
			}
			else if ( type == typeof( int ) )
			{
				property = TypeLibrary.CreateProperty<int>(
					displayName, () =>
					{
						var val = getter();

						if ( val is JsonElement el )
						{
							return int.Parse( el.GetRawText() );
						}

						return (int)val;
					}, x => SetDefaultValue( name, x ),
					attributes.ToArray()
				);
			}
			else if ( type == typeof( float ) )
			{
				property = TypeLibrary.CreateProperty<float>(
					displayName, () =>
					{
						var val = getter();

						if ( val is JsonElement el )
						{
							return float.Parse( el.GetRawText() );
						}

						return (float)val;
					}, x => SetDefaultValue( name, x ),
					attributes.ToArray()
				);
			}
			else if ( type == typeof( Vector2 ) )
			{
				property =  TypeLibrary.CreateProperty<Vector2>(
					displayName, () =>
					{
						var val = getter();

						if ( val is JsonElement el )
						{
							return Vector2.Parse( el.GetString() );
						}

						return (Vector2)val;
					}, x => SetDefaultValue( name, x ),
					attributes.ToArray()
				);
			}
			else if ( type == typeof( Vector3 ) )
			{
				property = TypeLibrary.CreateProperty<Vector3>(
					displayName, () =>
					{
						var val = getter();

						if ( val is JsonElement el )
						{
							return Vector3.Parse( el.GetString() );
						}

						return (Vector3)val;
					}, x => SetDefaultValue( name, x ),
					attributes.ToArray()
				);
			}
			else if ( type == typeof( Vector4 ) )
			{
				property = TypeLibrary.CreateProperty<Vector4>(
					displayName, () =>
					{
						var val = getter();

						if ( val is JsonElement el )
						{
							return Vector4.Parse( el.GetString() );
						}

						return (Vector4)val;
					}, x => SetDefaultValue( name, x ),
					attributes.ToArray()
				);
			}
			else if ( type == typeof( Color ) )
			{
				property = TypeLibrary.CreateProperty<Color>(
					displayName, () =>
					{
						var val = getter();

						if ( val is JsonElement el )
						{
							return Color.Parse( el.GetString() ) ?? Color.White;
						}

						return (Color)val;
					}, x => SetDefaultValue( name, x ),
					attributes.ToArray()
				);
			}
			else if ( type == typeof( Float2x2 ) )
			{
				property = EditorTypeLibrary.CreateProperty<Float2x2>(
					displayName, () =>
					{
						var val = getter();

						if ( val is JsonElement el )
						{
							return JsonSerializer.Deserialize<Float2x2>( el, ShaderGraphPlus.SerializerOptions() )!;
						}

						return (Float2x2)val;
					}, x => SetDefaultValue( name, x ),
					attributes.ToArray()
				);
			}
			else if ( type == typeof( Float3x3 ) )
			{
				property = EditorTypeLibrary.CreateProperty<Float3x3>(
					displayName, () =>
					{
						var val = getter();

						if ( val is JsonElement el )
						{
							return JsonSerializer.Deserialize<Float3x3>( el, ShaderGraphPlus.SerializerOptions() )!;
						}

						return (Float3x3)val;
					}, x => SetDefaultValue( name, x ),
					attributes.ToArray()
				);
			}
			else if ( type == typeof( Float4x4 ) )
			{
				property = EditorTypeLibrary.CreateProperty<Float4x4>(
					displayName, () =>
					{
						var val = getter();

						if ( val is JsonElement el )
						{
							return JsonSerializer.Deserialize<Float4x4>( el, ShaderGraphPlus.SerializerOptions() )!;
						}

						return (Float4x4)val;
					}, x => SetDefaultValue( name, x ),
					attributes.ToArray()
				);
			}
			else if ( type == typeof( Gradient ) )
			{
				property = EditorTypeLibrary.CreateProperty<Gradient>(
					displayName, () =>
					{
						var val = getter();

						if ( val is JsonElement el )
						{
							return JsonSerializer.Deserialize<Gradient>( el, ShaderGraphPlus.SerializerOptions() )!;
						}

						return (Gradient)val;
					}, x => SetDefaultValue( name, x ),
					attributes.ToArray()
				);
			}
			else if ( !Node.IsSubgraph && type == typeof( Sampler ) )
			{
				attributes.Add( new InlineEditorAttribute() { Label = false } );

				property = EditorTypeLibrary.CreateProperty<Sampler>(
					displayName, () =>
					{
						var val = getter();

						if ( val is JsonElement el )
						{
							return JsonSerializer.Deserialize<Sampler>( el, ShaderGraphPlus.SerializerOptions() )!;
						}

						return (Sampler)val;
					}, x => SetDefaultValue( name, x ),
					attributes.ToArray()
				);
			}

			if ( property != null )
			{
				groups[groupName].Add( property );
			}
		}

		foreach ( var group in groups )
		{
			Sheet.AddGroup( group.Key, group.Value.ToArray() );
		}
	}

	private void SetDefaultValue( string name, object value )
	{
		Node.DefaultValues[name] = value;
		Node.Update();
		Node.IsDirty = true;
	}
}
