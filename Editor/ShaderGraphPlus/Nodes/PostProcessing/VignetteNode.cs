
namespace ShaderGraphPlus.Nodes;

/// <summary>
/// Adds a vignette shadow to the edges of the image.
/// </summary>
[Title( "Vignette" ), Category( "PostProcessing/Effects" ), Icon( "vignette" )]
public class VignetteNode : ShaderNodePlus
{
	[Hide]
	public static string Vignette => @"
float Vignette( float2 vUV , float flVignetteIntensity, float flVignetteOpacity )
{
	vUV *= 1.0f - vUV.xy;
	float vignette = vUV.x * vUV.y * 15.0f;
	return pow( vignette, flVignetteIntensity * flVignetteOpacity );
}
";

	[Input( typeof( Vector2 ) ), Title( "Coords" )]
	[Hide]
	public NodeInput ScreenUVInput { get; set; }

	[Input( typeof( float ) ), Title( "Vignette Intensity" )]
	[Hide]
	public NodeInput VignetteIntensityInput { get; set; }

	[Input( typeof( float ) ), Title( "Vignette Opacity" )]
	[Hide]
	public NodeInput VignetteOpacityInput { get; set; }

	[InputDefault( nameof( VignetteIntensityInput ) )]
	[Title( "Vignette Intensity" )]
	public float DefaultVignetteIntensity { get; set; } = 1.0f;

	[InputDefault( nameof( VignetteOpacityInput ) )]
	[Title( "Vignette Opacity" )]
	public float DefaultVignetteOpacity { get; set; } = 0.5f;

	[Output( typeof( float ) )]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		if ( compiler.Graph.Domain != ShaderDomain.PostProcess )
		{
			return NodeResult.Error( $"'{DisplayInfo.Name}' node is only ment to be used in the '{ShaderDomain.PostProcess}' domain." );
		}

		var coords = compiler.Result( ScreenUVInput );
		var vignetteintensity = compiler.ResultOrDefault( VignetteIntensityInput, DefaultVignetteIntensity );
		var vignetteopacity = compiler.ResultOrDefault( VignetteOpacityInput, DefaultVignetteOpacity );

		string func = compiler.RegisterHLSLFunction( Vignette, "Vignette" );
		string funcCall = compiler.ResultHLSLFunction( func, $"{(coords.IsValid ? $"{coords.Cast( 2 )}" : "i.vPositionSs.xy / g_vRenderTargetSize")}, {vignetteintensity}, {vignetteopacity}" );

		return new NodeResult( ResultType.Float, funcCall );
	};
}
