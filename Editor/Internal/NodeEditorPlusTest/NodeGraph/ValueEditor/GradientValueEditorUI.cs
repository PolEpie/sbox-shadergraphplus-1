using Editor;
using Sandbox.UI;
using EngineGraph = EngineNodeGraph;

namespace NodeEditorPlusTest;

/// <summary>
/// Panel version of <see cref="NodeEditorPlus.GradientValueEditor"/>. The engine has no panel gradient
/// editor, so a click opens the same Qt <c>GradientEditorWidget</c> popup the Qt editor does.
/// </summary>
public class GradientValueEditorUI : EngineGraph.NodeValueEditor
{
	public string Title { get; set; }
	public Gradient Value { get; set; }

	/// <summary>
	/// Property the gradient is read from and written to.
	/// </summary>
	public SerializedProperty ValueProperty { get; set; }

	private global::Editor.PopupWidget _popup;

	public GradientValueEditorUI( EngineGraph.PlugPanel plug ) : base( plug )
	{
		Style.Cursor = "pointer";
	}

	public override void Tick()
	{
		base.Tick();

		if ( ValueProperty is not null )
			Value = ValueProperty.GetValue<Gradient>();
	}

	protected override void Paint( ref EngineGraph.NodePaint paint, Rect localRect )
	{
		var bg = Theme.ControlBackground.WithAlpha( 0.4f );
		var fg = Theme.TextControl;

		if ( !paint.HasMouseOver )
		{
			bg = bg.Darken( 0.1f );
			fg = fg.Darken( 0.1f );
		}

		var rect = localRect.Shrink( 1 );
		paint.ClearPen();
		paint.SetBrush( bg );
		paint.DrawRect( rect, 2 );

		PaintBlock( ref paint, Value, rect.Shrink( 2 ) );
	}

	private void PaintBlock( ref EngineGraph.NodePaint paint, Gradient gradient, Rect rect )
	{
		paint.ClearPen();

		float pixelWidth = 1;

		// this is kind of a lazy way of doing it but
		// it works and is accurate as can be so who cares
		for ( float x = (int)rect.Left; x <= (int)rect.Right; x += pixelWidth )
		{
			float w = pixelWidth;

			if ( x + pixelWidth > rect.Right )
				w = rect.Right - x;

			float normalizedX = (x - rect.Left) / rect.Width;
			var c = gradient.Evaluate( normalizedX );
			paint.SetBrush( c );
			paint.DrawRect( new Rect( x, rect.Top, w, rect.Height ) );
		}
	}

	protected override void OnMouseDown( MousePanelEvent e )
	{
		base.OnMouseDown( e );

		if ( !Enabled )
			return;

		if ( e.MouseButton != MouseButtons.Left )
			return;

		if ( ValueProperty is null )
			return;

		if ( _popup is { IsValid: true } )
		{
			e.StopPropagation();
			return;
		}

		OpenGradientEditorPopup( ( v ) =>
		{
			Value = v;
			ValueProperty.SetValue( v );
			Plug.Node.MarkNodeChanged();
		} );

		e.StopPropagation();
	}

	private void OpenGradientEditorPopup( Action<Gradient> onChange )
	{
		var popup = new global::Editor.PopupWidget( null );
		popup.WindowTitle = "Gradient Editor";
		popup.SetWindowIcon( "gradient" );
		popup.Layout = global::Editor.Layout.Column();
		popup.Layout.Margin = 8;
		popup.FixedHeight = 350;
		popup.FixedWidth = 500;
		popup.Position = global::Editor.Application.CursorPosition;

		var editor = popup.Layout.Add( new global::Editor.GradientEditorWidget( popup ), 1 );
		editor.Value = Value;
		editor.ValueChanged += ( v ) => onChange?.Invoke( v );

		popup.Show();
		popup.ConstrainToScreen();

		_popup = popup;
	}
}
