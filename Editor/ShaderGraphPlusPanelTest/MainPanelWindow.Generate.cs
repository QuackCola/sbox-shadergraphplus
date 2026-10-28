using Editor;
using ShaderGraphPlus;
using ShaderGraphPlus.Nodes;
using System;
using System.Collections.Generic;
using System.Text;
using static ShaderGraphPlus.GraphCompiler;

namespace ShaderGraphPlusPanelTest;

public sealed partial class ShaderGraphPlusPanelTest
{
	private Dictionary<string, ShaderFeatureBase> _registeredShaderFeatures = new();
	private readonly List<string> EditorErrors = new();
	private readonly List<string> EditorWarnings = new();

	/// <summary>
	/// Error list.
	/// </summary>
	public IEnumerable<GraphIssue> Errors => EditorErrors
		.Select( x => new GraphIssue { Node = null, Message = x, IsWarning = false } );

	/// <summary>
	/// Warning list.
	/// </summary>
	public IEnumerable<GraphIssue> Warnings => EditorWarnings
		.Select( x => new GraphIssue { Node = null, Message = x, IsWarning = true } );

	private void RegisterShaderFeatures( out List<string> registrationIssues )
	{
		_registeredShaderFeatures.Clear();
		registrationIssues = new();

		var features = _graph.Parameters.OfType<IBlackboardShaderFeatureParameter>();

		foreach ( var feature in features )
		{
			ShaderFeatureBase shaderFeature = default;

			if ( feature is ShaderFeatureBooleanParameter boolFeatureParam )
			{
				shaderFeature = new ShaderFeatureBoolean()
				{
					Name = boolFeatureParam.Name,
					HeaderName = boolFeatureParam.HeaderName,
					Description = boolFeatureParam.Description,
				};
			}
			else if ( feature is ShaderFeatureEnumParameter enumFeatureParam )
			{
				shaderFeature = new ShaderFeatureEnum()
				{
					Name = enumFeatureParam.Name,
					HeaderName = enumFeatureParam.HeaderName,
					Description = enumFeatureParam.Description,
					Options = enumFeatureParam.Options,
				};
			}

			if ( shaderFeature.IsValid && !_registeredShaderFeatures.ContainsKey( shaderFeature.Name ) )
			{
				_registeredShaderFeatures.Add( shaderFeature.Name, shaderFeature );
			}
		}
	}

	private void OnError( IEnumerable<GraphCompiler.GraphIssue> issues )
	{
		_output.Issues = issues.ToList();
		DockHost.Activate( "output" );

		_generatedCode = null;
		_generatedCodeView.Code = "";

		RestoreShader();
	}

	private void OnWarning( IEnumerable<GraphCompiler.GraphIssue> issues )
	{
		_output.Issues = issues.ToList();
		DockHost.Activate( "output" );
	}

