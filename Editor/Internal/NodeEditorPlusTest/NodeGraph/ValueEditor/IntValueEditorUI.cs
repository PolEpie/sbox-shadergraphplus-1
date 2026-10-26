using Editor;
using Sandbox.UI;
using EngineGraph = EngineNodeGraph;

namespace NodeEditorPlusTest;

/// <summary>
/// Panel version of <see cref="NodeEditorPlus.IntValueEditor"/>. Value, Min and Max are read from
/// the node's serialized properties every frame; edits are written straight back.
/// </summary>
public class IntValueEditorUI : EngineGraph.NodeValueEditor
{
	public string Title { get; set; }
	public float Value { get; set; }
	public int Min { get; set; } = 0;
	public int Max { get; set; } = 1;

	/// <summary>
	/// Property the value is read from and written to.
	/// </summary>
	public SerializedProperty ValueProperty { get; set; }

	/// <summary>
	/// Optional properties that drive <see cref="Min"/> and <see cref="Max"/>.
	/// </summary>
	public SerializedProperty MinProperty { get; set; }
	public SerializedProperty MaxProperty { get; set; }

	private bool IsRange => Min != Max;

	public override float PreferredWidth
	{
		get
		{
			var title = string.IsNullOrWhiteSpace( Title ) ? 0f : EngineGraph.NodePaint.Measure( Title ).x + 8f;
			// Every digit measured as '0': proportional digits would resize the node as the value changes
			var value = EngineGraph.NodePaint.Measure( string.Concat( $"{Value:0}".Select( c => char.IsDigit( c ) ? '0' : c ) ) ).x;

			return 4f + (IsRange ? 14f : 24f) + title + value;
		}
	}

	private Vector2 _lastCursorPos;
	private bool _pressed;
	private bool _rangeCursor;

	public IntValueEditorUI( EngineGraph.PlugPanel plug ) : base( plug )
	{
		Style.Cursor = "pointer";
		_rangeCursor = true;
	}

	public override void Tick()
	{
		base.Tick();

		if ( MinProperty is not null ) Min = MinProperty.GetValue<int>();
		if ( MaxProperty is not null ) Max = MaxProperty.GetValue<int>();

		if ( ValueProperty is not null )
		{
			// While dragging, Value holds the fractional drag; only take the property's value once it disagrees
			var current = ValueProperty.GetValue<int>();
			if ( !_pressed || (int)MathF.Round( Value ) != current )
				Value = current;
		}

		if ( _pressed && !HasActive )
			_pressed = false;

		if ( _rangeCursor != IsRange )
		{
			_rangeCursor = IsRange;
			Style.Cursor = _rangeCursor ? "pointer" : "ew-resize";
		}
	}

	protected override void Paint( ref EngineGraph.NodePaint paint, Rect localRect )
	{
		var field = EngineGraph.NodeTheme.FieldRect( localRect );
		var hover = paint.HasMouseOver || HasActive;

		paint.SetPen( hover ? EngineGraph.NodeTheme.OutlineHover : EngineGraph.NodeTheme.Outline );
		paint.SetBrush( EngineGraph.NodeTheme.FieldBackground );
		paint.DrawRect( field, 4f );

		var shrink = 7f;

		if ( IsRange )
		{
			var t = Math.Clamp( (Value - Min) / (Max - Min), 0f, 1f );

			if ( t > 0f )
			{
				var fill = field.Shrink( 1f );
				fill.Width *= t;

				paint.ClearPen();
				paint.SetBrush( EngineGraph.NodeTheme.Accent.WithAlpha( hover ? 0.5f : 0.38f ) );
				paint.DrawRect( fill, 3f );
			}
		}
		else
		{
			shrink = 12f;

			if ( hover )
			{
				paint.SetPen( EngineGraph.NodeTheme.TextMuted );
				paint.DrawIcon( field.Shrink( 1f, 0f ), "chevron_left", 10f, TextFlag.LeftCenter );
				paint.DrawIcon( field.Shrink( 1f, 0f ), "chevron_right", 10f, TextFlag.RightCenter );
			}
		}

		paint.SetDefaultFont();

		if ( !string.IsNullOrWhiteSpace( Title ) )
		{
			paint.SetPen( EngineGraph.NodeTheme.TextMuted.Lighten( hover ? 0.3f : 0f ) );
			paint.DrawText( field.Shrink( shrink, 0f ), Title, TextFlag.LeftCenter );
		}

		paint.SetPen( EngineGraph.NodeTheme.Text );
		paint.DrawText( field.Shrink( shrink, 0f ), $"{Value:0}", TextFlag.RightCenter );
	}

	/// <summary>
	/// Editor width in node units.
	/// </summary>
	private float Width => Box.Rect.Width / ScaleToScreen;

	/// <summary>
	/// Cursor x in the editor, in node units.
	/// </summary>
	private float LocalCursorX => MousePosition.x * ScaleFromScreen;

	protected override void OnMouseDown( MousePanelEvent e )
	{
		base.OnMouseDown( e );

		if ( !Enabled )
			return;

		if ( e.MouseButton != MouseButtons.Left )
			return;

		_pressed = true;

		if ( IsRange )
		{
			UpdateValue( LocalCursorX );
		}
		else
		{
			_lastCursorPos = MousePosition;
		}

		e.StopPropagation();
	}

	protected override void OnMouseUp( MousePanelEvent e )
	{
		base.OnMouseUp( e );

		_pressed = false;
	}

	protected override void OnMouseMove( MousePanelEvent e )
	{
		base.OnMouseMove( e );

		if ( !Enabled )
			return;

		if ( !_pressed )
			return;

		if ( IsRange )
		{
			UpdateValue( LocalCursorX );
		}
		else
		{
			var cursorPos = MousePosition;
			var delta = cursorPos - _lastCursorPos;
			_lastCursorPos = cursorPos;

			Value += delta.x * 0.01f;
			Commit();
		}

		e.StopPropagation();
	}

	private void UpdateValue( float position )
	{
		var field = EngineGraph.NodeTheme.FieldRect( new Rect( 0f, 0f, Width, Box.Rect.Height / ScaleToScreen ) ).Shrink( 1f );

		Value = (position - field.Left) / field.Width;
		Value = ((float)Min).LerpTo( Max, Value ).Clamp( Min, Max );
		Value = (float)Math.Round( Value / 1.0f ) * 1.0f;

		Commit();
	}

	private void Commit()
	{
		ValueProperty?.SetValue( (int)MathF.Round( Value ) );
		Plug.Node.Graph.NodeValueChanged();
		Plug.Node.MarkNodeChanged();
	}
}
