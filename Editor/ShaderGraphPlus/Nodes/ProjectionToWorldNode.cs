
namespace ShaderGraphPlus.Nodes;

[Title( "Projection To World" ), Category( "Variables/Matrix" ), Icon( "apps" )]
public sealed class ProjectionToWorldNode : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.GlobalVariableNode;

	[JsonIgnore, Hide, Browsable( false )]
	public override bool CanPreview => false;

	[Output( typeof( Float4x4 ) ), Title( "Matrix" )]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		return new NodeResult( ResultType.Float4x4, "g_matProjectionToWorld", true );
	};
}
