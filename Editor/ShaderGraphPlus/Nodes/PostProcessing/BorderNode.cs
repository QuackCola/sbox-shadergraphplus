
namespace ShaderGraphPlus.Nodes;

/// <summary>
/// Adds a black border surrounding the edges of the screen.
/// </summary>
[Title( "Border" ), Category( "PostProcessing/Effects" ), Icon( "crop_square" )]
public class BorderNode : ShaderNodePlus
{
	[Hide]
	public static string Border => @"
float Border( float2 vUV , float flWarpAmount )
{
	float radius = min( flWarpAmount, 0.08f );
	radius = max( min( min( abs( radius * 2.0f ), abs( 1.0f ) ), abs( 1.0f ) ) , 1e-5 );
	float2 abs_uv = abs( vUV * 2.0f - 1.0f ) - float2( 1.0f, 1.0f ) + radius;
	float dist = length( max( float2( 0.0f, 0.0f ), abs_uv ) ) / radius;
	float square = smoothstep( 0.96f, 1.0f, dist );
	return clamp( 1.0f - square, 0.0f, 1.0f );
}
";

	[Input( typeof( Vector2 ) ), Title( "Coords" )]
	[Hide]
	public NodeInput ScreenUVInput { get; set; }

	[Input( typeof( float ) ), Title( "Warp Amount" )]
	[Hide]
	public NodeInput WarpAmountInput { get; set; }

	[InputDefault( nameof( WarpAmountInput ) )]
	[Title( "Warp Amount" )]
	[Sandbox.Range( 0f, 0.08f ), Sandbox.Step( 0.01f )]
	public float DefaultWarpAmount { get; set; } = 1.0f;

	[Output( typeof( float ) )]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		if ( compiler.Graph.Domain != ShaderDomain.PostProcess )
		{
			return NodeResult.Error( $"'{DisplayInfo.Name}' node is only ment to be used in the '{ShaderDomain.PostProcess}' domain." );
		}

		var coords = compiler.Result( ScreenUVInput );
		var warpamount = compiler.ResultOrDefault( WarpAmountInput, DefaultWarpAmount );

		string func = compiler.RegisterHLSLFunction( Border, "Border" );
		string funcCall = compiler.ResultHLSLFunction( func, $"{(coords.IsValid ? $"{coords.Cast( 2 )}" : "i.vPositionSs.xy / g_vRenderTargetSize")}, {warpamount}" );

		return new NodeResult( ResultType.Float, funcCall );
	};
}
