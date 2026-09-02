using System.Text.Json.Nodes;

namespace ShaderGraphPlus;

[AttributeUsage( AttributeTargets.Method )]
public class SGPJsonUpgraderAttribute : Attribute
{
	/// <summary>
	/// The version of this upgrade.
	/// </summary>
	public int Version { get; }

	/// <summary>
	/// The type we're targeting for this upgrade.
	/// </summary>
	public Type Type { get; }

	public SGPJsonUpgraderAttribute( Type type, int version )
	{
		Type = type;
		Version = version;
	}
}

internal static class ShaderGraphPlusJsonUpgrader
{
	private static (MethodDescription Method, SGPJsonUpgraderAttribute Attribute)[] _methods;

	[Event( ShaderGraphPlusGlobals.EditorEvents.ShaderGraphPlusEditorCreated, Priority = 100 )]
	private static void UpdateUpgraders()
	{
		_methods = EditorTypeLibrary.GetMethodsWithAttribute<SGPJsonUpgraderAttribute>().ToArray();
	}

	public static void Upgrade( int version, JsonObject json, Type targetType )
	{
		// This is normal, upgraders have not been initialized using UpdateUpgraders
		// it's fine to ignore this.
		if ( _methods is null )
			return;

		foreach ( var e in _methods
			.Where( x => x.Attribute.Type == targetType )
			.OrderBy( x => x.Attribute.Version )
			.Where( x => x.Attribute.Version > version ) )
		{
			try
			{
				e.Method.Invoke( null, new[] { json } );
			}
			catch ( Exception exception )
			{
				Log.Warning( exception, $"A type version upgrader ( {e.Attribute.Type}, version {e.Attribute.Version}) threw an exception while trying to upgrade, so we halted the upgrade." );
				// Let's stop trying to upgrade because something is broken.
				return;
			}
			finally
			{
				// Update our serialized version step by step.
				json[ShaderGraphPlus.JsonKeys.Version] = e.Attribute.Version;
			}
		}

	}
}
