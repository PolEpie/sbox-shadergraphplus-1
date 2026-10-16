using Sandbox.UI;

namespace ShaderGraphPlusPanelTest;

/// <summary>
/// The node graph surface. Middle-drag pans, the wheel zooms around the cursor.
/// </summary>
public sealed class GraphViewPanel : CanvasPanel
{
	/// <summary>
	/// Spacing of the minor grid lines, in canvas units.
	/// </summary>
	private const float GridSize = 16f;

	/// <summary>
	/// Every Nth minor line is drawn as a major line.
	/// </summary>
	private const int MajorEvery = 8;

	/// <summary>
	/// Minor lines closer than this on screen are merged, so zooming out never draws thousands of lines.
	/// </summary>
	private const float MinLineSpacingPixels = 8f;

	private static readonly Color MinorColor = Color.White.WithAlpha( 0.04f );
	private static readonly Color MajorColor = Color.White.WithAlpha( 0.09f );

	public GraphViewPanel() : base()
	{
		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );
		AddClass( "sg-graph" );

		MinimumViewSpan = 100;
		MaximumViewSpan = 20000;

		ResetView();
	}

	public void ResetView() => SetView( new Vector2( -500, -300 ), new Vector2( 500, 300 ) );

	public override void OnDraw( Painter painter )
	{
		var viewport = Viewport;
		if ( viewport.Width <= 0 || viewport.Height <= 0 ) return;

		using var scope = painter.Scope();
		painter.Clip( viewport );

		var min = ScreenToCanvas( viewport.Position );
		var max = ScreenToCanvas( viewport.Position + viewport.Size );
		var pixelsPerUnit = viewport.Width / (max.x - min.x);

		var step = GridSize;
		while ( step * pixelsPerUnit < MinLineSpacingPixels ) step *= 2;
		var major = Math.Max( step, GridSize * MajorEvery );

		if ( step < major ) DrawLines( painter, min, max, step, major, MinorColor );
		DrawLines( painter, min, max, major, 0, MajorColor );
	}

	/// <summary>
	/// Vertical and horizontal lines every <paramref name="step"/> units, skipping multiples of
	/// <paramref name="skipEvery"/> so minor lines never overlap the major ones and double their alpha.
	/// </summary>
	private void DrawLines( Painter painter, Vector2 min, Vector2 max, float step, float skipEvery, Color color )
	{
		painter.Stroke = Stroke.Solid( color, 1 );

		for ( var x = MathF.Ceiling( min.x / step ) * step; x <= max.x; x += step )
		{
			if ( skipEvery > 0 && IsMultiple( x, skipEvery ) ) continue;
			painter.Line( CanvasToScreen( new Vector2( x, min.y ) ), CanvasToScreen( new Vector2( x, max.y ) ) );
		}

		for ( var y = MathF.Ceiling( min.y / step ) * step; y <= max.y; y += step )
		{
			if ( skipEvery > 0 && IsMultiple( y, skipEvery ) ) continue;
			painter.Line( CanvasToScreen( new Vector2( min.x, y ) ), CanvasToScreen( new Vector2( max.x, y ) ) );
		}
	}

	private static bool IsMultiple( float value, float of )
	{
		var r = MathF.Abs( value % of );
		return r < 0.001f || of - r < 0.001f;
	}
}
