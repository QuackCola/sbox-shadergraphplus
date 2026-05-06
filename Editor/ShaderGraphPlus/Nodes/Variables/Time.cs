
namespace ShaderGraphPlus.Nodes;

/// <summary>
/// Current time
/// </summary>
[Title( "Time" ), Category( "Variables" ), Icon( "timer" )]
public sealed class Time : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.GlobalVariableNode;

	[JsonIgnore]
	public float Value => RealTime.Now;

	[Output( typeof( float ) ), Title( "Time" )]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		return new NodeResult( ResultType.Float, compiler.IsPreview ? "g_flPreviewTime" : "g_flTime", compiler.IsNotPreview );
	};
}
