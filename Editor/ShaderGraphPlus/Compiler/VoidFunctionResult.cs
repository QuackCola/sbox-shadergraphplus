namespace ShaderGraphPlus;

internal struct VoidFunctionResult : IValid
{
	public string UserAssignedName { get; private set; }
	public string CompilerAssignedName { get; private set; }
	public ResultType ResultType { get; private set; }

	public readonly bool IsValid => !string.IsNullOrWhiteSpace( UserAssignedName ) || ResultType != ResultType.Invalid;

	public VoidFunctionResult( string userAssignedName, string compilerAssignedName, ResultType resultType )
	{
		UserAssignedName = userAssignedName;
		CompilerAssignedName = compilerAssignedName;
		ResultType = resultType;
	}

	public override readonly int GetHashCode()
	{
		return HashCode.Combine( UserAssignedName, CompilerAssignedName, ResultType );
	}
}
