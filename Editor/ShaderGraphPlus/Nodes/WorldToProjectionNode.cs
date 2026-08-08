
namespace ShaderGraphPlus.Nodes;

/// <summary>
/// World-space coordinates converted to view-space. This coordinate system is built relative to the camera, with it as the origin
/// </summary>
[Title( "World To Projection" ), Category( "Variables/Matrix" ), Icon( "apps" )]
public sealed class WorldToProjectionNode : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.GlobalVariableNode;

	[JsonIgnore, Hide, Browsable( false )]
	public override bool CanPreview => false;

	[Output( typeof( Float4x4 ) ), Title( "Matrix" )]
	[Hide]
	public static NodeResult.Func Result => ( GraphCompiler compiler ) => new NodeResult( ResultType.Float4x4, "g_matWorldToProjection" );
}
