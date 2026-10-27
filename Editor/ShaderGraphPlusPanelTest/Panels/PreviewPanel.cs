using Editor;
using EditorExtras;
using Sandbox.Rendering;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;
using Label = Sandbox.UI.Label;

#pragma warning disable CS0618 // ScenePanel.Camera is obsolete, but it's the path that lets us add the gizmo world and drive the camera ourselves

namespace ShaderGraphPlusPanelTest;

internal sealed class ThrobberPanel : Panel
{
	private readonly Texture _texture;

	public bool Enabled
	{
		get;
		set;
	}

	public ThrobberPanel() : base()
	{
		_texture = Texture.Load( "tools/images/common/busy.png", true );
	}

	public override void OnDraw( Painter painter )
	{
		base.OnDraw( painter );

		if ( !Enabled ) return;

		var size = 48;
		var position = new Vector2( painter.Bounds.Height, painter.Bounds.Height - size );
		var rect = new Rect( position.x, position.y, size, size );

		painter.Texture( _texture, rect );
	}
}

[StyleSheet.Inline( "sgp_previewpanel", Styles )]
public sealed partial class PreviewPanel : Panel
{
	private const string BoxModel = "models/dev/box.vmdl";
	private const string PlaneModel = "models/dev/plane.vmdl";

	/// <summary>
	/// What the preview model is. The built-ins light up their toolbar button; anything picked from
	/// the asset browser is <see cref="Custom"/>.
	/// </summary>
	enum Primitive
	{
		Box,
		Sphere,
		Plane,
		Custom
	}

	private static readonly float[] TimeScales = [0.25f, 0.5f, 1.0f, 2.0f, 4.0f];

	// Fly keys being held, by normalised button name ("w", "lshift")
	private readonly HashSet<string> _held = new();

	private readonly Scene _previewScene;
	private readonly ScenePanel _scenePanel;
	private readonly Gizmo.Instance _gizmo = new();
	private readonly GameObject _modelObject;
	private readonly ModelRenderer _modelRenderer;
	private readonly GameObject _skyObject;
	private readonly SkyBox2D _sky;
	private readonly GameObject _lightObject;
	readonly GameObject _cameraObject;
	readonly CameraComponent _camera;

	private readonly Dictionary<Primitive, Panel> _modelButtons = new();
	private readonly Label _playIcon;
	private readonly Label _speedText;
	private readonly Panel _playButton;
	private readonly Panel _speedButton;
	private readonly Panel _lightButton;
	private readonly Panel _settingsButton;

	private Panel _gridButton;
	private Panel _skyButton;
	private Sandbox.UI.Menu _speedMenu;
	private Sandbox.UI.Menu _settingsMenu;

	private ThrobberPanel _throbber;

	private int _stageId;
	private float _previewTime;
	private readonly Material _defaultSkyMaterial;

	private Model _sphere;
	private Primitive _primitive;

	private Vector3 _target;
	private Angles _angles = new( 25, 135, 0 );
	private float _distance = 150;

	private MouseButtons? _dragging;

	/// <summary>
	/// How far the fly keys move the camera per second,
	/// as a share of its distance to the orbit point.
	/// </summary>
	private const float FlySpeed = 0.5f;

	/// <summary>
	/// ow much faster flying is with Shift held.
	/// </summary>
	private const float FlyBoost = 4.0f;

	public bool EnablePostProcessing
	{
		get;
		set
		{
			if ( field == value )
				return;

			field = value;

			UpdateMaterial();
			UpdatePostProcessing();
		}
	}

	public bool EnableNodePreview
	{
		get;
		set
		{
			field = value;

			if ( _modelRenderer.IsValid() )
			{
				_modelRenderer.Attributes.Set( ShaderGraphPlusGlobals.PreviewShaderAttributes.StageID, field ? _stageId : ShaderGraphPlusGlobals.GraphCompiler.NoNodePreviewID );
			}
		}
	}

	public bool ShowGrid
	{
		get;
		set;
	}

