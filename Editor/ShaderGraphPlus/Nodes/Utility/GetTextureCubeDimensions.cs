namespace ShaderGraphPlus.Nodes;

/// <summary>
/// Get the dimensions of a TextureCube.
/// </summary>
[Title( "Get TextureCube Dimensions" ), Category( "Textures" ), Icon( "straighten" )]
public sealed class GetTextureCubeDimensions : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.FunctionNode;

	[Title( "TextureCube" )]
	[Input( typeof( Texture ) )]
	[Hide]
	public NodeInput TextureInput { get; set; }

	[Title( "Mip Level" )]
	[Input( typeof( int ) )]
	[Hide]
	public NodeInput MipLevelInput { get; set; }

	[JsonIgnore, Hide]
	public override bool CanPreview => false;

	[InputDefault( nameof( MipLevelInput ) )]
	public int DefaultMipLevel { get; set; } = 0;

	public GetTextureCubeDimensions()
	{
		ExpandSize = new Vector2( 8, 0 );
	}

	[Output( typeof( int ) )]
	[Title( "Size" )]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		ClearError();

		var textureResult = compiler.Result( TextureInput );
		if ( !textureResult.IsValid )
		{
			return NodeResult.MissingInput( "TextureCube" );
		}
		else if ( textureResult.ResultType != ResultType.TextureCube )
		{
			return NodeResult.IncorrectInputType( "TextureCube", ResultType.TextureCube );
		}

		var miplevelResult = compiler.ResultOrDefault( MipLevelInput, DefaultMipLevel );

		return new NodeResult( ResultType.Int, $"TextureDimensionsCube({textureResult}, {miplevelResult})", constant: false );
	};
}
