
namespace ShaderGraphPlus.Nodes;

/// <summary>
/// Camera position and shit
/// </summary>
[Title( "Camera" ), Category( "Variables" ), Icon( "photo_camera" )]
public sealed class Camera : ShaderNodePlus
{
	[JsonIgnore, Hide, Browsable( false )]
	public override Color NodeTitleColor => ShaderGraphPlusTheme.NodeHeaderColors.GlobalVariableNode;

	/// <summary>
	/// Camera position in world space
	/// </summary>
	[Output( typeof( Vector3 ) ), Title( "Position" )]
	[Hide]
	public static NodeResult.Func WorldPosition => ( GraphCompiler compiler ) => new( ResultType.Vector3, "g_vCameraPositionWs" );

	/// <summary>
	/// Camera direction in world space
	/// </summary>
	[Output( typeof( Vector3 ) )]
	[Hide]
	public static NodeResult.Func Direction => ( GraphCompiler compiler ) => new( ResultType.Vector3, "g_vCameraDirWs" );

	/// <summary>
	/// Camera up direction in world space
	/// </summary>
	[Output( typeof( Vector3 ) )]
	[Hide]
	public static NodeResult.Func DirectionUp => ( GraphCompiler compiler ) => new( ResultType.Vector3, "g_vCameraUpDirWs" );

	/// <summary>
	/// Camera FOV in radians
	/// </summary>
	[Output( typeof( float ) )]
	[Hide]
	public static NodeResult.Func FOV => ( GraphCompiler compiler ) => new( ResultType.Float, "g_flCameraFOV" );

	/// <summary>
	/// Camera's near plane
	/// </summary>
	[Output( typeof( float ) )]
	[Hide]
	public static NodeResult.Func NearPlane => ( GraphCompiler compiler ) => new( ResultType.Float, "g_flNearPlane" );

	/// <summary>
	/// Camera's far plane
	/// </summary>
	[Output( typeof( float ) )]
	[Hide]
	public static NodeResult.Func FarPlane => ( GraphCompiler compiler ) => new( ResultType.Float, "g_flFarPlane" );
}
