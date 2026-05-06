
using Facepunch.ActionGraphs;

namespace ShaderGraphPlus.Nodes;

/// <summary>
/// Get the dimensions of a Texture2D.
/// </summary>
[Title( "Get Texture2D Dimensions" ), Category( "Textures" ), Icon( "straighten" )]
public sealed class GetTexture2DDimensions : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.FunctionNode;

	[Title( "Texture2D" )]
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

	public GetTexture2DDimensions()
	{
		ExpandSize = new Vector2( 8, 0 );
	}

	[Output( typeof( Vector2 ) )]
	[Title( "Size" )]
	[Hide]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		ClearError();

		var textureResult = compiler.Result( TextureInput );
		if ( !textureResult.IsValid )
		{
			return NodeResult.MissingInput( "Texture2D" );
		}
		else if ( textureResult.ResultType != ResultType.Texture2D )
		{
			return NodeResult.IncorrectInputType( "Texture2D", ResultType.Texture2D );
		}

		var miplevelResult = compiler.ResultOrDefault( MipLevelInput, DefaultMipLevel );

		return new NodeResult( ResultType.Vector2, $"TextureDimensions2D({textureResult}, {miplevelResult})", constant: false );
	};
}
