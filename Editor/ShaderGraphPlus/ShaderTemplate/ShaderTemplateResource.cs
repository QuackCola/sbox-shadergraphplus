using Editor;
using Editor.Inspectors;
using static Editor.Inspectors.AssetInspector;

namespace ShaderGraphPlus;

[AssetType( Name = "Shader Template", Extension = "shdrtpl" )]
public sealed class ShaderTemplateResource
{
	[Feature( "Supported Material Inputs", Icon = "input" )]
	public bool Albedo { get; set; } = true;

	[Feature( "Supported Material Inputs", Icon = "input" ), ShowIf( nameof( LitShading ), true )]
	public bool Emission { get; set; } = true;

	[Feature( "Supported Material Inputs", Icon = "input" )]
	public bool Opacity { get; set; } = true;

	[Feature( "Supported Material Inputs", Icon = "input" ), ShowIf( nameof( LitShading ), true )]
	public bool Normal { get; set; } = true;

	[Feature( "Supported Material Inputs", Icon = "input" ), ShowIf( nameof( LitShading ), true )]
	public bool Roughness { get; set; } = true;

	[Feature( "Supported Material Inputs", Icon = "input" ), ShowIf( nameof( LitShading ), true )]
	public bool Metalness { get; set; } = true;

	[Feature( "Supported Material Inputs", Icon = "input" ), ShowIf( nameof( LitShading ), true )]
	public bool AmbientOcclusion { get; set; } = true;

	[Feature( "Supported Material Inputs", Icon = "input" )]
	public bool PositionOffset { get; set; } = true;

	[Feature( "Supported Shading Models", Icon = "tonality" )]
	public bool LitShading { get; set; } = true;

	[Feature( "Supported Shading Models", Icon = "tonality" )]
	public bool UnlitShading { get; set; } = true;

	[Feature( "Supported Blend Modes", Icon = "tonality" )]
	public bool OpaqueBlend { get; set; } = true;

	[Feature( "Supported Blend Modes", Icon = "tonality" )]
	public bool MaskedBlend { get; set; } = true;

	[Feature( "Supported Blend Modes", Icon = "tonality" )]
	public bool TranslucentBlend { get; set; } = true;

	[Feature( "Supported Blend Modes", Icon = "tonality" )]
	public bool DynamicBlend { get; set; } = true;

	[TextArea]
	public string Code { get; set; } = "";
}
