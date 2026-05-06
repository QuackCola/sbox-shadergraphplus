
namespace ShaderGraphPlus.Nodes;

/// <summary>
/// Takes in the Screen UV's and warps the edges, creating the spherized effect.
/// </summary>
[Title( "Warp" ), Category( "PostProcessing/Effects" ), Icon( "water" )]
public class WarpNode : ShaderNodePlus
{
	[Hide]
	public static string ScreenWarp => @"
float2 ScreenWarp( float2 vUV , float flWarpAmount )
{
	float2 delta = vUV - 0.5f;
	float delta2 = dot( delta.xy, delta.xy );
	float delta4 = delta2 * delta2;
	float deltaOffset = delta4 * flWarpAmount;
	
	return vUV + delta * deltaOffset;
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
	public float DefaultWarpAmount { get; set; } = 1.0f;

	[Output( typeof( Vector2 ) )]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		if ( compiler.Graph.Domain != ShaderDomain.PostProcess )
		{
			return NodeResult.Error( $"'{DisplayInfo.Name}' node is only ment to be used in the '{ShaderDomain.PostProcess}' domain." );
		}

		var coords = compiler.Result( ScreenUVInput );
		var warpamount = compiler.ResultOrDefault( WarpAmountInput, DefaultWarpAmount );

		string func = compiler.RegisterHLSLFunction( ScreenWarp, "Warp" );
		string funcCall = compiler.ResultHLSLFunction( func, $"{(coords.IsValid ? $"{coords.Cast( 2 )}" : "i.vPositionSs.xy / g_vRenderTargetSize")}, {warpamount}" );

		return new NodeResult( ResultType.Vector2, funcCall );
	};
}
