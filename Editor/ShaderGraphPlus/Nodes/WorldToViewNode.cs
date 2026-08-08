
namespace ShaderGraphPlus.Nodes;

/// <summary>
/// World-space coordinates converted to view space.
/// </summary>
[Title( "World To View" ), Category( "Variables/Matrix" ), Icon( "apps" )]
public sealed class WorldToViewNode : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.GlobalVariableNode;

	[JsonIgnore, Hide, Browsable( false )]
	public override bool CanPreview => false;

	[Output( typeof( Float4x4 ) ), Title( "Matrix" )]
	[Hide]
	public static NodeResult.Func Result => ( GraphCompiler compiler ) => new NodeResult( ResultType.Float4x4, "g_matWorldToView" );
}
