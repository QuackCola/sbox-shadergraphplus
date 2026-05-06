namespace ShaderGraphPlus.Diagnostics;

public static class SGPLogger
{
	private static Logger _log => new( ShaderGraphPlusGlobals.CleanName );

	/// <summary>
	/// Name of this logger.
	/// </summary>
	public static string Name => ShaderGraphPlusGlobals.CleanName;

	internal static IEnumerable<MainWindow> GetAllShadergraphPlusWindows()
	{
		return Editor.Window.All.OfType<MainWindow>();
	}

	public static void Info( string message, bool shouldLog = true )
	{
		if ( shouldLog )
			_log.Info( message );
	}

	public static void Trace( string message, bool shouldLog = true )
	{
		if ( shouldLog )
			_log.Trace( message );
	}

	public static void Warning( string message, bool shouldLog = true )
	{
		if ( shouldLog )
			_log.Warning( message );
	}

	public static void Error( string message, bool shouldLog = true )
	{
		if ( shouldLog )
			_log.Error( message );
	}

	public static void Error( Exception exception, string message, bool shouldLog = true )
	{
		if ( shouldLog )
			_log.Error( exception, message );
	}
}
