using Editor;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;

#pragma warning disable CS0618 // ScenePanel.Camera is obsolete, but it's the path that lets us add the gizmo world and drive the camera ourselves

namespace ShaderGraphPlusPanelTest;

/// <summary>
/// The Preview dock: a lit cube in an SGP-owned scene, seen through an orbit camera over an
/// editor-style grid that can be switched off. Left drag orbits, right drag looks around,
/// middle drag pans, the wheel zooms. Once clicked (so it has the keyboard), WASD flies,
/// Q and E go down and up, and Shift goes faster.
/// </summary>
public sealed class PreviewPanel : Panel
{
	const string PreviewModel = "models/dev/box.vmdl";
	const string GridCookie = "shadergraphplus.preview.grid";

	readonly Scene _previewScene;
	readonly ScenePanel _scene;
	readonly Gizmo.Instance _gizmo = new();
	readonly Panel _gridButton;

	/// <summary>Draw the ground grid. Remembered between sessions.</summary>
	public bool ShowGrid
	{
		get => _showGrid;
		set
		{
			_showGrid = value;
			_gridButton.SetClass( "active", value );
			EditorCookie.Set( GridCookie, value );
		}
	}

	bool _showGrid;

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
		var cubeCenter = BuildScene( _previewScene );

		_scene = AddChild<ScenePanel>();
		_scene.AddClass( "scene" );
		_scene.RenderScene = _previewScene;
		_scene.Camera.World = _previewScene.SceneWorld;
		_scene.Camera.BackgroundColor = new Color( 0.07f, 0.075f, 0.09f );
		_scene.Camera.AmbientLightColor = new Color( 0.18f, 0.19f, 0.23f );
		_scene.Camera.ZFar = 10000;
		_scene.Camera.Worlds.Add( _gizmo.World );

		_target = cubeCenter;

		var overlay = Add.Panel( "overlay" );
		overlay.Add.Label( "LMB orbit · RMB look · MMB pan · WASD/QE fly · Shift fast · wheel zoom", "hint" );

		_gridButton = overlay.Add.Panel( "iconbutton" );
		_gridButton.Add.Icon( "grid_on" );
		_gridButton.Tooltip = "Show grid";
		_gridButton.AddEventListener( "onmousedown", e => e.StopPropagation() );   // not an orbit drag
		_gridButton.AddEventListener( "onclick", () => ShowGrid = !ShowGrid );

		ShowGrid = EditorCookie.Get( GridCookie, true );
	}

	/// <summary>
	/// A cube resting on the grid with its default material, and a directional light so it's lit.
	/// </summary>
	/// <returns>The middle of the cube, which the camera orbits.</returns>
	static Vector3 BuildScene( Scene scene )
	{
		var center = Vector3.Zero;

		using ( scene.Push() )
		{
			var light = scene.CreateObject();
			light.Name = "Preview Light";
			light.WorldRotation = Rotation.From( 50, 30, 0 );
			light.Components.Create<DirectionalLight>().LightColor = Color.White * 1.5f;

			var model = Model.Load( PreviewModel );

			var cube = scene.CreateObject();
			cube.Name = "Cube";
			cube.WorldPosition = Vector3.Up * -model.Bounds.Mins.z;
			cube.Components.Create<ModelRenderer>().Model = model;

			center = cube.WorldPosition + Vector3.Up * model.Bounds.Center.z;
		}

		return center;
	}

	public void LoadSettings( PreviewSettings settings )
	{
		// The cube preview has no per-graph settings yet - nothing to apply.
	}

	public override void Tick()
	{
		if ( !_previewScene.IsValid() ) return;

		UpdateCamera();
		DrawGizmos();
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
		_gizmo.Dispose();

		// The ScenePanel doesn't own the scene (we set it), so it's on us
		if ( _previewScene.IsValid() )
			_previewScene.Destroy();

		base.OnDeleted();
	}
}
