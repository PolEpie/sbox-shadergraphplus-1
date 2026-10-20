using NodeEditorPlus;
using Editor;
using Sandbox.UI;
using ShaderGraphPlus;
using EngineGraph = EngineNodeGraph;

namespace ShaderGraphPlusPanelTest;

/// <summary>
/// Panel version of <see cref="ShaderGraphPlus.DefaultEditor"/>: the default value bubble drawn left of an
/// unconnected input, outside the node. Presses fall through to the plug, as the Qt one forwards them.
/// </summary>
public class DefaultEditorUI : EngineGraph.NodeValueEditor
{
	string _text;
	object _rawValue;

	// In plug space
	Rect _valueRect;
	Rect _bounds;

	public override bool HideLabel => false;

	/// <summary>
	/// Drawn outside the node, so the plug keeps its label width.
	/// </summary>
	public override float PreferredWidth => 0f;

	public DefaultEditorUI( EngineGraph.PlugPanel plug ) : base( plug )
	{
		Style.Cursor = "pointer";

		// The bubble sits left of the plug, outside its box
		Style.Overflow = OverflowMode.Visible;
		plug.Style.Overflow = OverflowMode.Visible;
	}

	/// <summary>
	/// Ignore the plug's in-node rect: <see cref="Tick"/> sets the bubble bounds. Taking the rect here
	/// moved the bubble into the node for a frame whenever the node relaid out, such as on a value change.
	/// </summary>
	public override void Place( Rect rect ) { }

	public override void Tick()
	{
		base.Tick();

		Refresh();

		if ( _text is null )
		{
			Style.Width = 0;
			Style.Height = 0;
			return;
		}

		Style.Left = _bounds.Left;
		Style.Top = _bounds.Top;
		Style.Width = _bounds.Width;
		Style.Height = _bounds.Height;
	}

	/// <summary>
	/// Work out the value, its text and where the bubble goes; leaves <see cref="_text"/> null when there is nothing to show.
	/// </summary>
	void Refresh()
	{
		_text = null;

		if ( !Enabled ) return;
		if ( Plug is null ) return;
		if ( Plug.IsConnected ) return;
		if ( Plug?.Node?.Node is not ShaderNodePlus node ) return;
		if ( Plug.Inner is IPlugOut plugOut ) return;
		if ( Plug.Inner is IPlugIn plugIn )
		{
			if ( plugIn.ConnectedOutput is not null ) return;
		}

		var so = node.GetSerialized();
		Type type = typeof( Type );
		object rawVal = null;
		string val = null;
		foreach ( var property in so )
		{
			if ( property.TryGetAttribute<BaseNodePlus.InputDefaultAttribute>( out var inputDefault ) )
			{
				if ( inputDefault.Input == Plug.Inner.Identifier )
				{
					type = property.PropertyType;
					rawVal = property.GetValue<object>();
					val = rawVal.ToString();
					break;
				}
			}
		}
		if ( val is null && node is SubgraphNode subgraphNode && Plug.Inner is IPlugIn innerPlugIn )
		{
			if ( subgraphNode.InputReferences.TryGetValue( innerPlugIn, out var entry ) )
			{
				var subgraphInput = entry.inputParameter;
				if ( subgraphInput.IsRequired ) return;
				type = entry.inputValueType;
				if ( innerPlugIn.ConnectedOutput is not null )
				{
					rawVal = subgraphInput.GetValue();
					val = rawVal.ToString();
				}
				else
				{
					rawVal = subgraphNode.DefaultValues.GetValueOrDefault( innerPlugIn.Identifier );
					val = rawVal?.ToString() ?? "";
				}
			}
		}

		if ( string.IsNullOrEmpty( val ) ) return;

		var rect = new Rect( 0, Plug.Size );

		var shrink = 10f;
		var extraWidth = 0f;
		val = PaintHelper.FormatValue( type, rawVal, out extraWidth, out rawVal );

		if ( string.IsNullOrWhiteSpace( val ) ) return;

		var textSize = EngineGraph.NodePaint.Measure( val ) + extraWidth;

		var valueRect = new Rect( rect.Left - textSize.x - shrink * 2 - 8f, rect.Top, textSize.x + shrink * 2,
		rect.Height )
				.Shrink( 0f, 2f, 0f, 2f );

		_valueRect = valueRect;
		_bounds = valueRect.Grow( 24, 0 );
		_text = val;
		_rawValue = rawVal;
	}

	protected override void Paint( ref EngineGraph.NodePaint paint, Rect localRect )
	{
		if ( _text is null ) return;

		var rect = new Rect( 0, Plug.Size );

		// Painting is in this panel's space, which starts at the bubble's bounds
		var origin = _bounds.Position;
		var valueRect = new Rect( _valueRect.Position - origin, _valueRect.Size );
		rect = new Rect( rect.Position - origin, rect.Size );

		var handleConfig = Plug.HandleConfig;
		paint.SetPen( handleConfig.Color, 4f );
		paint.DrawLine( rect.Center.WithX( rect.Left - 9f - 2f ), rect.Center.WithX( rect.Left ) );
		PaintHelperUI.DrawValue( ref paint, handleConfig, valueRect, _text, 1f, "", _rawValue );
	}
}

internal static class PaintHelperUI
{
	static Texture _checker;

	/// <summary>
	/// Panel version of <see cref="PaintHelper.DrawValue"/>.
	/// </summary>
	public static void DrawValue( ref EngineGraph.NodePaint paint, EngineGraph.NodeHandleConfig handleConfig, Rect valueRect, string text, float pulseScale = 1f, string icon = null, object rawValue = null )
	{
		var bg = Theme.ControlBackground;
		var fg = Theme.TextControl;

		var borderColor = handleConfig.Color.Desaturate( 0.2f ).Darken( 0.3f );

		if ( pulseScale > 1f )
		{
			bg = Color.Lerp( bg, borderColor, (pulseScale - 1f) * 0.25f );
		}

		paint.SetPen( borderColor, 2f * (pulseScale * 0.5f + 0.5f) );
		paint.SetBrush( bg );
		paint.DrawRect( valueRect, 2 );

		if ( rawValue is Color color )
		{
			PaintSwatch( ref paint, color, new Rect( valueRect.Left + 3f, valueRect.Top + 3f, 14f, 14f ), 2 );
			valueRect = valueRect.Shrink( 14f, 0f, 0f, 0f );
		}

		paint.SetPen( fg );

		if ( !string.IsNullOrEmpty( icon ) )
		{
			paint.DrawIcon( new Rect( valueRect.Left + 8f, valueRect.Top, 16f, valueRect.Height ), icon, 16f );
			valueRect = valueRect.Shrink( 20f, 0f, 0f, 0f );
		}

		paint.DrawText( valueRect, text, TextFlag.Center );
	}

	/// <summary>
	/// A colour over the transparency checker, like <c>ColorPalette.PaintSwatch</c>.
	/// </summary>
	static void PaintSwatch( ref EngineGraph.NodePaint paint, Color color, Rect rect, float radius )
	{
		_checker ??= Texture.Load( "image/transparent-small.png" );

		paint.ClearPen();
		paint.SetBrush( _checker );
		paint.DrawRect( rect, radius );
		paint.SetBrush( color );
		paint.DrawRect( rect, radius );
	}
}
