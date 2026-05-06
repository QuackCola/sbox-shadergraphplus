
namespace ShaderGraphPlus;

public sealed partial class GraphCompiler
{
	private partial class CompileResult
	{
		public Dictionary<string, VoidFunctionInfo> VoidFunctionLocals { get; private set; } = new();
	}
}
