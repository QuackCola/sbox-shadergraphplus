
namespace ShaderGraphPlus;

public sealed partial class GraphCompiler
{
	private partial class CompileResult
	{
		public Dictionary<string, VoidFunctionInfo> VoidFunctionData { get; private set; } = new();
	}
}
