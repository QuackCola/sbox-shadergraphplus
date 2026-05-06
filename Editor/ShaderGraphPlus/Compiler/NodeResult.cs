namespace ShaderGraphPlus;

public enum ResultType
{
	Invalid,
	Bool,
	Int,
	Float,
	Vector2,
	Vector3,
	Vector4,
	Float2x2,
	Float3x3,
	Float4x4,
	Sampler,
	Texture2D,
	TextureCube,
	Gradient,
	VoidFunction,
}

internal enum MetadataType
{
	ComboSwitchBody
}

public struct NodeResult : IValid
{
	public delegate NodeResult Func( GraphCompiler compiler );
	public string Code { get; private set; }
	public ResultType ResultType { get; private set; } = ResultType.Invalid;
	public string[] Errors { get; private init; }
	public string[] Warnings { get; private init; }
	public int Components { get; private set; }

	public int PreviewID { get; private set; }
	public string VoidLocalTargetID { get; private set; }

	public readonly bool IsValid
	{
		get
		{
			if ( IsMetaDataResult )
			{
				return ResultType != ResultType.Invalid && Metadata.Any();
			}
			else
			{
				return ResultType != ResultType.Invalid && !string.IsNullOrWhiteSpace( Code );
			}
		}
	}

	public readonly bool CanPreview
	{
		get
		{
			switch ( ResultType )
			{
				case ResultType.Bool:
					return false;
				case ResultType.Int:
					return true;
				case ResultType.Float:
					return true;
				case ResultType.Vector2:
					return true;
				case ResultType.Vector3:
					return true;
				case ResultType.Vector4:
					return true;
				case ResultType.Float2x2:
					return false;
				case ResultType.Float3x3:
					return false;
				case ResultType.Float4x4:
					return false;
				case ResultType.Sampler:
					return false;
				case ResultType.Texture2D:
					return false;
				case ResultType.TextureCube:
					return false;
				case ResultType.Gradient:
					return false;
				case ResultType.VoidFunction:
					return false;
				case ResultType.Invalid:
					throw new Exception( "Result Type Is Invalid!" );
				default:
					return false;
			}
		}
	}

	public readonly bool CanCast => ResultType switch
	{
		ResultType.Int => true,
		ResultType.Float => true,
		ResultType.Vector2 => true,
		ResultType.Vector3 => true,
		ResultType.Vector4 => true,
		_ => false,
	};

	public readonly string TypeName => ResultType switch
	{
		ResultType.Bool => "bool",
		ResultType.Int => "int",
		ResultType.Float => "float",
		ResultType.Vector2 => "float2",
		ResultType.Vector3 => "float3",
		ResultType.Vector4 => "float4",
		ResultType.Float2x2 => "float2x2",
		ResultType.Float3x3 => "float3x3",
		ResultType.Float4x4 => "float4x4",
		ResultType.Sampler => "SamplerState",
		ResultType.Texture2D => "Texture2D",
		ResultType.TextureCube => "TextureCube",
		ResultType.Gradient => "Gradient",
		_ => throw new Exception( $"Unsupported ResultType `{ResultType}`" )
	};

	public bool Constant { get; set; }

	public bool ShouldPreview { get; set; }

	public bool IsMetaDataResult { get; set; } = false;

	/// <summary>
	/// Generic-Ish metadata related to this NodeResult.
	/// </summary>
	internal Dictionary<string, object> Metadata { get; private set; }

	public NodeResult( ResultType resultType, string code, bool constant = false, Dictionary<string, object> metadata = null )
	{
		ResultType = resultType;
		Code = code;
		Constant = constant;
		IsMetaDataResult = false;

		if ( metadata == null )
		{
			Metadata = new();
		}
		else
		{
			Metadata = metadata;
		}

		Components = ResultType switch
		{
			ResultType.Bool => 1,
			ResultType.Int => 1,
			ResultType.Float => 1,
			ResultType.Vector2 => 2,
			ResultType.Vector3 => 3,
			ResultType.Vector4 => 4,
			ResultType.VoidFunction => 0,
			_ => 0
		};
	}

