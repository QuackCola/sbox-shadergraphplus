namespace ShaderGraphPlus;

internal static class ConCommands
{
	public static bool VerboseDebgging { get; internal set; } = false;

	public static bool VerboseJsonUpgrader { get; internal set; } = false;

	public static bool VerboseSerialization { get; internal set; } = false;

	public static bool NodeDebugInfo { get; internal set; } = false;

	public static bool BlackboardParameterIndexDebug { get; internal set; } = false;

	public static bool BlackboardTreeReOrderDebugPrinting { get; internal set; } = false;

	private static IEnumerable<MainWindow> GetAllShaderGraphPlusWindows()
	{
		return Editor.Window.All.OfType<MainWindow>();
	}

	[ConCmd( "sgp_verbosedebug" )]
	public static void CC_VerboseDebugging( bool value )
	{
		VerboseDebgging = value;
	}

	[ConCmd( "sgp_verbosejson_upgrader" )]
	public static void CC_VerboseJsonUpgrader( bool value )
	{
		VerboseJsonUpgrader = value;
	}

	[ConCmd( "sgp_verbose_serialization" )]
	public static void CC_VerboseSerialization( bool value )
	{
		VerboseSerialization = value;
	}

	[ConCmd( "sgp_debugnodeinfo" )]
	public static void CC_DebugNodeInfo( bool value )
	{
		NodeDebugInfo = value;
	}

	[ConCmd( "sgp_debug_blackboardtree_parameter_index" )]
	public static void CC_BlackboardParameterIndexDebug( bool value )
	{
		BlackboardParameterIndexDebug = value;
	}

	[ConCmd( "sgp_debug_blackboardtree_reorder" )]
	public static void CC_BlackboardTreeReOrderDebug( bool value )
	{
		BlackboardTreeReOrderDebugPrinting = value;
	}
}
