using ShaderGraphPlus.Nodes;
using System.Text.Json.Nodes;

namespace ShaderGraphPlus;

public interface ISGPJsonUpgradeable
{
	[Hide]
	public int Version { get; }
}

partial class ShaderGraphPlus
{
	/// <summary>
	/// Json Keys used for serialization and deserialization.
	/// </summary>
	internal static class JsonKeys
	{
		internal const string Version = "__version";
		internal const string Class = "_class";
		internal const string NodeArray = "nodes";
		internal const string ParameterArray = "parameters";
	}

	internal static JsonSerializerOptions SerializerOptions( bool indented = false )
	{
		var options = new JsonSerializerOptions
		{
			WriteIndented = indented,
			PropertyNameCaseInsensitive = true,
			NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
			DefaultIgnoreCondition = JsonIgnoreCondition.Never,
			ReadCommentHandling = JsonCommentHandling.Skip,
		};

		options.Converters.Add( new JsonStringEnumConverter( null, true ) );

		return options;
	}

	public string Serialize()
	{
		var doc = new JsonObject();
		var options = SerializerOptions( true );

		SerializeObject( this, doc, options );
		SerializeNodes( Nodes, doc, options );
		SerializeParameters( Parameters, doc, options );

		doc.Add( JsonKeys.Version, JsonSerializer.SerializeToNode( Version, options ) );

		return doc.ToJsonString( options );
	}

	public void Deserialize( string json, string subgraphPath = null, string fileName = "" )
	{
		using var doc = JsonDocument.Parse( json );
		var root = doc.RootElement;
		var options = SerializerOptions();
		var fileVersion = GetGraphVersion( root );

		if ( HandleGraphUpgrades( fileVersion, Json.ParseToJsonObject( json ), options, out JsonElement upgradedElement ) )
		{
			root = upgradedElement;
		}

		DeserializeObject( this, root, options );
		DeserializeParameters( root, options );
		DeserializeNodes( root, options, subgraphPath, fileVersion );
	}

	private bool HandleGraphUpgrades( int fileVersion, JsonObject json, JsonSerializerOptions options, out JsonElement upgradedElement )
	{
		upgradedElement = default;

		if ( fileVersion >= Version )
			return false;

		ShaderGraphPlusJsonUpgrader.Upgrade( fileVersion, json, typeof( ShaderGraphPlus ) );

		upgradedElement = JsonSerializer.Deserialize<JsonElement>( json.ToJsonString(), options );

		return true;
	}

	public IEnumerable<BaseNodePlus> DeserializeNodes( string json, bool useCurrentVersion = false )
	{
		using var doc = JsonDocument.Parse( json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip } );
		var root = doc.RootElement;
		var fileVersion = GetGraphVersion( root, useCurrentVersion );

