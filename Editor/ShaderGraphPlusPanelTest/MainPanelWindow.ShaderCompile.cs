using Editor;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShaderGraphPlusPanelTest;

public sealed partial class ShaderGraphPlusPanelTest
{
	private bool _isCompiling = false;
	private bool _isPendingCompile = false;
	private RealTimeSince _timeSinceCompile;

	private readonly List<string> _shaderCompileErrors = new();

	private struct StatusMessage
	{
		public string Status { get; set; }
		public string Message { get; set; }
	}

	private void Compile()
	{
		if ( string.IsNullOrWhiteSpace( _generatedCode ) )
		{
			RestoreShader();

			return;
		}

		if ( _isCompiling )
		{
			_isPendingCompile = true;

			return;
		}

		var assetPath = $"shadergraphplus/{_asset?.Name ?? "untitled"}_shadergraphplus.generated.shader";
		var resourcePath = System.IO.Path.Combine( ".source2/temp", assetPath );

		Editor.FileSystem.Root.CreateDirectory( ".source2/temp/shadergraphplus" );
		Editor.FileSystem.Root.WriteAllText( resourcePath, _generatedCode );

		_isCompiling = true;
		_preview.IsCompiling = _isCompiling;

		RestoreShader();

		_timeSinceCompile = 0;

		CompileAsync( resourcePath );
	}

	private async void CompileAsync( string path )
	{
		var options = new Sandbox.Engine.Shaders.ShaderCompileOptions
		{
			ConsoleOutput = true
		};

		var result = await EditorUtility.CompileShader( Editor.FileSystem.Root, path, options );

		if ( result.Success )
		{
			var asset = AssetSystem.RegisterFile( Editor.FileSystem.Root.GetFullPath( path ) );

			while ( !asset.IsCompiledAndUpToDate )
			{
				await Task.Yield();
			}
		}

		MainThread.Queue( () => OnCompileFinished( result.Success ? 0 : 1 ) );
	}

	internal void OnOutputData( object sender, DataReceivedEventArgs e )
	{
		var str = e.Data;
		if ( str == null )
			return;

		MainThread.Queue( () =>
		{
			var trimmed = str.Trim();
			if ( trimmed.StartsWith( '{' ) && trimmed.EndsWith( '}' ) )
			{
				var status = Json.Deserialize<StatusMessage>( trimmed );
				if ( status.Status == "Error" )
				{
					if ( !string.IsNullOrWhiteSpace( status.Message ) )
					{
						var lines = status.Message.Split( '\n' );
						foreach ( var line in lines.Where( x => !string.IsNullOrWhiteSpace( x ) ) )
						{
							_shaderCompileErrors.Add( line );
						}
					}
				}
			}
		} );
	}

	internal void OnErrorData( object sender, DataReceivedEventArgs e )
	{
		if ( e == null || e.Data == null )
			return;

		MainThread.Queue( () =>
		{
			var error = $"{e.Data}";
			if ( !string.IsNullOrWhiteSpace( error ) )
				_shaderCompileErrors.Add( error );
		} );
	}

	private void OnCompileFinished( int exitCode )
	{
		_isCompiling = false;

		if ( _isPendingCompile )
		{
			_isPendingCompile = false;

			Compile();

			return;
		}

		if ( exitCode == 0 && _shaderCompileErrors.Count == 0 )
		{
			SGPLogger.Info( $"Compile finished in {_timeSinceCompile}" );

			var shaderPath = $"shadergraphplus/{_asset?.Name ?? "untitled"}_shadergraphplus.generated.shader";

			// Reload the shader otherwise it's gonna be the old wank
			// Alternatively Material.Create could be made to force reload the shader
			Editor.ConsoleSystem.Run( $"mat_reloadshaders {shaderPath}" );

			var material = Material.Create( $"{_asset?.Name ?? "untitled"}_shadergraphplus_generated", shaderPath );

			_preview.Material = material;
		}
		else
		{
			SGPLogger.Error( $"Compile failed in {_timeSinceCompile}" );

			// TODO
			_output.Issues = _shaderCompileErrors.Select( x => new ShaderGraphPlus.GraphCompiler.GraphIssue { Message = x } ).ToList();

			DockHost.Activate( "output" );

			RestoreShader();
			ClearAttributes();
		}

		_preview.IsCompiling = _isCompiling;
		_preview.EnablePostProcessing = _graph.Domain == ShaderGraphPlus.ShaderDomain.PostProcess;

		_shaderCompileErrors.Clear();
	}
}