	/// <summary>
	/// Light the model with the preview's sun. Off leaves only the ambient light, to check emission
	/// or how a material reads unlit.
	/// </summary>
	public bool ShowLight
	{
		get => _lightObject.Enabled;
		set
		{
			_lightObject.Enabled = value;

			if ( _lightButton == null )
				return;

			_lightButton.SetClass( "on", value );
		}
	}

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

	private Model _model;
	/// <summary>
	/// The model being previewed. Null goes back to the default box, where the Qt preview goes back
	/// to its sphere.
	/// </summary>
	public Model Model
	{
		get => _model;
		set
		{
			if ( value is null )
			{
				ShowPrimitive( Primitive.Box );
			}
			else
			{
				SetModel( value, Primitive.Custom );
			}

			UpdateMaterial();
			UpdatePostProcessing();
			SetAllAttributes();

			//UpdateAnimationCombo(); // TODO

			OnModelChanged?.Invoke( value );
		}
	}

	public Material Material
	{
		get => (field) ?? ShaderGraphPlusGlobals.Preview.DefaultMaterial;
		set
		{
			field = value;

			UpdateMaterial();
			UpdatePostProcessing();
			SetAllAttributes();
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

	private CommandList _postProcessCmdList;
	internal void UpdatePostProcessing()
	{
		if ( _previewScene.Camera is null )
		{
			return;
		}

		if ( !EnablePostProcessing )
		{
			if ( _postProcessCmdList is not null )
			{
				_previewScene?.Camera?.RemoveCommandList( _postProcessCmdList );
				_postProcessCmdList = null;
			}
			return;
		}

		if ( _postProcessCmdList is null )
		{
			_postProcessCmdList = new CommandList( "Preview PostProcess" );
			_previewScene.Camera.AddCommandList( _postProcessCmdList, Stage.AfterPostProcess, 1000 );
		}

		_postProcessCmdList.Reset();
		_postProcessCmdList.Attributes.GrabFrameTexture( "ColorBuffer" );
		_postProcessCmdList.Blit( Material, _modelRenderer.Attributes );
	}

	/// <summary>
	/// Freeze preview time.
	/// </summary>
	public bool Paused
	{
		get;
		set
		{
			field = value;

			_playIcon.Text = value ? "play_arrow" : "pause";
			_playButton.Tooltip = value ? "Resume shader time" : "Pause shader time";
			_playButton.SetClass( "on", value );
		}
	}

	/// <summary>How fast shader preview time runs, as a multiple of real time.</summary>
	public float TimeScale
	{
		get;
		set
		{
			field = value;
			_speedText.Text = $"{value:0.##}x";
		}
	} = 1.0f;

	public bool IsCompiling
	{
		set
		{
			_throbber.Enabled = value;
		}
	}

	public Action<Model> OnModelChanged { get; set; }

	public PreviewPanel() : base()
	{
		AddClass( "sg-preview" );

		// Clicking the view gives it the keyboard, for the fly keys
		AcceptsFocus = true;

		// Ours rather than the panel's own, so the gizmo grid and camera share a world we control.
		// The panel never owns it, so OnDeleted destroys it.
		_previewScene = new Scene { WantsSystemScene = false };

		using ( _previewScene.Push() )
		{
			// Lit renderers need a light to be seen
			_lightObject = _previewScene.CreateObject();
			_lightObject.Name = "Preview Light";
			_lightObject.WorldRotation = Rotation.From( 50, 30, 0 );
			_lightObject.Components.Create<DirectionalLight>().LightColor = Color.White * 1.5f;

			_modelObject = _previewScene.CreateObject();
			_modelObject.Name = "Preview Model";
			_modelRenderer = _modelObject.Components.Create<ModelRenderer>();

			_skyObject = _previewScene.CreateObject();
			_skyObject.Name = "Preview Sky";
			_sky = _skyObject.Components.Create<SkyBox2D>();

			_defaultSkyMaterial = _sky.SkyMaterial;

			_cameraObject = _previewScene.CreateObject();
			_cameraObject.Name = "Preview Camera";
			_camera = _cameraObject.Components.Create<CameraComponent>();
			_camera.BackgroundColor = BackgroundColor;
			_camera.ZFar = 10000;

			var envProbeObject = _previewScene.CreateObject();
			envProbeObject.Name = "Preview Envmap";
			var envProbe = envProbeObject.Components.Create<EnvmapProbe>();
			envProbe.Texture = Texture.Load( "textures/cubemaps/default2.vtex" );
		}

		_scenePanel = AddChild<ScenePanel>();
		_scenePanel.AddClass( "scene" );
		_scenePanel.RenderScene = _previewScene;
		//_scenePanel.Camera.World = _previewScene.SceneWorld;
		//_scenePanel.Camera.BackgroundColor = new Color( 0.07f, 0.075f, 0.09f );
		//_scenePanel.Camera.AmbientLightColor = new Color( 0.18f, 0.19f, 0.23f );
		//_scenePanel.Camera.ZFar = 10000;
		_scenePanel.Camera.Worlds.Add( _gizmo.World );

		var overlayToolbar = Add.Panel( "overlay" );

		AddModelButton( overlayToolbar, Primitive.Box, "view_in_ar", "Box" );
		AddModelButton( overlayToolbar, Primitive.Sphere, "circle", "Sphere" );
		AddModelButton( overlayToolbar, Primitive.Plane, "square", "Plane" );
		AddIconButton( overlayToolbar, "folder_open", "Pick a model...", PickModel );

		overlayToolbar.Add.Panel( "separator" );

		_playButton = AddIconButton( overlayToolbar, "pause", "Pause shader time", () => Paused = !Paused );
		_playIcon = _playButton.GetChild( 0 ) as Label;

		_speedButton = overlayToolbar.Add.Panel( "iconbutton speed" );
		_speedButton.Tooltip = "Shader time speed";
		_speedButton.AddEventListener( "onmousedown", e => e.StopPropagation() );
		_speedButton.AddEventListener( "onclick", OpenSpeedMenu );
		_speedText = _speedButton.Add.Label( "1x", "speed-text" );

		overlayToolbar.Add.Panel( "spacer" );

		_lightButton = AddIconButton( overlayToolbar, "lightbulb", "Preview light", () => ShowLight = !ShowLight );
		_settingsButton = AddIconButton( overlayToolbar, "settings", "Preview Settings", OpenSettings );

		var overlayFooter = Add.Panel( "footer" );
		overlayFooter.Add.Label( "LMB orbit · RMB look · MMB pan · WASD/QE fly · Shift fast · wheel zoom", "hint" );

		_throbber = AddChild( new ThrobberPanel() );

		// The box is the default
		ShowPrimitive( Primitive.Box );

		Paused = false;
		TimeScale = 1.0f;
		ShowGrid = true;
		ShowLight = true;
	}

	private void CreateToolBar()
	{
	}

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

	private void OpenSpeedMenu()
	{
		_speedMenu?.Delete( true );

		_speedMenu = new Sandbox.UI.Menu();

		foreach ( var scale in TimeScales )
		{
			var value = scale;
			_speedMenu.AddOption( $"{value:0.##}x", value == TimeScale ? "check" : null, () => TimeScale = value );
		}

		_speedMenu.Closed += ( _ ) => CloseMenu( ref _speedMenu );
		_speedMenu.Open( _speedButton, Popup.PositionMode.UnderMouse );
	}

	private static void CloseMenu( ref Sandbox.UI.Menu menu )
	{
		menu.Delete( true );
		menu = null;
	}

	public void LoadSettings( PreviewSettings settings )
	{
		// The preview has no per-graph settings yet - nothing to apply.
	}

	public void UpdateMaterial()
	{
		var modelMaterial = Material;
		if ( EnablePostProcessing || modelMaterial.Flags.IsSky )
		{
			modelMaterial = Material.Load( "materials/dev/reflectivity_50.vmat" );
		}

		if ( Material.Flags.IsSky )
		{
			_sky.SkyMaterial = Material;
		}
		else if ( _sky.SkyMaterial != _defaultSkyMaterial )
		{
			_sky.SkyMaterial = _defaultSkyMaterial;
		}

		if ( _modelRenderer.IsValid() )
		{
			_modelRenderer.MaterialOverride = modelMaterial;
		}
	}

	private static Panel AddIconButton( Panel parent, string icon, string tooltip, Action onClick )
	{
		var button = parent.IconButton( icon, null );
		button.Tooltip = tooltip;
		button.AddEventListener( "onmousedown", e => e.StopPropagation() );
		button.AddEventListener( "onclick", () => onClick?.Invoke() );

		return button;
	}

	private void AddModelButton( Panel parent, Primitive primitive, string icon, string tooltip )
	{
		var button = AddIconButton( parent, icon, tooltip, () => ShowPrimitive( primitive ) );
		_modelButtons[primitive] = button;
	}

	/// <summary>
	/// The box and plane are the engine's dev models; the sphere is the Qt preview's own
	/// tessellated one, built the first time it's asked for.
	/// </summary>
	private void ShowPrimitive( Primitive primitive )
	{
		var model = primitive switch
		{
			Primitive.Sphere => _sphere ??= PreviewMeshes.CreateSphereModel(),
			Primitive.Plane => Model.Load( PlaneModel ),
			_ => Model.Load( BoxModel )
		};

		SetModel( model, primitive );
	}

	/// <summary>
	/// Put <paramref name="model"/> on the grid and frame it: the camera keeps its angles but
	/// orbits the model's middle from a distance that fits it.
	/// </summary>
	private void SetModel( Model model, Primitive primitive )
	{
		_model = model;
		_primitive = primitive;
		_modelRenderer.Model = model;

		var bounds = model.RenderBounds;
		_modelObject.WorldPosition = Vector3.Up * -bounds.Mins.z;

		_target = _modelObject.WorldPosition + bounds.Center;
		_distance = MathF.Max( bounds.Size.Length * 1.75f, 40.0f );

		foreach ( var (kind, button) in _modelButtons )
		{
			button.SetClass( "on", kind == primitive );
		}
	}

	/// <summary>
	/// The editor's asset browser, like the Qt preview's model button: whatever is highlighted
	/// shows straight away, and picking keeps it.
	/// </summary>
	private void PickModel()
	{
		var picker = AssetPicker.Create( null, AssetType.Model );
		picker.Title = "Select Preview Model";
		picker.OnAssetHighlighted = assets => UseAsset( assets.FirstOrDefault() );
		picker.OnAssetPicked = assets => UseAsset( assets.FirstOrDefault() );
		picker.Show();

		if ( _primitive == Primitive.Custom && !string.IsNullOrEmpty( _model?.ResourcePath ) )
		{
			picker.SetSelection( _model.ResourcePath );
		}
	}

	private void UseAsset( Asset asset )
	{
		if ( asset?.LoadResource<Model>() is { } model && _previewScene.IsValid() )
		{
			Model = model;
		}
	}

	public override void Tick()
	{
		if ( !_previewScene.IsValid() )
			return;

		UpdateTime();
		UpdateCamera();
		DrawGizmos();
	}

	private void UpdateCamera()
	{
		if ( _dragging is { } button )
		{
			var delta = Mouse.Delta;

			if ( button == MouseButtons.Middle )
			{
				var pan = _angles.ToRotation();
				var speed = _distance * 0.0015f;
				_target += pan.Left * delta.x * speed + pan.Up * delta.y * speed;
			}
			else if ( button == MouseButtons.Right )
			{
				// Look around from where the camera is: turn, then put the orbit point back in front of it
				var eye = _target - _angles.ToRotation().Forward * _distance;
				Turn( delta );
				_target = eye + _angles.ToRotation().Forward * _distance;
			}
			else
			{
				Turn( delta );
			}
		}

		Fly();

		var rotation = _angles.ToRotation();
		_cameraObject.WorldRotation = rotation;
		_cameraObject.WorldPosition = _target - rotation.Forward * _distance;
	}

	void Turn( Vector2 delta )
	{
		_angles.yaw -= delta.x * 0.25f;
		_angles.pitch = (_angles.pitch + delta.y * 0.25f).Clamp( -89, 89 );
	}

	/// <summary>
	/// WASD along the view, Q and E straight down and up. The camera and its orbit point move
	/// together, so orbiting afterwards still turns around what's in front of you. Speed follows
	/// the zoom - zoomed in close is fine movement, zoomed out covers ground.
	/// </summary>
	void Fly()
	{
		if ( _held.Count == 0 )
			return;

		var rotation = _angles.ToRotation();
		var direction = Vector3.Zero;

		if ( _held.Contains( "w" ) ) direction += rotation.Forward;
		if ( _held.Contains( "s" ) ) direction -= rotation.Forward;
		if ( _held.Contains( "d" ) ) direction += rotation.Right;
		if ( _held.Contains( "a" ) ) direction -= rotation.Right;
		if ( _held.Contains( "e" ) ) direction += Vector3.Up;
		if ( _held.Contains( "q" ) ) direction -= Vector3.Up;

		if ( direction.IsNearlyZero() )
			return;

		var speed = _distance * FlySpeed;
		if ( _held.Contains( "lshift" ) || _held.Contains( "rshift" ) )
		{
			speed *= FlyBoost;
		}

		_target += direction.Normal * speed * RealTime.Delta;
	}

	static readonly HashSet<string> FlyKeys = ["w", "a", "s", "d", "q", "e", "lshift", "rshift"];

	/// <summary>
	/// Key presses and releases reach the focused panel - the view, once it's been clicked.
	/// </summary>
	public override void OnButtonEvent( ButtonEvent e )
	{
		if ( !FlyKeys.Contains( e.Button ) )
		{
			base.OnButtonEvent( e );
			return;
		}

		if ( e.Pressed )
		{
			_held.Add( e.Button );
		}
		else
		{
			_held.Remove( e.Button );
		}
	}

	/// <summary>
	/// Releases are only sent to the focused panel - let go of everything rather than fly on forever.
	/// </summary>
	protected override void OnBlur( PanelEvent e )
	{
		_held.Clear();
		base.OnBlur( e );
	}

	/// <summary>
	/// The grid, drawn into our gizmo world. The gizmo is told where the camera and cursor are
	/// but never hovered while orbiting, so a camera drag can't grab anything.
	/// </summary>
	void DrawGizmos()
	{
		var cursor = _scenePanel.MousePosition;
		_gizmo.Input.Camera = _scenePanel.Camera;
		_gizmo.Input.CursorPosition = cursor;
		_gizmo.Input.CursorRay = _scenePanel.Camera.GetRay( cursor, _scenePanel.Box.Rect.Size );
		_gizmo.Input.IsHovered = _dragging is null && HasHovered;

		using ( _previewScene.Push() )
		using ( _gizmo.Push() )
		{
			if ( ShowGrid )
			{
				using ( Gizmo.Scope( "grid" ) )
				{
					Gizmo.Draw.Grid( Gizmo.GridAxis.XY, 16.0f, 0.35f );
				}
			}
		}
	}

	/// <summary>
	/// Any press orbits, looks or pans the camera. The camera captures the mouse: capture pins the
	/// cursor and reports movement through <see cref="Mouse.Delta"/>.
	/// </summary>
	protected override void OnMouseDown( MousePanelEvent e )
	{
		if ( _dragging is not null )
			return;

		_dragging = e.MouseButton;
		SetMouseCapture( true );

		e.StopPropagation();
	}

	protected override void OnMouseUp( MousePanelEvent e )
	{
		if ( _dragging != e.MouseButton )
			return;

		_dragging = null;
		SetMouseCapture( false );
	}

	public override void OnMouseWheel( Vector2 value )
	{
		_distance = (_distance * (1.0f + value.y * 0.1f)).Clamp( 10, 5000 );
	}

	public override void OnDeleted()
	{
		_speedMenu?.Delete( true );
		_settingsMenu?.Delete( true );
		_gizmo.Dispose();

		if ( _postProcessCmdList is not null )
		{
			_previewScene.Camera?.RemoveCommandList( _postProcessCmdList );
			_postProcessCmdList = null;
		}

		// The ScenePanel doesn't own the scene (we set it), so it's on us
		if ( _previewScene.IsValid() )
		{
			_previewScene.Destroy();
		}

		base.OnDeleted();
	}
}
