namespace ShaderGraphPlus;

// Reserved names for things so we can show an error to the user
// when they try to name a parameter, Feature or Combo to any name
// below.

public sealed partial class GraphCompiler
{
	/// <summary>
	/// From https://sbox.game/dev/doc/rendering/shaders/reference/global-variables
	/// </summary>
	public static Dictionary<string, string> ReservedGlobalParameters => new()
	{
		// Time
		{ "PreviewTime", "g_fl" }, // Used internally for the ShaderGraphPlus Preview
		{ "Time", "g_fl" },

		// Projections
		{ "WorldToProjection", "g_mat" },
		{ "ProjectionToWorld", "g_mat" },
		{ "WorldToView", "g_mat" },
		{ "ViewToProjection", "g_mat" },
		{ "ProjectionToView", "g_mat" },
		{ "CurrFrameViewToPrevFrameProj", "g_mat" },
		{ "InvProjRow3", "g_v" },

		// Camera
		{ "CameraPositionWs", "g_v" },
		{ "CameraDirWs", "g_v" },
		{ "CameraUpDirWs", "g_v" },
		{ "CameraAngles", "g_v" },
		{ "CameraFOV", "g_fl" },
		{ "NearPlane", "g_fl" },
		{ "FarPlane", "g_fl" },

		// Viewport
		{ "ViewportMinZ", "g_fl" },
		{ "ViewportMaxZ", "g_fl" },
		{ "ViewportSize", "g_v" },
		{ "ViewportOffset", "g_v" },
		{ "RenderTargetSize", "g_v" },
		{ "InvViewportSize", "g_v" },
		{ "FrameBufferCopyInvSizeAndUvScale", "g_v" },

		// Other 
		{ "RandomFloats", "g_v" },

		// Textures
		{ "FrameBufferCopyTexture", "g_t" },
		{ "ColorBuffer", "g_t" },
	};

	public static List<string> ReservedAttributeNames => new()
	{
		"bWantsFBCopyTexture",
	};

	// TODO
	public static List<string> ReservedFeatureNames => new()
	{
	};

	// TODO
	public static List<string> ReservedComboNames => new()
	{
	};

	/// <summary>
	/// Functions from https://sbox.game/dev/doc/rendering/shaders/reference/global-functions
	/// </summary>
	public static List<string> ReservedFunctionNames => new()
	{
		// Normal Transformations
		"DecodeNormal",
		"TransformNormal",
		"NormalWorldToTangent",
		"ComputeNormalFromXY",
		"ComputeNormalFromRGTexture",
		
		// Value Remapping
		"RemapVal",
		"RemapValClamped",

		// Camera Helpers
		"CalculateCameraToPositionRayWs",
		"CalculateCameraToPositionDirWs",
		"CalculateCameraToPositionRayTs",
		"CalculateCameraToPositionDirTs",
		"CalculateCameraReflectionDirWs",
		"CalculateDistanceToCamera",

		// Projection Transforms
		"Position4WsToVs",
		"Position3WsToVs",
		"Vector3WsToVs",
		"Vector3VsToWs",
		"Position4VsToPs",
		"Position3VsToPs",
		"Position4WsToPs",
		"Position3WsToPs",

		// Viewport UV helpers
		"CalculateViewportUvFromInvSize",
		"CalculateViewportUv",

		// sRGB Gamma Conversions
		"SrgbGammaToLinear",
		"SrgbLinearToGamma",

		// Color Manipulations
		"Luminance",
		"SaturateColor",

		// RGB <-> HSV conversions
		"RgbToHsv",
		"HsvToRgb",

		// Rotation Matrix Helpers
		"MatrixBuildRotationAboutAxisRadians",
		"MatrixBuildRotationAboutAxis",
		"RotationMatrixFromAngles",
	};
}
