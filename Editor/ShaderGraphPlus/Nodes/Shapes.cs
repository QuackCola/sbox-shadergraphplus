
namespace ShaderGraphPlus.Nodes;

/// <summary>
/// Basic procedural box shape
/// </summary>
[Title( "Box Shape" ), Category( "Procedural/Shapes" ), Icon( "check_box_outline_blank" )]
public sealed class BoxShapeNode : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.FunctionNode;

	[Title( "UV" )]
	[Input( typeof( Vector2 ) )]
	[Hide]
	public NodeInput Coords { get; set; }

	[Title( "Width" )]
	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Width { get; set; }

	[Title( "Height" )]
	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Height { get; set; }

	[InputDefault( nameof( Width ) )]
	public float DefaultWidth { get; set; } = 0.5f;

	[InputDefault( nameof( Height ) )]
	public float DefaultHeight { get; set; } = 0.5f;

	[Output( typeof( float ) )]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		var inputCoords = compiler.Result( Coords );
		var width = compiler.ResultOrDefault( Width, DefaultWidth );
		var height = compiler.ResultOrDefault( Height, DefaultHeight );
		var coords = inputCoords.IsValid ? $"{inputCoords.Cast( 2 )}" : compiler.GetTextureCoordinates();

		return new NodeResult( ResultType.Float, compiler.ResultHLSLFunction( "BoxShape", $"{coords}", $"{width}", $"{height}" ) );
	};

}

/// <summary>
/// Basic procedural elipse shape
/// </summary>
[Title( "Elipse Shape" ), Category( "Procedural/Shapes" ), Icon( "category" )]
public sealed class ElipseShapeNode : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.FunctionNode;

	[Title( "UV" )]
	[Input( typeof( Vector2 ) )]
	[Hide]
	public NodeInput Coords { get; set; }

	[Title( "Width" )]
	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Width { get; set; }

	[Title( "Height" )]
	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Height { get; set; }

	[InputDefault( nameof( Width ) )]
	public float DefaultWidth { get; set; } = 0.5f;

	[InputDefault( nameof( Height ) )]
	public float DefaultHeight { get; set; } = 0.5f;

	[Output( typeof( float ) )]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		var inputCoords = compiler.Result( Coords );
		var width = compiler.ResultOrDefault( Width, DefaultWidth );
		var height = compiler.ResultOrDefault( Height, DefaultHeight );
		var coords = inputCoords.IsValid ? $"{inputCoords.Cast( 2 )}" : compiler.GetTextureCoordinates();

		return new NodeResult( ResultType.Float, compiler.ResultHLSLFunction( "ElipseShape", $"{coords}", $"{width}", $"{height}" ) );
	};

}

/// <summary>
/// Basic procedural polygon shape.
/// </summary>
[Title( "Polygon Shape" ), Category( "Procedural/Shapes" ), Icon( "category" )]
public sealed class PolygonShapeNode : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.FunctionNode;

	[Title( "UV" )]
	[Input( typeof( Vector2 ) )]
	[Hide]
	public NodeInput Coords { get; set; }

	[Title( "Sides" )]
	[Input( typeof( int ) )]
	[Hide]
	public NodeInput Sides { get; set; }

	[Title( "Width" )]
	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Width { get; set; }

	[Title( "Height" )]
	[Input( typeof( float ) )]
	[Hide]
	public NodeInput Height { get; set; }

	[InputDefault( nameof( Sides ) )]
	public int DefaultSides { get; set; } = 4;

	[InputDefault( nameof( Width ) )]
	public float DefaultWidth { get; set; } = 0.5f;

	[InputDefault( nameof( Height ) )]
	public float DefaultHeight { get; set; } = 0.5f;

	[Output( typeof( float ) )]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		var inputCoords = compiler.Result( Coords );
		var sides = compiler.ResultOrDefault( Sides, DefaultSides );
		var width = compiler.ResultOrDefault( Width, DefaultWidth );
		var height = compiler.ResultOrDefault( Height, DefaultHeight );
		
		var coords = inputCoords.IsValid ? $"{inputCoords.Cast( 2 )}" : compiler.GetTextureCoordinates();

		return new NodeResult( ResultType.Float, compiler.ResultHLSLFunction( "PolygonShape", $"{coords}", $"{sides}", $"{width}", $"{height}" ) );
	};

}

