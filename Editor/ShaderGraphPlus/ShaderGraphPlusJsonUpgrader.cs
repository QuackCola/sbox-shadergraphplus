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
		if ( _methods == null )
		{
			return;
		}

		foreach ( var item2 in from x in _methods
							   where x.Attribute.Type == targetType
							   orderby x.Attribute.Version
							   where x.Attribute.Version > version
							   select x )
		{
			try
			{
				MethodDescription item = item2.Method;
				object[] parameters = [json];
				item.Invoke( null, parameters );
			}
			catch ( Exception exception )
			{
				Log.Warning( exception, $"A type version upgrader ( {item2.Attribute.Type}, version {item2.Attribute.Version}) threw an exception while trying to upgrade, so we halted the upgrade." );
				break;
			}
			finally
			{
				json[ShaderGraphPlus.JsonKeys.Version] = item2.Attribute.Version;
			}
		}

	}
}
