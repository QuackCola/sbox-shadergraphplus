using Sandbox.Resources;
using System.Text;

namespace ShaderGraphPlus;

public enum SubgraphOutputPreviewType
{
	[Icon( "clear" )]
	None,
	[Icon( "palette" )]
	Albedo,
	[Icon( "brightness_5" )]
	Emission,
	[Icon( "opacity" )]
	Opacity,
	[Icon( "texture" )]
	Normal,
	[Icon( "terrain" )]
	Roughness,
	[Icon( "auto_awesome" )]
	Metalness,
	[Icon( "tonality" )]
	AmbientOcclusion,
	[Icon( "arrow_forward" )]
	PositionOffset
}

/// <summary>
/// Output of a subgraph.
/// </summary>
[Title( "Subgraph Output" ), Icon( "output" ), SubgraphOnly]
[InternalNode]
public sealed class SubgraphOutput : BaseResult, BaseNodePlus.IInitializeNode, IBlackboardNode, IErroringNode
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.SubgraphNode;

	[JsonIgnore, Hide, Browsable( false )]
	public override bool AutoSize => true;

	[JsonIgnore, Hide, Browsable( false )]
	public override bool CanRemove => true;

	//[Hide, Browsable( false )]
	//public Guid OutputIdentifier { get; set; }

	[Hide, Browsable( false )]
	public Guid ParameterIdentifier { get; set; }

	[JsonIgnore, Hide, Browsable( false )]
	public string OutputName => GetParameter().Name;

	[JsonIgnore, Hide, Browsable( false )]
	public string OutputDescription => GetParameter().Description;

	[JsonIgnore, Hide, Browsable( false )]
	public SubgraphPortType PortType => GetParameter().PortType;

	[JsonIgnore, Hide, Browsable( false )]
	public SubgraphOutputPreviewType Preview => GetParameter().Preview;

	[JsonIgnore, Hide, Browsable( false )]
	public int PortOrder => GetParameter().PortOrder;

	[Hide]
	private List<IPlugIn> InternalInputs = new();

	[Hide]
	public override IEnumerable<IPlugIn> Inputs => InternalInputs;

	public SubgraphOutput() : base()
	{
	}

	[JsonIgnore, Hide, Browsable( false )]
	int _lastHashCode = 0;

	public override void OnFrame()
	{
		var hashCodeInput = 0;
		hashCodeInput = System.HashCode.Combine( ParameterIdentifier, OutputName, OutputDescription, PortType, PortOrder );

		if ( hashCodeInput != _lastHashCode )
		{
			_lastHashCode = hashCodeInput;
			InitializeNode();
		}
	}

	private IBlackboardSubgraphOutputParameter GetParameter()
	{
		if ( Graph is ShaderGraphPlus graph )
		{
			var parameter = graph.FindParameter( ParameterIdentifier );

			if ( parameter is IBlackboardSubgraphOutputParameter subgraphOutputParameter )
			{
				return subgraphOutputParameter;
			}

			return new FloatSubgraphOutputParameter();
		}

		return new FloatSubgraphOutputParameter();
	}

	public void InitializeNode()
	{
		CreateInput();
		Update();
	}

	private void CreateInput()
	{
		var plugs = new List<IPlugIn>();

		var parameter = GetParameter();

		if ( !parameter.IsValid )
			return;

		var type = PortType switch
		{
			SubgraphPortType.Bool => typeof( bool ),
			SubgraphPortType.Int => typeof( int ),
			SubgraphPortType.Float => typeof( float ),
			SubgraphPortType.Vector2 => typeof( Vector2 ),
			SubgraphPortType.Vector3 => typeof( Vector3 ),
			SubgraphPortType.Vector4 => typeof( Color ),
			SubgraphPortType.Color => typeof( Color ),
			SubgraphPortType.Float2x2 => typeof( Float2x2 ),
			SubgraphPortType.Float3x3 => typeof( Float3x3 ),
			SubgraphPortType.Float4x4 => typeof( Float4x4 ),
			SubgraphPortType.Gradient => typeof( Gradient ),
			SubgraphPortType.SamplerState => typeof( Sampler ),
			SubgraphPortType.Texture2DObject => typeof( Texture ),
			SubgraphPortType.TextureCubeObject => typeof( Texture ),
			_ => throw new NotImplementedException( $"Unknown PortType \'{PortType}\'" )
		};

		var info = new PlugInfo()
		{
			Id = parameter.Identifier,
			Name = parameter.Name,
			Type = type,
			DisplayInfo = new()
			{
				Name = parameter.Name,
				Fullname = type.FullName,
				Description = parameter.Description,
			}
		};

		var plug = new BasePlugIn( this, info, info.Type );
		//var oldPlug = InternalInputs.FirstOrDefault( x => x is BasePlugIn plugIn && plugIn.Info.Name == info.Name ) as BasePlugIn;
		var oldPlug = InternalInputs.FirstOrDefault( x => x is BasePlugIn plugIn && plugIn.Info.Id == info.Id ) as BasePlugIn;
		if ( oldPlug is not null )
		{
			oldPlug.Info.Name = info.Name;
			oldPlug.Info.Type = type;
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

		InternalInputs = plugs;
	}

	public void AddMaterialOutput( GraphCompiler compiler, StringBuilder sb, SubgraphOutputPreviewType previewType, out List<string> errors )
	{
		errors = new List<string>();

		// Make sure we dont try to preview outputs that we cant.
		var parameter = GetParameter();
		var graph = Graph as ShaderGraphPlus;

		if ( GetParameter().CannotPreviewOutputType )
		{
			parameter.Preview = SubgraphOutputPreviewType.None;
			graph.UpdateParameter( parameter );

			return;
		}

		if ( previewType == SubgraphOutputPreviewType.Albedo )
		{
			var albedoResult = GetAlbedoResult( compiler );

			if ( !albedoResult.IsValid )
				return;

			sb.AppendLine( $"m.Albedo = {albedoResult.Cast( 3 )};" );
		}
		if ( previewType == SubgraphOutputPreviewType.Emission )
		{
			var emissionResult = GetEmissionResult( compiler );

			if ( !emissionResult.IsValid )
				return;

			sb.AppendLine( $"m.Emission = {emissionResult.Cast( 3 )};" );
		}
		if ( previewType == SubgraphOutputPreviewType.Opacity )
		{
			var opacityResult = GetOpacityResult( compiler );

			if ( !opacityResult.IsValid )
				return;

			sb.AppendLine( $"m.Opacity = {opacityResult.Cast( 1 )};" );
		}
		if ( previewType == SubgraphOutputPreviewType.Normal )
		{
			var normalResult = GetNormalResult( compiler );

			if ( !normalResult.IsValid )
				return;

			sb.AppendLine( $"m.Normal = {normalResult.Cast( 3 )};" );
		}
		if ( previewType == SubgraphOutputPreviewType.Roughness )
		{
			var roughnessResult = GetRoughnessResult( compiler );

			if ( !roughnessResult.IsValid )
				return;

			sb.AppendLine( $"m.Roughness = {roughnessResult.Cast( 1 )};" );
		}
		if ( previewType == SubgraphOutputPreviewType.Metalness )
		{
			var metalnessResult = GetMetalnessResult( compiler );

			if ( !metalnessResult.IsValid )
				return;

			sb.AppendLine( $"m.Metalness = {metalnessResult.Cast( 1 )};" );
		}
		if ( previewType == SubgraphOutputPreviewType.AmbientOcclusion )
		{
			var ambientOcclusionResult = GetAmbientOcclusionResult( compiler );

			if ( !ambientOcclusionResult.IsValid )
				return;

			sb.AppendLine( $"m.AmbientOcclusion = {ambientOcclusionResult.Cast( 1 )};" );
		}
	}

	public NodeInput? GetInputFromPreview( SubgraphOutputPreviewType previewType )
	{
		// Make sure we dont try to preview outputs that we cant.
		var parameter = GetParameter();
		var graph = Graph as ShaderGraphPlus;

		if ( parameter.CannotPreviewOutputType )
		{
			parameter.Preview = SubgraphOutputPreviewType.None;
			graph.UpdateParameter( parameter );
		}

		if ( Preview == previewType )
		{
			var input = Inputs.FirstOrDefault( x => x is BasePlugIn plugIn && plugIn.Info.Id == ParameterIdentifier );
			if ( input is BasePlugIn plugIn )
			{

				if ( plugIn.Info.ConnectedPlug is not null )
				{
					return new NodeInput
					{
						Identifier = plugIn.Info.ConnectedPlug.Node.Identifier,
						Output = plugIn.Info.ConnectedPlug.Identifier
					};
				}
				else
				{
					SGPLogger.Warning( $"Missing Inputernal Input : {plugIn.DisplayInfo.Name}" );
				}

				return plugIn.Info.GetInput( plugIn.Node );
			}
		}

		return null;
	}

	public override NodeInput GetAlbedo()
	{
		var input = GetInputFromPreview( SubgraphOutputPreviewType.Albedo );
		if ( input is not null )
		{
			return input.Value;
		}
		return default;
	}

	public override NodeInput GetEmission()
	{
		var input = GetInputFromPreview( SubgraphOutputPreviewType.Emission );
		if ( input is not null )
		{
			return input.Value;
		}
		return default;
	}

	public override NodeInput GetOpacity()
	{
		var input = GetInputFromPreview( SubgraphOutputPreviewType.Opacity );
		if ( input is not null )
		{
			return input.Value;
		}
		return default;
	}

	public override NodeInput GetNormal()
	{
		var input = GetInputFromPreview( SubgraphOutputPreviewType.Normal );
		if ( input is not null )
		{
			return input.Value;
		}
		return default;
	}

	public override NodeInput GetRoughness()
	{
		var input = GetInputFromPreview( SubgraphOutputPreviewType.Roughness );
		if ( input is not null )
		{
			return input.Value;
		}
		return default;
	}

	public override NodeInput GetMetalness()
	{
		var input = GetInputFromPreview( SubgraphOutputPreviewType.Metalness );
		if ( input is not null )
		{
			return input.Value;
		}
		return default;
	}

	public override NodeInput GetAmbientOcclusion()
	{
		var input = GetInputFromPreview( SubgraphOutputPreviewType.AmbientOcclusion );
		if ( input is not null )
		{
			return input.Value;
		}
		return default;
	}

	public override NodeInput GetPositionOffset()
	{
		var input = GetInputFromPreview( SubgraphOutputPreviewType.PositionOffset );
		if ( input is not null )
		{
			return input.Value;
		}
		return default;
	}

	public override Color GetDefaultAlbedo()
	{
		return base.GetDefaultAlbedo();
	}

	public List<string> GetErrors()
	{
		var errors = new List<string>();

		if ( Graph is ShaderGraphPlus shaderGraphPlus && shaderGraphPlus.IsSubgraph )
		{
			//if ( string.IsNullOrWhiteSpace( OutputName ) )
			//{
			//	errors.Add( $"Subgraph output must have a name!" );
			//
			//	return errors;
			//}

			foreach ( var node in Graph.Nodes )
			{
				if ( node == this )
					continue;

				if ( node is SubgraphOutput otherOutput && otherOutput.OutputName == OutputName )
				{
					errors.Add( $"Duplicate subgraph output node \"{OutputName}\"" );

					break;
				}
			}
		}

		return errors;
	}
}
