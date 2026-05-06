namespace ShaderGraphPlus;

internal struct VoidFunctionInfo : IValid
{
	public List<VoidFunctionResult> TargetResults { get; private set; }

	public string FunctionCall { get; private set; }

	/// <summary>
	/// The Identifier of the node that this data is bound to.
	/// </summary>
	public string NodeIdentifier { get; private set; }

	/// <summary>
	/// Is this void data ment for a void function thats included from a hlsl file?
	/// </summary>
	public bool IsIncludedFunction { get; private set; }

	public bool IsAlreadyPostProcessed { get; set; }

	[Hide]
	public readonly bool IsValid => TargetResults != null && TargetResults.Any();

	/// <summary>
	/// 
	/// </summary>
	/// <param name="targetResults"></param>
	/// <param name="functionCall"></param>
	/// <param name="nodeIdentifier"></param>
	/// <param name="isIncludedFunction"></param>
	public VoidFunctionInfo( List<VoidFunctionResult> targetResults, string functionCall, string nodeIdentifier, bool isIncludedFunction )
	{
		TargetResults = targetResults;
		FunctionCall = functionCall;
		NodeIdentifier = nodeIdentifier;
		IsIncludedFunction = isIncludedFunction;
	}

	public readonly ResultType GetResultResultType( string compilerAssignedName )
	{
		var result = TargetResults.Where( x => x.CompilerAssignedName == compilerAssignedName ).FirstOrDefault();

		if ( result.IsValid )
			return result.ResultType;

		throw new Exception( $"Key `{compilerAssignedName}` does not exist within `{nameof( VoidFunctionInfo.TargetResults )}`" );
	}

	public readonly string GetCompilerAssignedName( string userAssignedName )
	{
		var result = TargetResults.Where( x => x.UserAssignedName == userAssignedName ).FirstOrDefault();

		if ( result.IsValid )
			return result.CompilerAssignedName;

		throw new Exception( "Shits fucked..." );
	}

	public override readonly int GetHashCode()
	{
		var hashCodeTargetResults = 0;
		foreach ( var item in TargetResults )
		{
			hashCodeTargetResults += item.GetHashCode();
		}

		return HashCode.Combine( FunctionCall, hashCodeTargetResults );
	}
}
