using Editor;
using ShaderGraphPlus.Nodes;
using static Editor.SceneViewportWidget;

namespace ShaderGraphPlus;

public enum BlendMode
{
	[Icon( "circle" )]
	Opaque,
	[Icon( "radio_button_unchecked" )]
	Masked,
	[Icon( "blur_on" )]
	Translucent,
	[Icon( "tune" )]
	Dynamic,
}

public enum ShadingModel
{
	/// <summary>
	/// Default Valve lighting model
	/// </summary>
	[Icon( "tungsten" )]
	Lit,
	/// <summary>
	/// No Lighting model
	/// </summary>
	[Icon( "brightness_3" )]
	Unlit,
}

public enum ShaderDomain
{
	[Icon( "view_in_ar" )]
	Surface,
	[Icon( "desktop_windows" )]
	PostProcess,
}

public enum RenderFace
{
	/// <summary>
	/// Render only the front faces
	/// </summary>
	[Icon( "visibility" )]
	Front,
	/// <summary>
	/// Render only the back faces
	/// </summary>
	[Icon( "visibility_off" )]
	Back,
	/// <summary>
	/// Render both the front and back faces
	/// </summary>
	[Icon( "visibility_off" )]
	Both,
}

public class PreviewSettings
{
	/// <summary>
	/// Current viewmode of the preview veiwport
	/// </summary>
	public ViewMode ViewMode { get; set; } = ViewMode.Perspective;
	public bool RenderBackfaces { get; set; } = false;

	/// <summary>
	/// If true we'll render shadows
	/// </summary>
	public bool EnableShadows { get; set; } = true;

	/// <summary>
	/// If true we'll show the ground plane
	/// </summary>
	public bool ShowGround { get; set; } = false;

	/// <summary>
	/// If true we'll show the shybox
	/// </summary>
	public bool ShowSkybox { get; set; } = true;

	/// <summary>
	/// Color of the background when the skybox is not being drawn
	/// </summary>
	public Color BackgroundColor { get; set; } = Color.Black;

	/// <summary>
	/// Color tint of the model in the preview
	/// </summary>
	public Color Tint { get; set; } = Color.White;
}

[AssetType( Name = ShaderGraphPlusGlobals.AssetTypeName, Extension = ShaderGraphPlusGlobals.AssetTypeExtension, Flags = AssetTypeFlags.NoEmbedding ), Icon( "account_tree" )]
public partial class ShaderGraphPlus : IBlackboardNodeGraph
{
	[Hide]
	public int Version => 10;

	[Hide, JsonIgnore]
	public IEnumerable<BaseNodePlus> Nodes => _nodes.Values;

	[Hide, JsonIgnore]
	private readonly Dictionary<string, BaseNodePlus> _nodes = new();

	[Hide, JsonIgnore]
	IEnumerable<IGraphNode> INodeGraph.Nodes => Nodes;

	[Hide, JsonIgnore]
	public IEnumerable<BlackboardParameter> Parameters => _parameters.Values;

	[Hide, JsonIgnore]
	private readonly OrderedDictionary<Guid, BlackboardParameter> _parameters = new();

	[Hide, JsonIgnore]
	IEnumerable<IBlackboardParameter> IBlackboardNodeGraph.Parameters => Parameters;

	[Hide, JsonIgnore]
	public IEnumerable<CategoryData> CategoryData => _categoryData.Values;

	[Hide, JsonIgnore]
	private readonly OrderedDictionary<Guid, CategoryData> _categoryData = new();

	/// <summary>
	///	Custom key-value storage for this project.
	/// </summary>
	[Hide]
	public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

	[Hide]
	public bool IsSubgraph { get; set; }

	[Hide]
	public string Path { get; set; }

	[Hide]
	public string Model { get; set; }

	/// <summary>
	/// The name of the Node when used in ShaderGraph
	/// </summary>
	[ShowIf( nameof( IsSubgraph ), true )]
	public string Title { get; set; }

	public string Description { get; set; }

	/// <summary>
	/// The category of the Node when browsing the Node Library (optional)
	/// </summary>
	[ShowIf( nameof( AddToNodeLibrary ), true )]
	public string Category { get; set; }

	[IconName, ShowIf( nameof( IsSubgraph ), true )]
	public string Icon { get; set; }

	/// <summary>
	/// Whether or not this Node should appear when browsing the Node Library.
	/// Otherwise can only be referenced by dragging the Subgraph asset into the graph.
	/// </summary>
	[ShowIf( nameof( IsSubgraph ), true )]
	public bool AddToNodeLibrary { get; set; }

