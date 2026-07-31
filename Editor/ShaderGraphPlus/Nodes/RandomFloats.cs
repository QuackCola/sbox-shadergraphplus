namespace ShaderGraphPlus.Nodes;

/// <summary>
/// Random floats in each component, values change every frame
/// </summary>
[Title( "Random Floats" ), Category( "Variables" ), Icon( "casino" )]
public sealed class RandomFloats : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.GlobalVariableNode;

	[Output( typeof( Vector4 ) ), Title( "XYZW" )]
	[Hide]
	public static NodeResult.Func Result => ( GraphCompiler compiler ) => new NodeResult( ResultType.Vector4, "g_vRandomFloats.xyzw" );
}
