using Editor;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;
using Label = Sandbox.UI.Label;

#pragma warning disable CS0618 // ScenePanel.Camera is obsolete, but it's the path that lets us add the gizmo world and drive the camera ourselves

namespace ShaderGraphPlusPanelTest;

/// <summary>
/// The Preview dock: a lit model in an SGP-owned scene, seen through an orbit camera over an
/// editor-style grid that can be switched off. Left drag orbits, right drag looks around,
/// middle drag pans, the wheel zooms. Once clicked (so it has the keyboard), WASD flies,
/// Q and E go down and up, and Shift goes faster.
/// The overlay's toolbar picks the model, like the Qt preview: a box, a sphere, a plane, or any
/// model asset. It also pauses and speeds up the shader's preview time.
/// </summary>
public sealed class PreviewPanel : Panel
{
	const string BoxModel = "models/dev/box.vmdl";
	const string PlaneModel = "models/dev/plane.vmdl";
	const string GridCookie = "shadergraphplus.preview.grid";

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

	/// <summary>
	/// Shader time, as the preview shaders read it. The compiler's Time node reads this attribute
	/// instead of g_flTime when compiling for the preview (see <c>GraphCompiler.IsPreview</c>), and
	/// the Qt preview feeds it the same way.
	/// </summary>
	const string PreviewTimeAttribute = "g_flPreviewTime";

	static readonly float[] TimeScales = [0.25f, 0.5f, 1.0f, 2.0f, 4.0f];

	readonly Scene _previewScene;
	readonly ScenePanel _scene;
	readonly Gizmo.Instance _gizmo = new();
	readonly GameObject _modelObject;
	readonly ModelRenderer _modelRenderer;
	readonly Panel _gridButton;
	readonly Dictionary<Primitive, Panel> _modelButtons = new();
	readonly Label _playIcon;
	readonly Label _speedText;
	readonly Panel _playButton;
	readonly Panel _speedButton;

	Sandbox.UI.Menu _speedMenu;
	Model _sphere;

	Primitive _primitive;
	Model _model;

	/// <summary>Draw the ground grid. Remembered between sessions.</summary>
	public bool ShowGrid
	{
		get => _showGrid;
		set
		{
			_showGrid = value;
			_gridButton.SetClass( "on", value );
			EditorCookie.Set( GridCookie, value );
		}
	}