	[ShowIf( nameof( ShowShadingModel ), true )]
	public BlendMode BlendMode { get; set; }

	[ShowIf( nameof( ShowShadingModel ), true )]
	public ShadingModel ShadingModel { get; set; }

	[ShowIf( nameof( ShowShadingModel ), true )]
	public RenderFace RenderFace { get; set; }

	[Hide]
	private bool ShowShadingModel => Domain != ShaderDomain.PostProcess;

	[Hide]
	private bool HasNoTemplate => string.IsNullOrWhiteSpace( ShaderTemplate );

	/// <summary>
	/// What shader type this graph is
	/// </summary>
	[ShowIf( nameof( HasNoTemplate ), true )]
	public ShaderDomain Domain { get; set; }

	[Group( "Advanced" ), ShaderTemplatePath]
	public string ShaderTemplate { get; set; }

	[Hide]
	public PreviewSettings PreviewSettings { get; set; } = new();

	[Hide, JsonIgnore]
	public UserShaderTemplateInfo UserTemplateInfo { get; set; } = new();

	public ShaderGraphPlus()
	{
	}

	/// <summary>
	/// Validates and auto-corrects BlendMode and ShadingModel if current values are not supported
	/// </summary>
	public void ValidateTemplateSettings()
	{
		// Auto-correct BlendMode if current is not supported
		bool currentBlendModeSupported = BlendMode switch
		{
			BlendMode.Opaque => UserTemplateInfo.SupportsOpaqueBlend,
			BlendMode.Masked => UserTemplateInfo.SupportsMaskedBlend,
			BlendMode.Translucent => UserTemplateInfo.SupportsTranslucentBlend,
			BlendMode.Dynamic => UserTemplateInfo.SupportsDynamicBlend,
			_ => false
		};

		if ( !currentBlendModeSupported )
		{
			// Find the first supported blend mode
			if ( UserTemplateInfo.SupportsOpaqueBlend ) BlendMode = BlendMode.Opaque;
			else if ( UserTemplateInfo.SupportsMaskedBlend ) BlendMode = BlendMode.Masked;
			else if ( UserTemplateInfo.SupportsTranslucentBlend ) BlendMode = BlendMode.Translucent;
			else if ( UserTemplateInfo.SupportsDynamicBlend ) BlendMode = BlendMode.Dynamic;
		}

		// Auto-correct ShadingModel if current is not supported
		bool currentShadingModelSupported = ShadingModel switch
		{
			ShadingModel.Lit => UserTemplateInfo.SupportsLitShading,
			ShadingModel.Unlit => UserTemplateInfo.SupportsUnlitShading,
			_ => false
		};

		if ( !currentShadingModelSupported )
		{
			// Find the first supported shading model
			if ( UserTemplateInfo.SupportsLitShading ) ShadingModel = ShadingModel.Lit;
			else if ( UserTemplateInfo.SupportsUnlitShading ) ShadingModel = ShadingModel.Unlit;
		}

		// Ensure template ShaderDomain
		if ( !string.IsNullOrWhiteSpace( ShaderTemplate ) )
		{
			Domain = UserTemplateInfo.SupportsSurfaceDomain ? ShaderDomain.Surface : ShaderDomain.PostProcess;
		}
	}

	public bool ContainsNode( string id )
	{
		if ( _nodes.ContainsKey( id ) ) return true;
		return false;
	}

	public bool ContainsParameter( Guid id )
	{
		if ( _parameters.ContainsKey( id ) ) return true;
		return false;
	}

	public void AddNode( BaseNodePlus node )
	{
		node.Graph = this;
		_nodes.Add( node.Identifier, node );
	}

	public void AddParameter( IBlackboardParameter parameter )
	{
		AddParameter( (BlackboardParameter)parameter );
	}

	public void RemoveNode( BaseNodePlus node )
	{
		if ( node.Graph != this )
			return;

		//SGPLog.Info( $"Removing node with id : {node.Identifier}");

		_nodes.Remove( node.Identifier );
	}

	public BaseNodePlus FindNode( string name )
	{
		_nodes.TryGetValue( name, out var node );
		return node;
	}

	public int GetParameterIndex( BlackboardParameter parameter )
	{
		var index = _parameters.IndexOf( parameter.Identifier );

		if ( index != -1 )
		{
			return index;
		}

		return 0;
	}

	public BlackboardParameter FindParameter( Guid identifier )
	{
		_parameters.TryGetValue( identifier, out var parameter );
		return parameter;
	}

