namespace ShaderGraphPlus;

public static partial class ShaderTemplate
{
	[Flags]
	public enum SupportFlags
	{
		[Hide]
		None = 0,

		OpaqueBlend = 1 << 0,
		MaskedBlend = 1 << 1,
		TranslucentBlend = 1 << 2,

		LitShading = 1 << 3,
		UnlitShading = 1 << 4,

		RenderFaceFront = 1 << 5,
		RenderFaceBack = 1 << 6,
		RenderFaceBoth = 1 << 7,

		[Hide]
		AllRenderFace = RenderFaceFront | RenderFaceBack | RenderFaceBoth,

		[Hide]
		AllBlend = OpaqueBlend | MaskedBlend | TranslucentBlend,

		[Hide]
		All = OpaqueBlend | MaskedBlend | TranslucentBlend | LitShading | UnlitShading | RenderFaceFront | RenderFaceBack | RenderFaceBoth
	}
}
