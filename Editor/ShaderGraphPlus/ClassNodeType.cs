using Editor;
using ShaderGraphPlus.Nodes;

namespace ShaderGraphPlus;

public class ClassNodeType : INodeType
{
	public virtual string Identifier => Type.FullName;

	public TypeDescription Type { get; }
	public DisplayInfo DisplayInfo { get; protected set; }

	public Menu.PathElement[] Path => Menu.GetSplitPath( DisplayInfo );

	public ClassNodeType( TypeDescription type )
	{
		Type = type;
		if ( Type is not null )
			DisplayInfo = DisplayInfo.ForType( Type.TargetType );
		else
			DisplayInfo = new DisplayInfo();
	}

	public bool TryGetInput( Type valueType, out string name )
	{
		var property = Type.Properties
			.Select( x => (Property: x, Attrib: x.GetCustomAttribute<BaseNodePlus.InputAttribute>()) )
			.Where( x => x.Attrib != null )
			.FirstOrDefault( x => x.Attrib.Type?.IsAssignableFrom( valueType ) ?? true )
			.Property;

		name = property?.Name;
		return name is not null;
	}

	public bool TryGetOutput( Type valueType, out string name )
	{
		var property = Type.Properties
			.Select( x => (Property: x, Attrib: x.GetCustomAttribute<BaseNodePlus.OutputAttribute>()) )
			.Where( x => x.Attrib != null )
			.FirstOrDefault( x => x.Attrib.Type?.IsAssignableTo( valueType ) ?? true )
			.Property;

		name = property?.Name;
		return name is not null;
	}


	public virtual IGraphNode CreateNode( INodeGraph graph )
	{
		var node = Type.Create<BaseNodePlus>();

		node.Graph = graph;

		return node;
	}
}

public sealed class SubgraphNodeType : ClassNodeType
{
	public override string Identifier => AssetPath;
	string AssetPath { get; }

	public SubgraphNodeType( string assetPath, TypeDescription type ) : base( type )
	{
		AssetPath = assetPath;
	}

	public void SetDisplayInfo( ShaderGraphPlus subgraph )
	{
		var info = DisplayInfo;
		if ( !string.IsNullOrEmpty( subgraph.Title ) )
			info.Name = subgraph.Title;
		else
			info.Name = System.IO.Path.GetFileNameWithoutExtension( AssetPath );
		if ( !string.IsNullOrEmpty( subgraph.Description ) )
			info.Description = subgraph.Description;
		if ( !string.IsNullOrEmpty( subgraph.Icon ) )
			info.Icon = subgraph.Icon;
		if ( !string.IsNullOrEmpty( subgraph.Category ) )
			info.Group = subgraph.Category;
		DisplayInfo = info;
	}

	public override IGraphNode CreateNode( INodeGraph graph )
	{
		var node = base.CreateNode( graph );

		if ( node is SubgraphNode subgraphNode )
		{
			subgraphNode.SubgraphPath = AssetPath;
			subgraphNode.OnNodeCreated();
		}

		return node;
	}
}

public sealed class SubgraphOutputNodeType : ClassNodeType
{
	private string Name;
	private SubgraphPortType OutputType;

	public SubgraphOutputNodeType( TypeDescription type, SubgraphPortType outputType, string name = "" ) : base( type )
	{
		Name = name;
		OutputType = outputType;
	}

	public override IGraphNode CreateNode( INodeGraph graph )
	{
		var node = base.CreateNode( graph );
		if ( node is SubgraphOutput subgraphOutput )
		{
		}
		return node;
	}
}

public sealed class ParameterNodeType : ClassNodeType
{
	BlackboardParameter Parameter;
	Action OnNodeCreated;

	string ImagePath;

	public ParameterNodeType( BlackboardParameter parameter ) : base( null )
	{
		Parameter = parameter;
	}

	public ParameterNodeType( TypeDescription type, string imagePath, Action onNodeCreated ) : base( type )
	{
		ImagePath = imagePath;
		OnNodeCreated = onNodeCreated;
	}

	public override IGraphNode CreateNode( INodeGraph graph )
	{
		var sg = graph as ShaderGraphPlus;

		if ( Parameter == null && !string.IsNullOrWhiteSpace( ImagePath ) )
		{
			var baseName = $"{(sg.IsSubgraph ? "SubgraphInput" : "MaterialParameter")}";
			var id = 0;
			while ( sg.HasParameterWithName( $"{baseName}{id}" ) )
			{
				id++;
			}

			var name = $"{baseName}{id}";

			if ( sg.IsSubgraph && Type.TargetType == typeof( SubgraphInput ) )
			{
				Parameter = new Texture2DSubgraphInputParameter()
				{
					Name = name,
					Value = new TextureInput() { DefaultTexture = ImagePath }
				};
			}
			else if ( !sg.IsSubgraph && Type.TargetType == typeof( Texture2DParameterNode ) )
			{
				Parameter = new Texture2DParameter()
				{
					Name = name,
					Value = new TextureInput() { DefaultTexture = ImagePath }
				};
			}

			sg.AddParameter( Parameter );

			var node = Parameter.ToNode() as BaseNodePlus;
			node.Graph = sg;

			OnNodeCreated?.Invoke();

			return node;
		}
		else
		{
			var node = Parameter.ToNode() as BaseNodePlus;
			node.Graph = sg;
			return node;
		}
	}
}

public sealed class NamedRerouteDeclarationNodeType : ClassNodeType
{
	private string Name;

	public NamedRerouteDeclarationNodeType( TypeDescription type, string name = "" ) : base( type )
	{
		Name = name;
	}

	public override IGraphNode CreateNode( INodeGraph graph )
	{
		var node = base.CreateNode( graph );
		if ( node is NamedRerouteDeclarationNode namedRerouteDeclarationNode )
		{
			namedRerouteDeclarationNode.Name = Name;

			return namedRerouteDeclarationNode;
		}
		return node;
	}
}

public sealed class NamedRerouteNodeType : ClassNodeType
{
	private string Name;

	public NamedRerouteNodeType( TypeDescription type, string name = "" ) : base( type )
	{
		Name = name;
	}

	public override IGraphNode CreateNode( INodeGraph graph )
	{
		var node = base.CreateNode( graph );
		if ( node is NamedRerouteNode namedRerouteNode )
		{
			namedRerouteNode.Name = Name;

			return namedRerouteNode;
		}
		return node;
	}
}
