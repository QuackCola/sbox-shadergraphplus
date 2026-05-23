
namespace ShaderGraphPlus.Nodes;

/// <summary>
/// If True, do this, if False, do that.
/// </summary>
[Title( "Branch" ), Category( "Utility/Logic" ), Icon( "alt_route" )]
public sealed class BranchNode : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.LogicNode;

	[Hide]
	private bool IsSubgraph => (Graph is ShaderGraphPlus shaderGraph && shaderGraph.IsSubgraph);

	[Title( "Predicate" )]
	[Input( typeof( bool ) ), Hide]
	public NodeInput InputPredicate { get; set; }

	[Input, Hide]
	public NodeInput True { get; set; }

	[Input, Hide]
	public NodeInput False { get; set; }

	[Title( "Default Predicate" )]
	[InputDefault( nameof( InputPredicate ) )]
	public bool Enabled { get; set; } = true;

	[Output]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		var results = compiler.Result( True, False, 0.0f, 0.0f );
		var resultPredicate = compiler.ResultOrDefault( InputPredicate, Enabled );

		return new NodeResult( results.Item1.ResultType, $"{resultPredicate} ? {results.Item1} : {results.Item2}" );
	};
}
