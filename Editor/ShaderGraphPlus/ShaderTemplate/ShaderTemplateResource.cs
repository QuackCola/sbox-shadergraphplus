using Editor;

namespace ShaderGraphPlus;

[AssetType( Name = "Shader Template", Extension = "shdrtpl" )]
public sealed partial class ShaderTemplateResource
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
	public string Code { get; set; } = DefaultCode;

	[Hide]
	public static string DefaultCode => @"
HEADER
{{
	Description = ""{sgp_shader_description}"";
}}	

FEATURES
{{
{sgp_shader_feature_defines}
}}

MODES
{{
	Forward();
	Depth();
	ToolsShadingComplexity( ""tools_shading_complexity.shader"" );
}}

COMMON
{{
{sgp_shader_common}
}}

struct VertexInput
{{
	#include ""common/vertexinput.hlsl""
{sgp_vertex_input_data}
}};

struct PixelInput
{{
	#include ""common/pixelinput.hlsl""
{sgp_pixel_input_data}
}};

VS
{{
	#include ""common/vertex.hlsl""
{sgp_vertex_globals}{sgp_vertex_combo_rules}{sgp_vertex_functions}
	PixelInput MainVs( VertexInput v )
	{{
{sgp_vertex_code}
	}}
}}

PS
{{
	#include ""common/pixel.hlsl""
{sgp_pixel_globals}{sgp_pixel_combo_rules}{sgp_pixel_functions}
	float4 MainPs( PixelInput i ) : SV_Target0
	{{
{sgp_pixel_init}
{sgp_pixel_locals}
{sgp_pixel_material}
{sgp_pixel_output}
	}}
}}
	";
}
