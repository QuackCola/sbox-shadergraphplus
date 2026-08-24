namespace ShaderGraphPlus;

public static partial class ShaderTemplate
{
	public const string SupportsOpaqueBlend = "SupportsOpaqueBlend";
	public const string SupportsMaskedBlend = "SupportsMaskedBlend";
	public const string SupportsTranslucentBlend = "SupportsTranslucentBlend";

	/// <summary>
	/// Supports all 3 blend modes.
	/// </summary>
	public const string SupportsAllBlend = "SupportsAllBlend";

	public const string SupportsLitShading = "SupportsLitShading";
	public const string SupportsUnlitShading = "SupportsUnlitShading";

	public const string SupportsRenderFaceFront = "SupportsRenderFaceFront";
	public const string SupportsRenderFaceBack = "SupportsRenderFaceBack";
	public const string SupportsRenderFaceBoth = "SupportsRenderFaceBoth";

	/// <summary>
	/// Supports all 3 render face modes.
	/// </summary>
	public const string SupportsAllRenderFace = "SupportsAllRenderFace";
}