	public BlackboardParameter FindParameter( string name )
	{
		var parameter = _parameters.Values.FirstOrDefault( x => x.Name == name );
		return parameter;
	}

	public bool TryFindParameter( Guid identifier, out BlackboardParameter parameter )
	{
		return _parameters.TryGetValue( identifier, out parameter );
	}

	public bool TryFindParameter( string name, out BlackboardParameter parameter )
	{
		parameter = _parameters.Values.FirstOrDefault( x => x.Name == name );

		return parameter != null;
	}

	public T FindParameter<T>( Guid identifier ) where T : BlackboardParameter
	{
		_parameters.TryGetValue( identifier, out var parameter );
		return (T)parameter;
	}

	public T FindParameter<T>( string name ) where T : BlackboardParameter
	{
		var parameter = _parameters.Values.OfType<T>().FirstOrDefault( x => x.Name == name );
		return parameter;
	}

	public bool TryFindParameter<T>( Guid identifier, out T parameter ) where T : BlackboardParameter
	{
		parameter = null;

		if ( _parameters.TryGetValue( identifier, out var foundParameter ) )
		{
			parameter = (T)foundParameter;

			return true;
		}

		return false;
	}

	public bool TryFindParameter<T>( string name, out T parameter ) where T : BlackboardParameter
	{
		parameter = (T)_parameters.Values.FirstOrDefault( x => x.Name == name );

		return parameter != null;
	}

	public bool TryFindCategoryData( Guid identifier, out CategoryData categoryData )
	{
		categoryData = null;

		if ( _categoryData.TryGetValue( identifier, out var foundCategoryData ) )
		{
			categoryData = foundCategoryData;

			return true;
		}

		return false;
	}

	public bool HasParameterWithName( string name )
	{
		return _parameters.Any( x => string.Equals( x.Value.Name, name, StringComparison.CurrentCultureIgnoreCase ) );
	}

	public bool HasCategoryDataWithName( string name )
	{
		return _categoryData.Any( x => x.Value.Name == name );
	}

	public void AddParameter( BlackboardParameter parameter, int index = -1 )
	{
		parameter.Graph = this;

		if ( index <= -1 )
		{
			_parameters.Add( parameter.Identifier, parameter );
		}
		else
		{
			_parameters.Insert( index, parameter.Identifier, parameter );
		}
	}

	public void AddCategoryData( CategoryData categoryData )
	{
		categoryData.Graph = this;
		_categoryData.Add( categoryData.Identifier, categoryData );
	}

	public bool ReOrderParameter( BlackboardParameter parameter, int newIndex )
	{
		if ( parameter.Graph != this )
			return false;

		if ( newIndex <= -1 )
		{
			//throw new IndexOutOfRangeException( $"New Index Invalid '{newIndex}'" );

			SGPLogger.Error( $"New Index Invalid '{newIndex}'" );

			return false;
		}

		_parameters.Remove( parameter.Identifier );

		if ( newIndex > _parameters.Count )
		{
			_parameters.Add( parameter.Identifier, parameter );
		}
		else
		{
			_parameters.Insert( newIndex, parameter.Identifier, parameter );
		}

		return true;
	}

	public void UpdateParameter( IBlackboardParameter parameter )
	{
		var blackboardParameter = parameter as BlackboardParameter;
		if ( blackboardParameter.Graph != this )
			return;

		_parameters[parameter.Identifier] = blackboardParameter;
	}

	public void UpdateParameterValue( Guid identifier, object value )
	{
		if ( !_parameters.ContainsKey( identifier ) )
			throw new Exception( $"There is no parameter with the identifier : {identifier}" );

		_parameters[identifier].SetValue( value );
	}

	public void RemoveParameter( BlackboardParameter parameter )
	{
		if ( parameter.Graph != this )
			return;

		RemoveParameter( parameter.Identifier );
	}

	public void RemoveParameter( Guid identifier )
	{
		_parameters.Remove( identifier );
	}

	public void RemoveCategoryData( CategoryData categoryData )
	{
		if ( categoryData.Graph != this )
			return;

		_categoryData.Remove( categoryData.Identifier );
	}

	public void UpdateCategoryData( CategoryData categoryData )
	{
		if ( categoryData.Graph != this )
			return;

		_categoryData[categoryData.Identifier] = categoryData;
	}

	internal NamedRerouteDeclarationNode FindNamedRerouteDeclarationNode( string name )
	{
		var node = Nodes.OfType<NamedRerouteDeclarationNode>().Where( x => x.Name == name ).FirstOrDefault();

		if ( node != null )
		{
			return node;
		}

		SGPLogger.Error( $"Could not find NamedReroute \"{name}\"" );

		return null;
	}

