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
}

public enum ShadingModel
{
	[Icon( "tungsten" )]
	Lit,
	[Icon( "brightness_3" )]
	Unlit,
	//[Icon( "build" )] // TODO
	//Custom,
}

public enum ShaderDomain
{
	[Icon( "view_in_ar" )]
	Surface,
	[Icon( "brush" )]
	[Hide]
	BlendingSurface, // TODO : Hidden for now since its broken.
	[Icon( "desktop_windows" )]
	PostProcess,
}

public class PreviewSettings
{
	public ViewMode ViewMode { get; set; } = ViewMode.Perspective;
	public bool RenderBackfaces { get; set; } = false;
	public bool EnableShadows { get; set; } = true;
	public bool ShowGround { get; set; } = false;
	public bool ShowSkybox { get; set; } = true;
	public Color BackgroundColor { get; set; } = Color.Black;
	public Color Tint { get; set; } = Color.White;
}

[AssetType( Name = ShaderGraphPlusGlobals.AssetTypeName, Extension = ShaderGraphPlusGlobals.AssetTypeExtension, Flags = AssetTypeFlags.NoEmbedding ), Icon( "account_tree" )]
public partial class ShaderGraphPlus : INodeGraph
{
	[Hide]
	public int Version => 8;

	[Hide, JsonIgnore]
	public IEnumerable<BaseNodePlus> Nodes => _nodes.Values;

	[Hide, JsonIgnore]
	private readonly Dictionary<string, BaseNodePlus> _nodes = new();

	[Hide, JsonIgnore]
	IEnumerable<IGraphNode> INodeGraph.Nodes => Nodes;

	[Hide, JsonIgnore]
	public IEnumerable<BlackboardParameter> Parameters => _parameters.Values;

	[Hide, JsonIgnore]
	public readonly Dictionary<Guid, BlackboardParameter> _parameters = new();

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

	public BlendMode BlendMode { get; set; }

	[ShowIf( nameof( ShowShadingModel ), true )]
	public ShadingModel ShadingModel { get; set; }

	[Hide] private bool ShowShadingModel => Domain != ShaderDomain.PostProcess;

	public ShaderDomain Domain { get; set; }

	/// <summary>
	///	Custom key-value storage for this project.
	/// </summary>
	[Hide]
	public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

	[Hide]
	public PreviewSettings PreviewSettings { get; set; } = new();

	public ShaderGraphPlus()
	{
	}

	public bool ContainsNode( string id )
	{
		if ( _nodes.ContainsKey( id ) ) return true;
		return false;
	}

	public void AddNode( BaseNodePlus node )
	{
		node.Graph = this;
		_nodes.Add( node.Identifier, node );
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

	public BlackboardParameter FindParameter( Guid identifier )
	{
		if ( _parameters.TryGetValue( identifier, out var parameter ) )
		{
			return parameter;
		}

		throw new Exception( $"There is no parameter with the identifier : {identifier}" );
	}

	public BlackboardParameter FindParameter( string name )
	{
		var parameter = _parameters.Values.FirstOrDefault( x => x.Name == name );

		if ( parameter != null )
			return parameter;

		throw new Exception( $"There is no parameter with the name : {name}" );
	}

	public T FindParameter<T>( Guid identifier ) where T : BlackboardParameter
	{
		if ( _parameters.TryGetValue( identifier, out var parameter ) )
		{
			return (T)parameter;
		}

		throw new Exception( $"There is no parameter with the identifier : {identifier}" );
	}

	public T FindParameter<T>( string name ) where T : BlackboardParameter
	{
		var parameter = _parameters.Values.OfType<T>().FirstOrDefault( x => x.Name == name );

		if ( parameter != null )
			return parameter;

		throw new Exception( $"There is no parameter with the name : {name}" );
	}

	public bool HasParameterWithName( string name )
	{
		return _parameters.Any( x => x.Value.Name == name );
	}

	public void AddParameter( BlackboardParameter parameter )
	{
		parameter.Graph = this;
		_parameters.Add( parameter.Identifier, parameter );
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

	string INodeGraph.SerializeNodes( IEnumerable<IGraphNode> nodes )
	{
		return SerializeNodes( nodes.Cast<BaseNodePlus>() );
	}

	IEnumerable<IGraphNode> INodeGraph.DeserializeNodes( string serialized )
	{
		return DeserializeNodes( serialized );
	}

	void INodeGraph.AddNode( IGraphNode node )
	{
		AddNode( (BaseNodePlus)node );
	}

	void INodeGraph.RemoveNode( IGraphNode node )
	{
		RemoveNode( (BaseNodePlus)node );
	}

	public void AddParameter( IBlackboardParameter parameter )
	{
		AddParameter( (BlackboardParameter)parameter );
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