	bool _showGrid;

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
				return;
			}

			SetModel( value, Primitive.Custom );
		}
	}

	/// <summary>Freeze shader preview time.</summary>
	public bool Paused
	{
		get => _paused;
		set
		{
			_paused = value;
			_playIcon.Text = value ? "play_arrow" : "pause";
			_playButton.Tooltip = value ? "Resume shader time" : "Pause shader time";
			_playButton.SetClass( "on", value );
		}
	}

	bool _paused;

	/// <summary>How fast shader preview time runs, as a multiple of real time.</summary>
	public float TimeScale
	{
		get => _timeScale;
		set
		{
			_timeScale = value;
			_speedText.Text = $"{value:0.##}x";
		}
	}

	float _timeScale = 1.0f;
	float _previewTime;

	Vector3 _target;
	Angles _angles = new( 25, 135, 0 );
	float _distance = 150;

	MouseButtons? _dragging;

	// Fly keys being held, by normalised button name ("w", "lshift")
	readonly HashSet<string> _held = new();

	/// <summary>How far the fly keys move the camera per second, as a share of its distance to the orbit point.</summary>
	const float FlySpeed = 0.5f;

	/// <summary>How much faster flying is with Shift held.</summary>
	const float FlyBoost = 4.0f;

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
			var light = _previewScene.CreateObject();
			light.Name = "Preview Light";
			light.WorldRotation = Rotation.From( 50, 30, 0 );
			light.Components.Create<DirectionalLight>().LightColor = Color.White * 1.5f;

			_modelObject = _previewScene.CreateObject();
			_modelObject.Name = "Preview Model";
			_modelRenderer = _modelObject.Components.Create<ModelRenderer>();
		}

		_scene = AddChild<ScenePanel>();
		_scene.AddClass( "scene" );
		_scene.RenderScene = _previewScene;
		_scene.Camera.World = _previewScene.SceneWorld;
		_scene.Camera.BackgroundColor = new Color( 0.07f, 0.075f, 0.09f );
		_scene.Camera.AmbientLightColor = new Color( 0.18f, 0.19f, 0.23f );
		_scene.Camera.ZFar = 10000;
		_scene.Camera.Worlds.Add( _gizmo.World );

		var bar = Add.Panel( "overlay" );

		AddModelButton( bar, Primitive.Box, "view_in_ar", "Box" );
		AddModelButton( bar, Primitive.Sphere, "circle", "Sphere" );
		AddModelButton( bar, Primitive.Plane, "square", "Plane" );
		AddButton( bar, "folder_open", "Pick a model..." ).AddEventListener( "onclick", PickModel );

		bar.Add.Panel( "separator" );

		_playButton = AddButton( bar, "pause", "Pause shader time" );
		_playButton.AddEventListener( "onclick", () => Paused = !Paused );
		_playIcon = _playButton.GetChild( 0 ) as Label;

		_speedButton = bar.Add.Panel( "iconbutton speed" );
		_speedButton.Tooltip = "Shader time speed";
		_speedButton.AddEventListener( "onmousedown", e => e.StopPropagation() );
		_speedButton.AddEventListener( "onclick", OpenSpeedMenu );
		_speedText = _speedButton.Add.Label( "1x", "speed-text" );

		bar.Add.Panel( "spacer" );

		_gridButton = AddButton( bar, "grid_on", "Show grid" );
		_gridButton.AddEventListener( "onclick", () => ShowGrid = !ShowGrid );

		var footer = Add.Panel( "footer" );
		footer.Add.Label( "LMB orbit · RMB look · MMB pan · WASD/QE fly · Shift fast · wheel zoom", "hint" );

		// The box is the default
		ShowPrimitive( Primitive.Box );

		Paused = false;
		TimeScale = 1.0f;
		ShowGrid = EditorCookie.Get( GridCookie, true );
	}

	/// <summary>
	/// A square icon button on the overlay. Its press doesn't reach the viewport, so using the
	/// toolbar never starts an orbit drag.
	/// </summary>
	static Panel AddButton( Panel parent, string icon, string tooltip )
	{
		var button = parent.IconButton( icon, null );
		button.Tooltip = tooltip;
		button.AddEventListener( "onmousedown", e => e.StopPropagation() );
		return button;
	}

	void AddModelButton( Panel parent, Primitive primitive, string icon, string tooltip )
	{
		var button = AddButton( parent, icon, tooltip );
		button.AddEventListener( "onclick", () => ShowPrimitive( primitive ) );
		_modelButtons[primitive] = button;
	}

	/// <summary>
	/// The box and plane are the engine's dev models; the sphere is the Qt preview's own
	/// tessellated one, built the first time it's asked for.
	/// </summary>
	void ShowPrimitive( Primitive primitive )
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
	void SetModel( Model model, Primitive primitive )
	{
		_model = model;
		_primitive = primitive;
		_modelRenderer.Model = model;

		var bounds = model.RenderBounds;
		_modelObject.WorldPosition = Vector3.Up * -bounds.Mins.z;

		_target = _modelObject.WorldPosition + bounds.Center;
		_distance = MathF.Max( bounds.Size.Length * 1.75f, 40.0f );

		foreach ( var (kind, button) in _modelButtons )
			button.SetClass( "on", kind == primitive );
	}

	/// <summary>
	/// The editor's asset browser, like the Qt preview's model button: whatever is highlighted
	/// shows straight away, and picking keeps it.
	/// </summary>
	void PickModel()
	{
		var picker = AssetPicker.Create( null, AssetType.Model );
		picker.Title = "Select Preview Model";
		picker.OnAssetHighlighted = assets => UseAsset( assets.FirstOrDefault() );
		picker.OnAssetPicked = assets => UseAsset( assets.FirstOrDefault() );
		picker.Show();

		if ( _primitive == Primitive.Custom && !string.IsNullOrEmpty( _model?.ResourcePath ) )
			picker.SetSelection( _model.ResourcePath );
	}

	void UseAsset( Asset asset )
	{
		if ( asset?.LoadResource<Model>() is { } model && _previewScene.IsValid() )
			Model = model;
	}

	void OpenSpeedMenu()
	{
		_speedMenu?.Delete( true );

		var menu = new Sandbox.UI.Menu();
		_speedMenu = menu;

		foreach ( var scale in TimeScales )
		{
			var value = scale;
			menu.AddOption( $"{value:0.##}x", value == TimeScale ? "check" : null, () => TimeScale = value );
		}

		menu.Closed += OnSpeedMenuClosed;
		menu.Open( _speedButton, Popup.PositionMode.UnderMouse );
	}

	void OnSpeedMenuClosed( Sandbox.UI.Menu menu )
	{
		if ( _speedMenu == menu ) _speedMenu = null;
		menu.Delete( true );
	}

	public void LoadSettings( PreviewSettings settings )
	{
		// The preview has no per-graph settings yet - nothing to apply.
	}

	public override void Tick()
	{
		if ( !_previewScene.IsValid() ) return;

		UpdateTime();
		UpdateCamera();
		DrawGizmos();
	}

	/// <summary>
	/// Shader preview time runs on its own clock so it can stop and change speed. It's set on the
	/// model's renderer, which is where the Qt preview sets it. The engine's own g_flTime can't be
	/// steered from here: it is picked per frame from real time, and the view-level override
	/// (<see cref="Sandbox.Rendering.ViewSetup.Time"/>) isn't reachable through a ScenePanel.
	/// </summary>
	void UpdateTime()
	{
		if ( !Paused )
			_previewTime += RealTime.Delta * TimeScale;

		_modelRenderer.Attributes.Set( PreviewTimeAttribute, _previewTime );
	}

	void UpdateCamera()
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
		_scene.Camera.Rotation = rotation;
		_scene.Camera.Position = _target - rotation.Forward * _distance;
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
		if ( _held.Count == 0 ) return;

		var rotation = _angles.ToRotation();
		var direction = Vector3.Zero;

		if ( _held.Contains( "w" ) ) direction += rotation.Forward;
		if ( _held.Contains( "s" ) ) direction -= rotation.Forward;
		if ( _held.Contains( "d" ) ) direction += rotation.Right;
		if ( _held.Contains( "a" ) ) direction -= rotation.Right;
		if ( _held.Contains( "e" ) ) direction += Vector3.Up;
		if ( _held.Contains( "q" ) ) direction -= Vector3.Up;

		if ( direction.IsNearlyZero() ) return;

		var speed = _distance * FlySpeed;
		if ( _held.Contains( "lshift" ) || _held.Contains( "rshift" ) ) speed *= FlyBoost;

		_target += direction.Normal * speed * RealTime.Delta;
	}

	static readonly HashSet<string> FlyKeys = ["w", "a", "s", "d", "q", "e", "lshift", "rshift"];

	/// <summary>Key presses and releases reach the focused panel - the view, once it's been clicked.</summary>
	public override void OnButtonEvent( ButtonEvent e )
	{
		if ( !FlyKeys.Contains( e.Button ) )
		{
			base.OnButtonEvent( e );
			return;
		}

		if ( e.Pressed ) _held.Add( e.Button );
		else _held.Remove( e.Button );
	}

	/// <summary>Releases are only sent to the focused panel - let go of everything rather than fly on forever.</summary>
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
		var cursor = _scene.MousePosition;
		_gizmo.Input.Camera = _scene.Camera;
		_gizmo.Input.CursorPosition = cursor;
		_gizmo.Input.CursorRay = _scene.Camera.GetRay( cursor, _scene.Box.Rect.Size );
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
		if ( _dragging is not null ) return;

		_dragging = e.MouseButton;
		SetMouseCapture( true );

		e.StopPropagation();
	}

	protected override void OnMouseUp( MousePanelEvent e )
	{
		if ( _dragging != e.MouseButton ) return;

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
		_gizmo.Dispose();

		// The ScenePanel doesn't own the scene (we set it), so it's on us
		if ( _previewScene.IsValid() )
			_previewScene.Destroy();

		base.OnDeleted();
	}
}
