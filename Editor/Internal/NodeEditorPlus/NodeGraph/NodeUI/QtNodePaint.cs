using Editor;
using EngineNodeGraph;
using System.Runtime.CompilerServices;

namespace NodeEditorPlus;

/// <summary>
/// <see cref="INodePaint"/> over Qt's <see cref="Paint"/>, so a node's drawing is shared with the panel graph.
/// </summary>
public readonly struct QtNodePaint : INodePaint
{
	static readonly ConditionalWeakTable<Texture, Pixmap> Pixmaps = new();

	/// <summary>
	/// The texture as a pixmap, converted once it has loaded.
	/// </summary>
	static Pixmap ToPixmap( Texture texture )
	{
		if ( texture is null || !texture.IsLoaded ) return null;
		return Pixmaps.GetValue( texture, x => Pixmap.FromTexture( x ) );
	}

	public bool HasMouseOver => Paint.HasMouseOver;

	public void SetPen( Color color, float width = 1 ) => Paint.SetPen( color, width );

	public void ClearPen() => Paint.ClearPen();

	public void SetBrush( Color color ) => Paint.SetBrush( color );

	public void SetBrush( Texture texture )
	{
		if ( ToPixmap( texture ) is { } pixmap ) Paint.SetBrush( pixmap );
		else Paint.ClearBrush();
	}

	public void ClearBrush() => Paint.ClearBrush();

	public void SetDefaultFont( float size = 8, int weight = 400, bool italic = false ) => Paint.SetDefaultFont( size, weight, italic );

	public void SetFont( string name, float size, int weight = 400, bool italic = false )
	{
		name ??= Theme.DefaultFont;

		// Qt can't apply bold weights, so use the Bold family
		if ( weight >= 700 && !name.EndsWith( " Bold" ) )
		{
			Paint.SetFont( $"{name} Bold", size, 400, italic );
			return;
		}

		Paint.SetFont( name, size, weight, italic );
	}

	public void DrawRect( Rect rect, float radius = 0 ) => Paint.DrawRect( rect, radius );

	public void DrawCircle( Rect rect ) => Paint.DrawCircle( rect );

	public void DrawLine( Vector2 from, Vector2 to ) => Paint.DrawLine( from, to );

	public void DrawPolygon( params ReadOnlySpan<Vector2> points ) => Paint.DrawPolygon( points.ToArray() );

	public Rect DrawText( Rect rect, string text, TextFlag flags = TextFlag.Center ) => Paint.DrawText( rect, text, flags );

	public void DrawIcon( Rect rect, string icon, float size, TextFlag flags = TextFlag.Center ) => Paint.DrawIcon( rect, icon, size, flags );

	public Vector2 MeasureText( string text ) => Paint.MeasureText( text );

	public void Draw( Rect rect, Texture texture )
	{
		if ( ToPixmap( texture ) is { } pixmap ) Paint.Draw( rect, pixmap );
	}
}
