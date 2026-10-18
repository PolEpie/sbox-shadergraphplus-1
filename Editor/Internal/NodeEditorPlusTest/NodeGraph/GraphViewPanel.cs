
using Editor;
using Sandbox.UI;
using ShaderGraphPlus;
using ShaderGraphPlusPanelTest;

namespace NodeEditorPlusTest;

/// <summary>
/// The node graph surface. Middle-drag pans, the wheel zooms around the cursor, and palette
/// rows dropped here become <see cref="NodeCardUI"/>s at the cursor.
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
	/// See-through card showing where the dragged palette node will land.
	/// </summary>
	private NodeCardUI _dropPreview;

	/// <summary>
	/// Set by <see cref="OnDragLeave"/>, resolved in <see cref="Tick"/>. Drag enter/leave bubble up from
	/// descendant cards, and children process their events before us, so crossing a card yields
	/// enter/leave pairs in either order. Only a leave that no enter cancelled in the same frame is real.
	/// </summary>
	private bool _dragLeavePending;

	readonly List<NodeCardUI> _cards = new();

	public bool HasDropped => _hasDropped;
	private bool _hasDropped;

	public GraphViewPanel() : base()
	{
		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );
		AddClass( "sg-graph" );

		MinimumViewSpan = 100;
		MaximumViewSpan = 20000;

		ResetView();
	}

	public void ResetView() => SetView( new Vector2( -500, -300 ), new Vector2( 500, 300 ) );

	public NodeCardUI CreateNewNode( INodeType type, Vector2 position )
	{
		return CreateNewNode( type, position.SnapToGrid( GridSize ), node =>
			node.Position = position.SnapToGrid( GridSize ) );
	}

	public NodeCardUI CreateNewNode( INodeType type, Vector2 position, Action<IGraphNode> onCreated = null )
	{
		if ( type == null )
			return null;

		SGPLogger.Info( $"TODO : GraphViewPanel.CreateNewNode()" );

		var typeDescription = type.CreateNodeTest();

		var nodeCard = new NodeCardUI( typeDescription, position.SnapToGrid( GridSize ) );

		Content.AddChild( nodeCard );
		_cards.Add( nodeCard );

		return nodeCard;
	}

	/// <summary>
	/// Round a canvas position to the nearest grid intersection.
	/// </summary>
	public static Vector2 SnapToGrid( Vector2 position )
	{
		return position.SnapToGrid( GridSize );
	}

	public override void Tick()
	{
		base.Tick();
		_hasDropped = false;

		if ( _dragLeavePending )
		{
			ClearDropPreview();
		}

		if ( _dropPreview != null )
		{
			OnDragHover();
		}
	}

	protected virtual INodeType NodeTypeFromPanelEvent( PanelEvent ev )
	{
		return null;
	}

	protected override void OnDragEnter( PanelEvent e )
	{
		base.OnDragEnter( e );

		if ( e.Target is TreeRow row && row.UserData is DisplayInfo info )
		{
			_dragLeavePending = false;

			// Also fires when the cursor moves onto a placed card (bubbled), so reuse the existing preview.
			if ( _dropPreview == null )
			{
				var position = ScreenToCanvas( MousePosition ).SnapToGrid( GridSize );

				_dropPreview = Content.AddChild( new NodeCardUI( EditorTypeLibrary.GetType( info.Fullname ), position, true ) );
			}
		}

		SGPLogger.Info( "OnDragEnter()" );
	}

	protected virtual void OnDragHover()
	{
		if ( _hasDropped )
		{
			return;
		}

		var position = ScreenToCanvas( MousePosition ).SnapToGrid( GridSize );
	
		_dropPreview.MoveTo( position );
	}

	protected override void OnDragLeave( PanelEvent e )
	{
		base.OnDragLeave( e );

		if ( _hasDropped )
		{
			return;
		}

		if ( _dropPreview != null )
		{
			_dragLeavePending = true;
		}

		SGPLogger.Info( "OnDragLeave()" );
	}

	protected override void OnDrop( PanelEvent e )
	{
		base.OnDrop( e );

		if ( _hasDropped )
		{
			return;
		}

		Focus();

		_hasDropped = true;

		if ( _dropPreview != null )
		{

			ClearDropPreview();
		}
		
		if ( NodeTypeFromPanelEvent( e ) is not { } type )
		{
			return;
		}
		
		var position = ScreenToCanvas( MousePosition ).SnapToGrid( GridSize );
		CreateNewNode( type, position );

		e.StopPropagation();

		OnDragDropFinish();

		SGPLogger.Info( "OnDrop()" );
	}

	/// <summary>
	/// Called when <see cref="OnDrop( PanelEvent )"/> finishes.
	/// </summary>
	protected virtual void OnDragDropFinish()
	{

	}

	protected void ClearDropPreview()
	{
		_dropPreview?.Delete( true );
		_dropPreview = null;
		_dragLeavePending = false;
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
			DrawGridLines( painter, min, max, step, major, MinorColor );
		}

		DrawGridLines( painter, min, max, major, 0, MajorColor );

		var lineWidth = 4.0f;
		var lineWidthScale = 1.0f;

		painter.Stroke = Stroke.Solid( Theme.Blue, lineWidth * lineWidthScale );

		for ( int i = 0; i < _cards.Count - 1; i++ )
		{
			var nodeCardA = _cards[i];
			var nodeCardB = _cards[i + 1];
			
			painter.Line( CanvasToScreen( Position( nodeCardA ) + new Vector2( nodeCardA.Box.Rect.Width, nodeCardA.Box.Rect.Height / 2 ) / ScaleToScreen ),
				CanvasToScreen( Position( nodeCardB ) + new Vector2( 0, nodeCardB.Box.Rect.Height / 2 ) / ScaleToScreen ) );
		}
	}
	
	Vector2 Position( Panel panel ) => (panel.Box.Rect.Position - Content.Box.Rect.Position) / ScaleToScreen;
	
	/// <summary>
	/// Vertical and horizontal lines every <paramref name="step"/> units, skipping multiples of
	/// <paramref name="skipEvery"/> so minor lines never overlap the major ones and double their alpha.
	/// </summary>
	private void DrawGridLines( Painter painter, Vector2 min, Vector2 max, float step, float skipEvery, Color color )
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

	public void RebuildFromGraph()
	{
		OnClear();

		DeleteAllNodeCards();

		BuildFromNodes( false );
		
		OnRebuild();
	}

	public void BuildFromNodes( bool insert, Vector2 offset = default, bool selectNew = false )
	{
		if ( !insert )
		{
			var removed = _cards
				.OfType<NodeCardUI>()
				.ToArray();
		
			foreach ( var cardUi in removed )
			{
				cardUi.Delete();
			}
		}

		//UpdateConnections( nodesSet, selectNew );
		UpdateConnections();
	}

	//	public void UpdateConnections( IEnumerable<IGraphNode> nodes, bool selectNew = false )
	public void UpdateConnections()
	{
		SGPLogger.Info( $"TODO : GraphViewPanel.UpdateConnections()" );
	}

	private void DeleteAllNodeCards()
	{
		NodeCardUI[] array = _cards.ToArray();
		for ( int i = 0; i < array.Length; i++ )
		{
			array[i].Delete();
		}

		_cards.Clear();
	}

	protected virtual void OnClear()
	{

	}

	protected virtual void OnRebuild()
	{

	}

	private bool IsHoveringOverView()
	{
		return Viewport.IsInside( MousePosition );
	}

	private static bool IsMultiple( float value, float of )
	{
		var r = MathF.Abs( value % of );
		return r < 0.001f || of - r < 0.001f;
	}
}
