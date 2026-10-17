
using Sandbox.UI;
using ShaderGraphPlus;
using ShaderGraphPlusPanelTest;

namespace NodeEditorPlusTest;

/// <summary>
/// The node graph surface. Middle-drag pans, the wheel zooms around the cursor, and palette
/// rows dropped here become <see cref="GraphNodeCard"/>s at the cursor.
/// </summary>
public class GraphViewPanel : CanvasPanel
{
	/// <summary>
	/// Spacing of the minor grid lines, in canvas units. Nodes snap to it.
	/// </summary>
	public const float GridSize = 16f;

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

	/// <summary>
	/// The palette row being dragged over us, picked up from the bubbled ondragenter.
	/// </summary>
	private TreeRow _dragSource;

	/// <summary>
	/// See-through card showing where the dragged palette node will land.
	/// </summary>
	private GraphNodeCard _dropPreview;

	public GraphViewPanel() : base()
	{
		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );
		AddClass( "sg-graph" );

		MinimumViewSpan = 100;
		MaximumViewSpan = 20000;

		ResetView();
	}

	public void ResetView() => SetView( new Vector2( -500, -300 ), new Vector2( 500, 300 ) );

	/// <summary>
	/// Place a node card with its top left at <paramref name="position"/>, snapped to the grid.
	/// </summary>
	public GraphNodeCard AddNode( TypeDescription type, Vector2 position )
	{
		return Content.AddChild( new GraphNodeCard( type, position.SnapToGrid( GridSize ) ) );
	}

	//public GraphNodeCard CreateNewNode( TypeDescription type, Vector2 position )
	//{
	//	return Content.AddChild( new GraphNodeCard( type, SnapToGrid( position ) ) );
	//}

	public GraphNodeCard CreateNewNode( INodeType type, Vector2 position )
	{
		return CreateNewNode( type, position.SnapToGrid( GridSize ), node =>
			node.Position = position.SnapToGrid( GridSize ) );
	}

	public GraphNodeCard CreateNewNode( INodeType type, Vector2 position, Action<IGraphNode> onCreated = null )
	{
		if ( type == null )
			return null;

		SGPLogger.Info( $"TODO : GraphViewPanel.CreateNewNode()" );

		var typeDescription = type.CreateNodeTest();

		return AddNode( typeDescription, position );
	}

	/// <summary>
	/// Round a canvas position to the nearest grid intersection.
	/// </summary>
	public static Vector2 SnapToGrid( Vector2 position )
	{
		return position.SnapToGrid( GridSize );
	}

	/// <summary>
	/// The shader node type behind a dragged palette row, or null for categories and anything else.
	/// </summary>
	private static TypeDescription PaletteType( Panel panel )
	{
		if ( panel is not TreeRow { RowIndex: >= 0, Tree: TreeView<Node> palette } row ) return null;
		return palette.GetRowItem( row.RowIndex ).Type;
	}

	protected override void OnDragEnter( PanelEvent e )
	{
		base.OnDragEnter( e );

		if ( e.Target is TreeRow row && PaletteType( row ) is not null )
			_dragSource = row;
	}

	/// <summary>
	/// A palette row was released over the graph: <paramref name="e"/>'s target is the dragged row.
	/// </summary>
	protected override void OnDrop( PanelEvent e )
	{
		// DropEvent is files/text from outside the app, not a panel drag
		if ( e is DropEvent ) return;

		var type = PaletteType( e.Target );
		if ( type is null ) return;

		ClearDropPreview();

		AddNode( type, ScreenToCanvas( MousePosition ) );
		e.StopPropagation();
	}

	public override void Tick()
	{
		base.Tick();
		UpdateDropPreview();
	}

	/// <summary>
	/// Keep the preview under the cursor, snapped like <see cref="AddNode"/> so the node lands exactly
	/// where its preview was. Gone as soon as the drag ends or leaves the graph.
	/// </summary>
	private void UpdateDropPreview()
	{
		if ( _dragSource is not null && (!_dragSource.IsValid() || _dragSource.Tree is not { IsDragging: true }) )
			_dragSource = null;

		var type = _dragSource is not null && Viewport.IsInside( MousePosition ) ? PaletteType( _dragSource ) : null;
		if ( type is null )
		{
			ClearDropPreview();
			return;
		}

		var position = ScreenToCanvas( MousePosition ).SnapToGrid( GridSize );

		if ( _dropPreview is null || _dropPreview.Type != type )
		{
			ClearDropPreview();
			_dropPreview = Content.AddChild( new GraphNodeCard( type, position, isPreview: true ) );
		}
		else
		{
			_dropPreview.MoveTo( position );
		}
	}

	private void ClearDropPreview()
	{
		_dropPreview?.Delete( true );
		_dropPreview = null;
	}

	public override void OnDraw( Painter painter )
	{
		var viewport = Viewport;

		if ( viewport.Width <= 0 || viewport.Height <= 0 )
			return;

		using var scope = painter.Scope();
		painter.Clip( viewport );

		var min = ScreenToCanvas( viewport.Position );
		var max = ScreenToCanvas( viewport.Position + viewport.Size );
		var pixelsPerUnit = viewport.Width / (max.x - min.x);

		var step = GridSize;

		while ( step * pixelsPerUnit < MinLineSpacingPixels )
		{
			step *= 2;
		}

		var major = Math.Max( step, GridSize * MajorEvery );

		if ( step < major )
		{
			DrawLines( painter, min, max, step, major, MinorColor );
		}

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
			if ( skipEvery > 0 && IsMultiple( x, skipEvery ) )
				continue;

			painter.Line( CanvasToScreen( new Vector2( x, min.y ) ), CanvasToScreen( new Vector2( x, max.y ) ) );
		}

		for ( var y = MathF.Ceiling( min.y / step ) * step; y <= max.y; y += step )
		{
			if ( skipEvery > 0 && IsMultiple( y, skipEvery ) )
				continue;

			painter.Line( CanvasToScreen( new Vector2( min.x, y ) ), CanvasToScreen( new Vector2( max.x, y ) ) );
		}
	}

	private static bool IsMultiple( float value, float of )
	{
		var r = MathF.Abs( value % of );
		return r < 0.001f || of - r < 0.001f;
	}
}
