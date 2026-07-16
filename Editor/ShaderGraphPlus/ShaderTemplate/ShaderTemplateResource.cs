using Editor;
using Editor.Inspectors;
using static Editor.Inspectors.AssetInspector;

namespace ShaderGraphPlus;


[AssetType( Name = "Shader Template", Extension = "shdrtpl" )]
public sealed class ShaderTemplateResource
{
	public Dictionary<string, bool> Features { get; set; } = new()
	{
		{ "SupportsAlbedo", true},
		{ "SupportsEmission", true },
		{ "SupportsOpacity", true },
		{ "SupportsNormal", true },
		{ "SupportsRoughness", true },
		{ "SupportsMetalness", true },
		{ "SupportsAmbientOcclusion", true },
		{ "SupportsPositionOffset", true },
		{ "SupportsPixelDepthOffset", true },

		{ "SupportsLitShadingModel", true },
		{ "SupportsUnlitShadingModel", true },
		{ "SupportsCustomShadingModel", true },

		{ "SupportsOpaqueBlendMode", true },
		{ "SupportsMaskedBlendMode", true },
		{ "SupportsTranslucentBlendMode", true },
		{ "SupportsDynamicBlendMode", true }
	};

	[TextArea]
	public string Code { get; set; } = "";
}
