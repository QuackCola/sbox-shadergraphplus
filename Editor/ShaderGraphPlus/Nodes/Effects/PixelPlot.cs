
namespace ShaderGraphPlus.Nodes;

/// <summary>
///
/// </summary>
[Title( "Pixel Plot" ), Category( "Effects" ), Icon( "grid_on" )]
public sealed class PixelPlotNode : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.FunctionNode;

	[Hide]
	public string PixelPlot => @"	
float4 PixelPlot( in Texture2D vColorTex, in SamplerState sSampler, float2 vUv , float2 vGridSize , float flBoarderThickness)
{
	float2 vGridBlock = 1.0f / vGridSize;
	float2 vUvGrid = floor( vUv * vGridSize ) / vGridSize; // Divide By Gridsize so that uvspace is clamped to  0 to 1.
	float2 vGridBoarder = step( 0.5f - flBoarderThickness, frac( vUv / vGridBlock ) ) *
						 step( frac( vUv / vGridBlock ), 0.5f + flBoarderThickness );

	return vColorTex.Sample( sSampler, vUvGrid ) * ( vGridBoarder.x * vGridBoarder.y );
}
";

	/// <summary>
	/// Texture object to apply the effect to.
	/// </summary>
	[Title( "Texture2D" )]
	[Input( typeof( Texture ) )]
	[Hide]
	public NodeInput Texture2D { get; set; }

	/// <summary>
	/// Coordinates to sample this texture
	/// </summary>
	[Title( "Coordinates" )]
	[Input( typeof( Vector2 ) )]
	[Hide]
	public NodeInput Coords { get; set; }

	/// <summary>
	/// How the effect is filtered and wrapped when sampled
	/// </summary>
	[Title( "Sampler" )]
	[Input( typeof( Sampler ) )]
	[Hide]
	public NodeInput Sampler { get; set; }

	[Input( typeof( Vector2 ) )]
	[Hide]
	public NodeInput GridSize { get; set; }

	[Input( typeof( float ) )]
	[Hide]
	public NodeInput BoarderThickness { get; set; }

	[InlineEditor( Label = false ), Group( "Sampler" )]
	[ShowIf( nameof( ShowDefaultSamplerState ), true )]
	public Sampler SamplerState { get; set; } = new Sampler();

	[InputDefault( nameof( GridSize ) )]
	public Vector2 DefaultGridSize { get; set; } = new Vector2( 24.0f, 24.0f );

	[InputDefault( nameof( BoarderThickness ) )]
	public float DefaultBoarderThickness { get; set; } = 0.420f;

	[JsonIgnore, Hide, Browsable( false )]
	private bool ShowDefaultSamplerState { get; set; } = false;

	[Output( typeof( Vector4 ) ), Title( "Result" )]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		var coordsResult = compiler.Result( Coords );
		var textureResult = compiler.Result( Texture2D );
		var samplerResult = compiler.ResultSamplerOrDefault( Sampler, SamplerState );
		var gridResult = compiler.ResultOrDefault( GridSize, DefaultGridSize );
		var boarderThicknessResult = compiler.ResultOrDefault( BoarderThickness, DefaultBoarderThickness );

		ShowDefaultSamplerState = !Sampler.IsValid;

		if ( !textureResult.IsValid )
		{
			return NodeResult.MissingInput( "Texture2D" );
		}
		else if ( textureResult.ResultType is not ResultType.Texture2D )
		{
			return NodeResult.Error( $"Input to TexObject is not a texture object!" );
		}

		string func = compiler.RegisterHLSLFunction( PixelPlot, "PixelPlot" );
		string funcCall = compiler.ResultHLSLFunction( func, $"{textureResult}, {samplerResult}, {(coordsResult.IsValid ? $"{coordsResult.Cast( 2 )}" : "i.vTextureCoords.xy")}, {gridResult}, {boarderThicknessResult}" );

		return new NodeResult( ResultType.Vector4, funcCall );
	};
}
