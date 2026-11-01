using Editor;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;

public sealed partial class PreviewPanel
{
	/// <summary>
	/// If true we'll render shadows
	/// </summary>
	public bool EnableShadows
	{
		get;
		set
		{
			field = value;

			if ( _modelRenderer.IsValid() )
			{
				_modelRenderer.RenderType = value ? ModelRenderer.ShadowRenderType.On : ModelRenderer.ShadowRenderType.Off;
			}
		}
	} = true;

	/// <summary>
	/// If true we'll show the shybox.
	/// </summary>
	public bool ShowSkybox
	{
		get => _sky.IsValid() && _sky.Enabled;
		set
		{
			if ( !_sky.IsValid() )
				return;

			_sky.Enabled = value;

			//if ( _skyButton == null )
			//	return;
		}
	}

	/// <summary>
	/// Color of the background when the skybox is not being drawn.
	/// </summary>
	public Color BackgroundColor
	{
		get => _previewScene.Camera.IsValid() ? _previewScene.Camera.BackgroundColor : default;
		set
		{
			if ( !_previewScene.Camera.IsValid() )
				return;

			_previewScene.Camera.BackgroundColor = value;
		}
	}

	/// <summary>
	/// Color tint of the model in the preview.
	/// </summary>
	public Color Tint
	{
		get;
		set
		{
			field = value;

			if ( _modelRenderer.IsValid() )
			{
				_modelRenderer.Tint = field;
			}
		}
	} = Color.White;


	public void OpenSettings()
	{
		_settingsMenu?.Delete( true );

		_settingsMenu = new Sandbox.UI.Menu();

		_gridButton = _settingsMenu.AddOption( "Show grid", "grid_on", () =>
		{
			ShowGrid = !ShowGrid;
		} );

		_skyButton = _settingsMenu.AddOption( "Show Sky", "nights_stay", () =>
		{
			ShowSkybox = !ShowSkybox;
		} );

		_settingsMenu.Closed += ( _ ) => CloseMenu( ref _settingsMenu );
		_settingsMenu.Open( _settingsButton, Popup.PositionMode.UnderMouse );
	}


	public void LoadSettings( PreviewSettings settings )
	{
		SGPLogger.Info( $"TODO : PreviewPanel.LoadSettings()" );
	}
}