	public void ClearNodes()
	{
		_nodes.Clear();
	}

	public void ClearParameters()
	{
		_parameters.Clear();
	}

	public void ClearCategoryData()
	{
		_categoryData.Clear();
	}

	string INodeGraph.SerializeNodes( IEnumerable<IGraphNode> nodes )
	{
		return SerializeNodes( nodes.Cast<BaseNodePlus>() );
	}

	IEnumerable<IGraphNode> INodeGraph.DeserializeNodes( string serialized )
	{
		return DeserializeNodes( serialized );
	}

	string IBlackboardNodeGraph.SerializeParameters( IEnumerable<IBlackboardParameter> parameters )
	{
		return SerializeParameters( parameters.Cast<BlackboardParameter>() );
	}

	IEnumerable<IBlackboardParameter> IBlackboardNodeGraph.DeserializeParameters( string serialized )
	{
		return DeserializeParameters( serialized );
	}

	void INodeGraph.AddNode( IGraphNode node )
	{
		AddNode( (BaseNodePlus)node );
	}

	void INodeGraph.RemoveNode( IGraphNode node )
	{
		RemoveNode( (BaseNodePlus)node );
	}

	void IBlackboardNodeGraph.AddParameter( IBlackboardParameter parameter )
	{
		AddParameter( (BlackboardParameter)parameter );
	}

	void IBlackboardNodeGraph.RemoveParameter( IBlackboardParameter parameter )
	{
		RemoveParameter( (BlackboardParameter)parameter );
	}

	IBlackboardParameter IBlackboardNodeGraph.FindParameter( Guid identifier )
	{
		return FindParameter( identifier );
	}

	internal void UpdateCategoryPriority( CategoryData target, int newPriority )
	{
		var oldPriority = target.Priority;
		target.Priority = newPriority;

		if ( newPriority > oldPriority ) // Category moved down the list
		{
			foreach ( var kvp in _categoryData )
			{
				if ( kvp.Value != target &&
					kvp.Value.Priority > oldPriority &&
					kvp.Value.Priority <= newPriority )
				{
					kvp.Value.Priority--;
				}
			}
		}
		else if ( newPriority < oldPriority ) // Category moved up the list
		{
			foreach ( var kvp in _categoryData )
			{
				if ( kvp.Value != target &&
					kvp.Value.Priority >= newPriority &&
					kvp.Value.Priority < oldPriority )
				{
					kvp.Value.Priority++;
				}
			}
		}
	}

	/// <summary>
	/// Try to get a value at given key in <see cref="ShaderGraphPlus.Metadata"/>.
	/// </summary>
	/// <typeparam name="T">Type of the value.</typeparam>
	/// <param name="keyname">The key to retrieve the value of.</param>
	/// <param name="outvalue"> The value, if it was present in the metadata storage.</param>
	/// <returns>Whether the value was successfully retrieved.</returns>
	public bool TryGetMeta<T>( string keyname, out T outvalue )
	{
		outvalue = default( T );
		if ( Metadata == null )
		{
			return false;
		}

		if ( !Metadata.TryGetValue( keyname, out var value ) )
		{
			return false;
		}

		if ( value is T val )
		{
			outvalue = val;
			return true;
		}

		if ( value is JsonElement element )
		{
			try
			{
				T val2 = element.Deserialize<T>( new JsonSerializerOptions() );
				outvalue = ((val2 != null) ? val2 : default( T ));
			}
			catch ( Exception )
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Store custom data at given key in <see cref="ShaderGraphPlus.Metadata"/>.
	/// </summary>
	/// <param name="keyname">The key for the data.</param>
	/// <param name="outvalue">The data itself to store.</param>
	/// <returns>Always true.</returns>
	public bool SetMeta( string keyname, object outvalue )
	{
		if ( Metadata == null )
		{
			Dictionary<string, object> dictionary2 = (Metadata = new Dictionary<string, object>());
		}

		if ( outvalue == null )
		{
			return Metadata.Remove( keyname );
		}

		Metadata[keyname] = outvalue;
		return true;
	}

}

[AssetType( Name = ShaderGraphPlusGlobals.SubgraphAssetTypeName, Extension = ShaderGraphPlusGlobals.SubgraphAssetTypeExtension, Flags = AssetTypeFlags.NoEmbedding )]
public sealed partial class ShaderGraphPlusSubgraph : ShaderGraphPlus
{
}
