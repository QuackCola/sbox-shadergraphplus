namespace ShaderGraphPlus.Nodes;

/// <summary>
/// Direction and color of the main directional light (sun). Direction points the way the light
/// travels, negate it to get the direction towards the sun.
/// </summary>
[Title( "Sun" ), Category( "Variables" ), Icon( "light_mode" )]
public sealed class Sun : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.GlobalVariableNode;

	[Output( typeof( Vector3 ) ), Title( "Direction" )]
	[Hide]
	public static NodeResult.Func Direction => ( GraphCompiler compiler ) => new( ResultType.Vector3, "g_DirectionalLightDirection.xyz" );

	[Output( typeof( Vector3 ) ), Title( "Color" )]
	[Hide]
	public static NodeResult.Func Color => ( GraphCompiler compiler ) => new( ResultType.Vector3, "g_DirectionalLightColor.rgb" );
}