	private string GeneratePreviewCode()
	{
		EditorWarnings.Clear();
		EditorErrors.Clear();

		// SkyBox2D.SkyMaterial requires that the shader name contains the substring 'sky' in the name.
		if ( _graph.Domain == ShaderDomain.Sky && (_asset == null || !_asset.Name.Contains( "sky" )) )
		{
			EditorErrors.Add( "The graph name must contain the string 'sky'" );
		}

		if ( _graph.HasTemplate )
		{
			if ( !_graph.ShaderTypeInfo.IsValid )
			{
				// Template shat the bed when trying to load.
				EditorErrors.Add( $"Shader Template \"{_graph.ShaderType}\" failed to load. Template may be incompatable with the current version and will need to be upgraded." );
			}
			else if ( !_shaderTemplate.Validate( _graph.ShaderType, out var tagErrors ) )
			{
				// Template loaded but there are issues with the template code.
				EditorErrors.AddRange( tagErrors );
			}
		}

		foreach ( var parameter in _graph.Parameters )
		{
			if ( !parameter.CheckParameter( out var parameterErrors ) )
			{
				EditorErrors.AddRange( parameterErrors );
			}
		}

		// Go ahead preregister anything before iterating over all the nodes in the graph.
		RegisterShaderFeatures( out List<string> featureErrors );

		if ( featureErrors.Any() )
		{
			EditorErrors.AddRange( featureErrors );
		}

		// Dont bother trying to generate the preview code if there are any errors.
		if ( EditorErrors.Any() )
		{
			OnError( Errors );
			return null;
		}

		if ( _autoCompile )
		{
			ClearAttributes();
		}

		var resultNode = _graph.Nodes.OfType<BaseResult>().FirstOrDefault();
		var compiler = new GraphCompiler( _graph, _shaderTemplate, _registeredShaderFeatures, true );
		var nodeErrors = new List<GraphCompiler.GraphIssue>();
		var nodeWarnings = new List<GraphCompiler.GraphIssue>();
		var evaluatedCustomFunctions = new List<string>();

		if ( _autoCompile )
		{
			compiler.OnAttribute = OnAttribute;
		}

		foreach ( var node in _graph.Nodes.OfType<BaseNodePlus>() )
		{
			node.ClearError();

			// Assign a PreviewID to any Previewable node.
			if ( node.CanPreview )
			{
				node.PreviewID = compiler.PreviewID++;
			}

			if ( node is IWarningNode warningNode )
			{
				node.HasWarning = false;

				var warnings = warningNode.GetWarnings();
				if ( warnings.Count > 0 )
				{
					node.HasWarning = true;
					foreach ( var warning in warnings )
					{
						nodeWarnings.Add( new() { Message = warning, Node = node, IsWarning = true } );
					}
				}
			}

			if ( node is IErroringNode erroringNode )
			{
				node.ClearError();

				var errors = erroringNode.GetErrors();
				if ( errors.Count > 0 )
				{
					node.HasError = true;

					var errorSb = new StringBuilder();
					foreach ( var error in errors )
					{
						nodeErrors.Add( new() { Message = error, Node = node, IsWarning = false } );

						errorSb.AppendLine( error );
					}

					node.ErrorMessage = errorSb.ToString();
				}
			}

			if ( node is PreviewableNode previewableNode )
			{
				previewableNode.EvaluatePreview( compiler );
			}

			if ( node is CustomFunctionNode customFunctionNode )
			{
				if ( !string.IsNullOrWhiteSpace( customFunctionNode.Name ) )
				{
					node.ClearError();

					if ( evaluatedCustomFunctions.Contains( customFunctionNode.Name ) )
					{
						node.HasError = true;
						node.ErrorMessage = "Duplicate Custom Function node";
						nodeErrors.Add( new() { Message = node.ErrorMessage, Node = node, IsWarning = false } );
					}
					else
					{
						evaluatedCustomFunctions.Add( customFunctionNode.Name );
					}
				}
			}

			var property = node.GetType().GetProperties( BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static )
				.FirstOrDefault( x => x.GetGetMethod() != null && x.PropertyType == typeof( NodeResult.Func ) );

			if ( property == null )
				continue;

			var output = property.GetCustomAttribute<BaseNodePlus.OutputAttribute>();
			if ( output == null )
				continue;

			var result = compiler.Result( new NodeInput { Identifier = node.Identifier, Output = property.Name } );
			if ( !result.IsValid() )
				continue;

			Type componentType = null;
			if ( result.ResultType == ResultType.VoidFunction )
			{
				componentType = result.Components switch
				{
					int r when r == 1 => typeof( float ),
					int r when r == 2 => typeof( Vector2 ),
					int r when r == 3 => typeof( Vector3 ),
					int r when r == 4 => typeof( Vector4 ),
					_ => throw new NotImplementedException(),
				};
			}
			else
			{
				componentType = result.ResultType switch
				{
					ResultType.Bool => typeof( bool ),
					ResultType.Int => typeof( int ),
					ResultType.Float => typeof( float ),
					ResultType.Vector2 => typeof( Vector2 ),
					ResultType.Vector3 => typeof( Vector3 ),
					ResultType.Vector4 => typeof( Vector4 ),
					ResultType.Float2x2 => typeof( Float2x2 ),
					ResultType.Float3x3 => typeof( Float3x3 ),
					ResultType.Float4x4 => typeof( Float4x4 ),
					ResultType.Sampler => typeof( Sampler ),
					ResultType.Texture2D => typeof( Texture ),
					ResultType.TextureCube => typeof( Texture ),
					ResultType.Gradient => typeof( Gradient ),
					_ => throw new Exception( $"Unsupported ResultType \"{result.ResultType}\"" ),
				};
			}

			if ( componentType == null )
				continue;

			// While we're here, let's check the output plugs and update their handle configs to the result type
			var nodeUI = _graphView.FindNode( node );
			if ( !nodeUI.IsValid() )
				continue;

			var plugOut = nodeUI.Outputs.FirstOrDefault( x => ((BasePlug)x.Inner).Info.Property == property );
			if ( !plugOut.IsValid() )
				continue;

			plugOut.PropertyType = componentType;
		}

		if ( _properties.IsValid() && _properties.Target is BaseNodePlus targetNode && targetNode.CanPreview )
		{
			_preview.SetStage( targetNode.PreviewID );
		}
		else
		{
			_preview.SetStage( ShaderGraphPlusGlobals.GraphCompiler.NoNodePreviewID );
		}

		var code = compiler.Generate();

		nodeWarnings.AddRange( compiler.Warnings );
		nodeErrors.AddRange( compiler.Errors );

		if ( nodeWarnings.Any() )
		{
			if ( nodeErrors.Any() )
			{
				nodeWarnings.AddRange( nodeErrors );
				OnError( nodeWarnings );

				return null;
			}
			else
			{
				OnWarning( nodeWarnings );
			}
		}
		else
		{
			_output.ClearWarnings();
		}

		if ( nodeErrors.Any() )
		{
			OnError( nodeErrors );

			return null;
		}
		else
		{
			_output.ClearErrors();
		}

		if ( _generatedCode != code )
		{
			_generatedCode = code;

			if ( _generatedCodeView != null )
			{
				_generatedCodeView.Code = _generatedCode;
			}

			if ( _autoCompile )
			{
				Compile();
			}
		}

		return code;
	}

	private string GenerateShaderCode()
	{
		// Go ahead preregister anything before iterating over all the nodes in the graph.
		RegisterShaderFeatures( out _ );

		var compiler = new GraphCompiler( _graph, _shaderTemplate, _registeredShaderFeatures, false );
		return compiler.Generate();
	}
}
