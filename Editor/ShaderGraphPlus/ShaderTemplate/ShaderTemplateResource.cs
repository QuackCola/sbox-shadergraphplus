using Editor;
using Editor.Inspectors;
using static Editor.Inspectors.AssetInspector;

namespace ShaderGraphPlus;

[AssetType( Name = "Shader Template", Extension = "shdrtpl" )]
public sealed class ShaderTemplateResource
{
	[TabPage( "Supported Material Inputs" )]
	public bool Albedo { get; set; } = true;

	[TabPage( "Supported Material Inputs" )]
	[ShowIf( nameof( LitShading ), true )]
	public bool Emission { get; set; } = true;

	[TabPage( "Supported Material Inputs" )]
	public bool Opacity { get; set; } = true;

	[TabPage( "Supported Material Inputs" )]
	[ShowIf( nameof( LitShading ), true )]
	public bool Normal { get; set; } = true;

	[TabPage( "Supported Material Inputs" )]
	[ShowIf( nameof( LitShading ), true )]
	public bool Roughness { get; set; } = true;

	[TabPage( "Supported Material Inputs" )]
	[ShowIf( nameof( LitShading ), true )]
	public bool Metalness { get; set; } = true;

	[TabPage( "Supported Material Inputs" )]
	[ShowIf( nameof( LitShading ), true )]
	public bool AmbientOcclusion { get; set; } = true;

	[TabPage( "Supported Material Inputs" )]
	public bool PositionOffset { get; set; } = true;

	[TabPage( "Supported Shading Models" )]
	public bool LitShading { get; set; } = true;

	[TabPage( "Supported Shading Models" )]
	public bool UnlitShading { get; set; } = true;

	[TabPage( "Supported Blend Modes" )]
	public bool OpaqueBlend { get; set; } = true;

	[TabPage( "Supported Blend Modes" )]
	public bool MaskedBlend { get; set; } = true;

	[TabPage( "Supported Blend Modes" )]
	public bool TranslucentBlend { get; set; } = true;

	[TabPage( "Supported Blend Modes" )]
	public bool DynamicBlend { get; set; } = true;

	[TabPage( "Code" ), TextArea]
	public string Code { get; set; } = "";
}
