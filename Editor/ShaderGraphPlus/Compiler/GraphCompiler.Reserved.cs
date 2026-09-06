namespace ShaderGraphPlus;

// Reserved names for things so we can show an error to the user
// when they try to name a parameter, Feature or Combo to any name
// below.

// TODO : Hookup this stuff to the Blackboard, GraphCompiler and the
// ShaderGraphPlus editor window.

public sealed partial class GraphCompiler
{
	public static List<string> ReservedGlobalParameters => new()
	{
		// Floats
		"g_flPreviewTime",
		"g_flTime",

		// Vectors
		"g_vFrameBufferCopyInvSizeAndUvScale",
		"g_vInvProjRow3",
		"g_vRandomFloats",
		"g_vRenderTargetSize",

		"g_vViewportSize",
		"g_vInvViewportSize",
		"g_vViewportOffset",
		"g_flViewportMinZ",
		"g_flViewportMaxZ",

		"g_vCameraPositionWs",
		"g_vCameraDirWs",
		"g_vCameraUpDirWs",
		"g_flCameraFOV",
		"g_flNearPlane",
		"g_flFarPlane",

		// Matrices
		"g_matViewToProjection",
		"g_matWorldToProjection",
		"g_matProjectionToView",
		"g_matProjectionToWorld",
		"g_matWorldToView",

		// Textures
		"g_tFrameBufferCopyTexture",
		"g_tColorBuffer",

		// Attributes
		"bWantsFBCopyTexture",
	};

	public static List<string> ReservedFeatureNames => new()
	{

	};

	public static List<string> ReservedComboNames => new()
	{

	};

	public static List<string> ReservedFunctionNames => new()
	{

	};
}