	public static NodeResult Error( params string[] errors ) => new() { Errors = errors };
	public static NodeResult Warning( params string[] warnings ) => new() { Warnings = warnings };
	public static NodeResult MissingInput( string name ) => Error( $"Missing required input '{name}'." );
	public static NodeResult IncorrectInputType( string name, ResultType correctResultType ) => Error( $"Input '{name}' must be of ResultType '{correctResultType}'" );

	public void SetPreviewID( int previewid ) { PreviewID = previewid; }
	public void SetVoidLocalTargetID( string voidLocalTargetID ) { VoidLocalTargetID = voidLocalTargetID; }

	#region Metdata
	internal T GetMetadata<T>( string metaName, bool ignoreException = false )
	{
		if ( Metadata.TryGetValue( metaName, out var actualData ) )
		{
			if ( typeof( T ) == actualData.GetType() )
			{
				return (T)actualData;
			}
			else
			{
				throw new InvalidCastException( $"Generic type of `{typeof( T )}` is not of metadata actual data type `{actualData.GetType()}`" );
			}
		}

		if ( !ignoreException )
		{
			throw new Exception( $"Unable to get metadata with name `{metaName}`" );
		}

		return default( T );
	}

	internal bool TryGetMetaData<T>( string metaName, out T data )
	{
		data = default;

		if ( Metadata.TryGetValue( metaName, out var actualData ) )
		{
			if ( typeof( T ) == actualData.GetType() )
			{
				data = (T)actualData;
				return true;
			}
			else
			{
				return false;
				//throw new InvalidCastException( $"Generic type of `{typeof( T )}` is not of metadata actual data type `{actualData.GetType()}`" );
			}
		}

		return false;
	}

	internal void AddMetadataEntry( string metaName, object actualData )
	{
		if ( !Metadata.ContainsKey( metaName ) )
		{
			Metadata.Add( metaName, actualData );
		}
		else
		{
			throw new Exception( "Metadata entry already exists!" );
		}
	}
	#endregion Metdata

	/// <summary>
	/// "Cast" this result to different float types
	/// </summary>
	public string Cast( int components, float defaultValue = 0.0f )
	{
		if ( components > 4 )
		{
			throw new Exception( $"There is no float type with a component count of `{components}`" );
		}

		if ( !CanCast )
		{
			throw new Exception( $"ResultType `{ResultType}` cannot be cast." );
		}

		if ( ResultType == ResultType.Int )
		{
			if ( Components == components )
			{
				return $"{Code}";
			}

			return $"float{components}( {string.Join( ", ", Enumerable.Repeat( Code, components ) )} )";
		}

		if ( Components == components )
		{
			return Code;
		}

		if ( Components > components )
		{
			return $"{Code}.{"xyzw"[..components]}";
		}
		else if ( Components == 1 )
		{
			return $"float{components}( {string.Join( ", ", Enumerable.Repeat( Code, components ) )} )";
		}
		else
		{
			if ( !string.IsNullOrWhiteSpace( Code ) )
				return $"float{components}( {Code}, {string.Join( ", ", Enumerable.Repeat( $"{defaultValue}", components - Components ) )} )";

			return $"float{components}( {string.Join( ", ", Enumerable.Repeat( $"{defaultValue}", components ) )} )";
		}
	}

	public readonly bool IsFloatTypeResult()
	{
		return ResultType switch
		{
			ResultType.Float => true,
			ResultType.Vector2 => true,
			ResultType.Vector3 => true,
			ResultType.Vector4 => true,
			_ => false,
		};
	}

	public readonly bool IsMatrixResult()
	{
		return ResultType switch
		{
			ResultType.Float2x2 => true,
			ResultType.Float3x3 => true,
			ResultType.Float4x4 => true,
			_ => false,
		};
	}

	public readonly bool IsTextureResult()
	{
		return ResultType switch
		{
			ResultType.Texture2D => true,
			ResultType.TextureCube => true,
			_ => false,
		};
	}

	public override readonly string ToString()
	{
		return Code;
	}
}
