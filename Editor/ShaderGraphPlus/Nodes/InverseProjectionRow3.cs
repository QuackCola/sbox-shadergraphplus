
namespace ShaderGraphPlus.Nodes;

/// <summary>
/// Row 3 of the inverse projection matrix.
/// </summary>
[Title( "Inverse Projection Row 3" ), Category( "Variables" ), Icon( "apps" )]
public sealed class InverseProjectionRow3 : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.GlobalVariableNode;

	[Output( typeof( Vector4 ) ), Title( "Result" )]
	[Hide]
	public static NodeResult.Func Result => ( GraphCompiler compiler ) => new( ResultType.Vector4, "g_vInvProjRow3" );
}
