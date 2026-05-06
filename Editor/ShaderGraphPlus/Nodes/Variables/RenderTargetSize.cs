
namespace ShaderGraphPlus.Nodes;

/// <summary>
///
/// </summary>
[Title( "Render Target Size" ), Category( "Variables" ), Icon( "texture" )]
public sealed class RenderTargetSizeNode : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.GlobalVariableNode;

	[Output( typeof( Vector2 ) ), Title( "Render Target Size" )]
	[Hide]
	public static NodeResult.Func RenderTargetSize => ( GraphCompiler compiler ) => new( ResultType.Vector2, "g_vRenderTargetSize" );
}
