
namespace ShaderGraphPlus.Nodes;

/// <summary>
/// Projection-space coordinates converted to view space
/// </summary>
[Title( "Projection To View" ), Category( "Variables/Matrix" ), Icon( "apps" )]
public sealed class ProjectionToViewNode : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.GlobalVariableNode;

	[JsonIgnore, Hide, Browsable( false )]
	public override bool CanPreview => false;

	[Output( typeof( Float4x4 ) ), Title( "Matrix" )]
	[Hide]
	public static NodeResult.Func Result => ( GraphCompiler compiler ) => new NodeResult( ResultType.Float4x4, "g_matProjectionToView" );
}
