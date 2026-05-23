
namespace ShaderGraphPlus.Nodes;

/// <summary>
/// Compare Input 'A' with Input 'B' and output the input from either 'True' or 'False' based on the result of the comparison.
/// </summary>
[Title( "Comparison" ), Category( "Utility/Logic" ), Icon( "question_mark" )]
public sealed class ComparisonNode : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.LogicNode;

	[Hide]
	public override string Title => $"{DisplayInfo.For( this ).Name} (A {Op} B)";

	[Hide]
	private bool IsSubgraph => (Graph is ShaderGraphPlus shaderGraph && shaderGraph.IsSubgraph);

	[Input, Hide]
	public NodeInput True { get; set; }

	[Input, Hide]
	public NodeInput False { get; set; }

	[Input, Hide]
	public NodeInput A { get; set; }

	[Input, Hide]
	public NodeInput B { get; set; }

	public enum OperatorType
	{
		Equal,
		NotEqual,
		GreaterThan,
		LessThan,
		GreaterThanOrEqual,
		LessThanOrEqual
	}

	public OperatorType Operator { get; set; } = OperatorType.Equal;

	[Hide]
	private string Op
	{
		get
		{
			return Operator switch
			{
				OperatorType.Equal => "==",
				OperatorType.NotEqual => "!=",
				OperatorType.GreaterThan => ">",
				OperatorType.LessThan => "<",
				OperatorType.GreaterThanOrEqual => ">=",
				OperatorType.LessThanOrEqual => "<=",
				_ => throw new System.NotImplementedException(),
			};
		}
	}

	[Output( typeof( bool ) ), Title( "Predicate" )]
	[Description( "Either 'true' or 'false' depending on the result of the comparison with Input 'A' and Input 'B'." )]
	[Hide]
	public NodeResult.Func ResultPredicate => ( GraphCompiler compiler ) =>
	{
		var resultA = compiler.ResultOrDefault( A, 0.0f );
		var resultB = compiler.ResultOrDefault( B, 0.0f );

		return new NodeResult( ResultType.Bool, $"{resultA} {Op} {resultB}" );
	};

	[Output, Title( "Result" )]
	[Description( "Result from either the 'True' or 'False' inputs depending on the result of the comparison with Input 'A' and Input 'B'." )]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		var results = compiler.Result( True, False, 0.0f, 0.0f );
		var resultA = compiler.ResultOrDefault( A, 0.0f );
		var resultB = compiler.ResultOrDefault( B, 0.0f );

		var result = $"{resultA.Cast( 1 )} {Op} {resultB.Cast( 1 )} ? {results.Item1} : {results.Item2}";

		return new NodeResult( results.Item1.ResultType, $"{result}" );
	};
}