		return DeserializeNodes( root, SerializerOptions(), null, fileVersion );
	}

	private static void DeserializeObject( object obj, JsonElement doc, JsonSerializerOptions options )
	{
		var type = obj.GetType();
		var properties = type.GetProperties( BindingFlags.Instance | BindingFlags.Public )
			.Where( x => x.GetSetMethod() != null );

		// start deserilzing each property of the current type we are deserialzing. Also handle 
		// any property that needs upgrading.
		foreach ( var jsonProperty in doc.EnumerateObject() )
		{
			var propertyInfo = properties.FirstOrDefault( x =>
			{
				var propName = x.Name;

				if ( x.GetCustomAttribute<JsonPropertyNameAttribute>() is JsonPropertyNameAttribute jpna )
					propName = jpna.Name;

				return string.Equals( propName, jsonProperty.Name, StringComparison.OrdinalIgnoreCase );
			} );

			if ( propertyInfo == null )
				continue;

			if ( propertyInfo.CanWrite == false )
				continue;

			if ( propertyInfo.IsDefined( typeof( JsonIgnoreAttribute ) ) )
				continue;

			// Handle any types that use the ISGPJsonUpgradeable interface
			if ( typeof( ISGPJsonUpgradeable ).IsAssignableFrom( propertyInfo.PropertyType ) )
			{
				var propertyTypeInstance = EditorTypeLibrary.Create( propertyInfo.PropertyType.Name, propertyInfo.PropertyType );
				int oldVersionNumber = 0;

				// if we have a valid version then set oldVersionNumber otherwise just use a version of 0.
				if ( jsonProperty.Value.TryGetProperty( JsonKeys.Version, out var versionElement ) )
				{
					oldVersionNumber = versionElement.GetInt32();
				}
				else
				{
					SGPLogger.Warning( $"Failed to get property \"{type}\" upgradeable version. defaulting to \"0\"", ConCommands.VerboseJsonUpgrader );
				}

				// Dont even bother upgrading if we dont need to.
				if ( propertyTypeInstance is ISGPJsonUpgradeable upgradeable && oldVersionNumber < upgradeable.Version )
				{
					var upgradedElement = UpgradeJsonUpgradeable( oldVersionNumber, upgradeable, propertyInfo.PropertyType, jsonProperty, options );

					propertyInfo.SetValue( obj, JsonSerializer.Deserialize( upgradedElement.GetRawText(), propertyInfo.PropertyType, options ) );

					// Continue to the next jsonProperty :)
					continue;
				}
			}

			propertyInfo.SetValue( obj, JsonSerializer.Deserialize( jsonProperty.Value.GetRawText(), propertyInfo.PropertyType, options ) );
		}
	}

	private IEnumerable<BaseNodePlus> DeserializeNodes( JsonElement doc, JsonSerializerOptions options, string subgraphPath = null, int graphFileVersion = -1 )
	{
		var nodes = new Dictionary<string, BaseNodePlus>();
		var identifiers = _nodes.Count > 0 ? new Dictionary<string, string>() : null;
		var connections = new List<(IPlugIn Plug, NodeInput Value)>();

		var arrayProperty = doc.GetProperty( JsonKeys.NodeArray );
		foreach ( var element in arrayProperty.EnumerateArray() )
		{
			var typeName = element.GetProperty( JsonKeys.Class ).GetString();
			var typeDesc = EditorTypeLibrary.GetType<BaseNodePlus>( typeName );
			var type = new ClassNodeType( typeDesc );

			BaseNodePlus node;
			if ( typeDesc is null )
			{
				SGPLogger.Error( $"Missing Node : \"{typeName}\"" );

				var missingNode = new MissingNode( typeName, element );
				node = missingNode;
				DeserializeObject( node, element, options );

				nodes.Add( node.Identifier, node );

				AddNode( node );
			}
			else
			{
				node = EditorTypeLibrary.Create<BaseNodePlus>( typeName );
				DeserializeObject( node, element, options );

				if ( identifiers != null && _nodes.ContainsKey( node.Identifier ) )
				{
					identifiers.Add( node.Identifier, node.NewIdentifier() );
				}

				// Early case to hook up the graph to node early so that the connections are actually loaded and dont break on load.
				if ( node is EnumFeatureSwitchNode || node is SubgraphOutput )
				{
					node.Graph = this;
				}

				if ( node is BaseNodePlus.IInitializeNode initializeableNode )
				{
					initializeableNode.InitializeNode();
				}

				if ( node is SubgraphNode subgraphNode )
				{
					if ( !Editor.FileSystem.Content.FileExists( subgraphNode.SubgraphPath ) )
					{
						var missingNode = new MissingNode( typeName, element );
						node = missingNode;
						DeserializeObject( node, element, options );
					}
					else
					{
						subgraphNode.OnNodeCreated();
					}
				}

				foreach ( var input in node.Inputs )
				{
					if ( !element.TryGetProperty( input.Identifier, out var connectedElem ) )
						continue;

					var connected = connectedElem
						.Deserialize<NodeInput?>();

					if ( connected is { IsValid: true } )
					{
						var connection = connected.Value;
						if ( !string.IsNullOrEmpty( subgraphPath ) )
						{
							connection = new()
							{
								Identifier = connection.Identifier,
								Output = connection.Output,
								Subgraph = subgraphPath
							};
						}

						connections.Add( (input, connection) );
					}
				}

				nodes.Add( node.Identifier, node );
				AddNode( node );
			}
		}

		foreach ( var (input, value) in connections )
		{
			var outputIdent = identifiers?.TryGetValue( value.Identifier, out var newIdent ) ?? false
				? newIdent : value.Identifier;

			if ( nodes.TryGetValue( outputIdent, out var node ) )
			{
				var output = node.Outputs.FirstOrDefault( x => x.Identifier == value.Output );

				if ( output is null )
				{
					// Check for Aliases
					foreach ( var op in node.Outputs )
					{
						if ( op is not BasePlugOut plugOut ) continue;

						var aliasAttr = plugOut.Info.Property?.GetCustomAttribute<AliasAttribute>();
						if ( aliasAttr is not null && aliasAttr.Value.Contains( value.Output ) )
						{
							output = plugOut;
							break;
						}
					}
				}
				input.ConnectedOutput = output;
			}
		}

		return nodes.Values;
	}

	public IEnumerable<BlackboardParameter> DeserializeParameters( string json )
	{
		using var doc = JsonDocument.Parse( json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip } );
		var root = doc.RootElement;

		return DeserializeParameters( root, SerializerOptions() );
	}

	private IEnumerable<BlackboardParameter> DeserializeParameters( JsonElement doc, JsonSerializerOptions options )
	{
		var parameters = new Dictionary<string, BlackboardParameter>();

		if ( doc.TryGetProperty( JsonKeys.ParameterArray, out var arrayProperty ) )
		{
			foreach ( var element in arrayProperty.EnumerateArray() )
			{
				var typeName = element.GetProperty( JsonKeys.Class ).GetString();
				var typeDesc = EditorTypeLibrary.GetType<BlackboardParameter>( typeName );
				var type = new ClassBlackboardParameterType( typeDesc );

				BlackboardParameter parameter;

				if ( typeDesc != null )
				{
					parameter = EditorTypeLibrary.Create<BlackboardParameter>( typeName );
					DeserializeObject( parameter, element, options );

					if ( string.IsNullOrWhiteSpace( parameter.Name ) )
					{
						var name = $"{(IsSubgraph ? "SubgraphInput" : "MaterialParameter")}";
						var id = name;
						int count = 0;

						while ( parameters.ContainsKey( id ) )
						{
							id = $"{name}_{count++}";
						}

						parameter.Name = id;
					}

					parameters.Add( parameter.Name, parameter );

					AddParameter( parameter );
				}
			}
		}

		return parameters.Values;
	}

	public string SerializeNodes()
	{
		return SerializeNodes( Nodes );
	}

	public string UndoStackSerialize()
	{
		var doc = new JsonObject();
		var options = SerializerOptions();

		doc = SerializeNodes( Nodes, doc );

		return SerializeParameters( Parameters, doc ).ToJsonString( options );
	}

	public string SerializeNodes( IEnumerable<BaseNodePlus> nodes )
	{
		var doc = new JsonObject();
		var options = SerializerOptions();

		SerializeNodes( nodes, doc, options );

		return doc.ToJsonString( options );
	}

	public JsonObject SerializeNodes( IEnumerable<BaseNodePlus> nodes, JsonObject doc )
	{
		var options = SerializerOptions();

		SerializeNodes( nodes, doc, options );

		return doc;
	}

	private static void SerializeObject( object obj, JsonObject doc, JsonSerializerOptions options, Dictionary<string, string> identifiers = null )
	{
		var type = obj.GetType();
		var properties = type.GetProperties( BindingFlags.Instance | BindingFlags.Public )
			.Where( x => x.GetSetMethod() != null );

		foreach ( var property in properties )
		{
			if ( !property.CanRead )
				continue;

			if ( property.PropertyType == typeof( NodeInput ) )
				continue;

			if ( property.IsDefined( typeof( JsonIgnoreAttribute ) ) )
				continue;

			var propertyName = property.Name;
			if ( property.GetCustomAttribute<JsonPropertyNameAttribute>() is { } jpna )
				propertyName = jpna.Name;

			var propertyValue = property.GetValue( obj );
			if ( propertyName == "Identifier" && propertyValue is string identifier )
			{
				if ( identifiers.TryGetValue( identifier, out var newIdentifier ) )
				{
					propertyValue = newIdentifier;
				}
			}

			if ( propertyName != "Version" )
			{
				doc.Add( propertyName, JsonSerializer.SerializeToNode( propertyValue, options ) );
			}
		}

		if ( obj is IGraphNode node )
		{
			foreach ( var input in node.Inputs )
			{
				if ( input.ConnectedOutput is not { } output )
					continue;

				doc.Add( input.Identifier, JsonSerializer.SerializeToNode( new NodeInput
				{
					Identifier = identifiers?.TryGetValue( output.Node.Identifier, out var newIdent ) ?? false ? newIdent : output.Node.Identifier,
					Output = output.Identifier,
				} ) );
			}
		}
	}

	private static void SerializeNodes( IEnumerable<BaseNodePlus> nodes, JsonObject doc, JsonSerializerOptions options )
	{
		var identifiers = new Dictionary<string, string>();
		foreach ( var node in nodes )
		{
			identifiers.Add( node.Identifier, $"{identifiers.Count}" );
		}

		var nodeArray = new JsonArray();

		foreach ( var node in nodes )
		{
			if ( node is DummyNode )
				continue;

			var type = node.GetType();
			var nodeObject = new JsonObject { { JsonKeys.Class, type.Name } };

			SerializeObject( node, nodeObject, options, identifiers );

			nodeArray.Add( nodeObject );
		}

		doc.Add( JsonKeys.NodeArray, nodeArray );
	}

	public string SerializeParameters()
	{
		return SerializeParameters( Parameters );
	}

	private string SerializeParameters( IEnumerable<BlackboardParameter> parameters )
	{
		var doc = new JsonObject();
		var options = SerializerOptions();

		SerializeParameters( parameters, doc, options );

		return doc.ToJsonString( options );
	}

	private JsonObject SerializeParameters( IEnumerable<BlackboardParameter> parameters, JsonObject doc )
	{
		var options = SerializerOptions();

		SerializeParameters( parameters, doc, options );

		return doc;
	}

	private static void SerializeParameters( IEnumerable<BlackboardParameter> parameters, JsonObject doc, JsonSerializerOptions options )
	{
		var parameterArray = new JsonArray();

		foreach ( var parameter in parameters )
		{
			var type = parameter.GetType();
			var parameterObject = new JsonObject { { JsonKeys.Class, type.Name } };

			SerializeObject( parameter, parameterObject, options );

			parameterArray.Add( parameterObject );
		}

		doc.Add( JsonKeys.ParameterArray, parameterArray );
	}

}
