using Editor;
using Sandbox.UI;
using EngineNodeGraph;
using ShaderGraphPlus;
using ShaderGraphPlusPanelTest;

namespace NodeEditorPlusTest;

/// <summary>
/// The engine <see cref="NodeGraphPanel"/> plus palette drag and drop: rows dropped here become
/// nodes at the cursor, with a see-through preview while dragging. Also hosts comments: nodes
/// dropped inside one join it, and only its right-click menu takes them out again.
/// </summary>
public class GraphViewPanel : NodeGraphPanel
{
	/// <summary>
	/// See-through card showing where the dragged palette node will land.
	/// </summary>
	private NodePanel _dropPreview;

	/// <summary>
	/// Palette drag enter/leave count. Events bubble up from cards in any order, so the drag only
	/// left the view when this is 0 in <see cref="Tick"/>.
	/// </summary>
	private int _dragDepth;

	public bool HasDropped => _hasDropped;
	private bool _hasDropped;

	public GraphViewPanel() : base()
	{
		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );
		AddClass( "sg-graph" );
	}

	/// <summary>
	/// Create a node of <paramref name="type"/> at <paramref name="position"/> and add it to the graph.
	/// </summary>
	public NodePanel CreateNewNode( INodeType type, Vector2 position )
	{
		if ( type is null || Graph is null )
			return null;

		if ( type.CreateNode( Graph as INodeGraph ) is not INode node )
			return null;

		node.Position = position;

		return AddNode( node );
	}

	/// <summary>
	/// Creates comments, or null when this graph has none.
	/// </summary>
	protected virtual INodeType CommentNodeType { get; }

	protected virtual INodeType RerouteNodeType { get; }

	static readonly Vector2 DefaultCommentSize = new( 300f, 192f );

	protected override NodePanel CreateNodePanel( INode node, bool isPreview = false ) => node is ICommentNode
		? new CommentPanel( this, node, isPreview )
		: new GraphNodePanel( this, node, isPreview );

	public IEnumerable<CommentPanel> Comments => Nodes.OfType<CommentPanel>();

	/// <summary>
	/// The comment holding <paramref name="node"/>, or null.
	/// </summary>
	public CommentPanel FindComment( NodePanel node )
	{
		if ( node is CommentPanel ) return null;

		return Comments.FirstOrDefault( x => x.Comment.Members?.Contains( node.Node.Identifier ) ?? false );
	}

	/// <summary>
	/// Add a comment at <paramref name="position"/> holding <paramref name="members"/>, taking them
	/// out of any comment they were in.
	/// </summary>
	public CommentPanel CreateComment( Vector2 position, IReadOnlyList<NodePanel> members )
	{
		if ( Graph is null || CommentNodeType?.CreateNode( Graph as INodeGraph ) is not INode node || node is not ICommentNode comment )
			return null;

		node.Position = position;
		comment.Size = DefaultCommentSize;
		comment.Members ??= new();

		RemoveFromComment( members );
		comment.Members.AddRange( members.Where( x => x is not CommentPanel ).Select( x => x.Node.Identifier ) );

		var panel = AddNode( node );
		SelectNode( panel );

		return panel as CommentPanel;
	}

	/// <summary>
	/// Put <paramref name="nodes"/> in <paramref name="comment"/>, out of any comment they were in.
	/// </summary>
	public void AddToComment( CommentPanel comment, IEnumerable<NodePanel> nodes )
	{
		var members = comment.Comment.Members ??= new();

		foreach ( var node in nodes.ToArray() )
		{
			if ( node is CommentPanel ) continue;

			FindComment( node )?.Comment.Members.Remove( node.Node.Identifier );
			members.Add( node.Node.Identifier );
		}
	}

	public void RemoveFromComment( IEnumerable<NodePanel> nodes )
	{
		foreach ( var node in nodes.ToArray() )
		{
			FindComment( node )?.Comment.Members.Remove( node.Node.Identifier );
		}
	}

	/// <summary>
	/// A comment only joins a box selection when the box holds all of it, so selecting nodes
	/// inside one doesn't drag the whole comment along.
	/// </summary>
	protected override bool BoxSelects( NodePanel node, Rect box ) => node is CommentPanel
		? box.IsInside( new Rect( node.Position, node.Size ), true )
		: base.BoxSelects( node, box );

	CommentPanel _dropTarget;

	/// <summary>
	/// Selected nodes that would join a comment if dropped in one: not comments, and in none yet.
	/// </summary>
	IEnumerable<NodePanel> FreeSelectedNodes() => SelectedNodes.Where( x => x is not CommentPanel && FindComment( x ) is null );

	/// <summary>
	/// While nodes are dragged, highlight the comment they'd join: the smallest one under the
	/// mouse that isn't moving with them.
	/// </summary>
	internal void UpdateCommentDrop()
	{
		var mouse = ScreenToCanvas( MousePosition );
		var target = FreeSelectedNodes().Any()
			? Comments.Where( x => !x.Selected && new Rect( x.Position, x.Size ).IsInside( mouse ) ).MinBy( x => x.Size.x * x.Size.y )
			: null;

		if ( target == _dropTarget ) return;

		if ( _dropTarget.IsValid() ) _dropTarget.IsDropTarget = false;
		_dropTarget = target;
		if ( target is not null ) target.IsDropTarget = true;
	}

	internal void FinishCommentDrop( bool cancelled )
	{
		var target = _dropTarget;
		_dropTarget = null;

		if ( !target.IsValid() ) return;

		target.IsDropTarget = false;
		if ( !cancelled ) AddToComment( target, FreeSelectedNodes() );
	}

	Sandbox.UI.Menu _menu;

	void OpenMenu( Action<Sandbox.UI.Menu> build )
	{
		_menu?.Delete( true );

		var menu = _menu = new Sandbox.UI.Menu();
		build( menu );

		menu.Closed += _ =>
		{
			if ( _menu == menu ) _menu = null;
			menu.Delete( true );
		};

		menu.Open( this, Sandbox.UI.Popup.PositionMode.UnderMouse );
	}

	void AddCommentOptions( Sandbox.UI.Menu menu, Vector2 position )
	{
		if ( CommentNodeType is null ) return;

		var selected = SelectedNodes.Where( x => x is not CommentPanel ).ToArray();

		menu.AddOption( "Add Comment", "notes", () => CreateComment( position, [] ) );

		if ( selected.Length > 0 )
		{
			menu.AddOption( $"Add Comment for {selected.Length} {(selected.Length == 1 ? "node" : "nodes")}", "notes", () => CreateComment( position, selected ) );
		}
	}

	/// <summary>
	/// Right-clicking a node selects it, then offers to take the selection out of its comment.
	/// </summary>
	internal void OpenNodeMenu( NodePanel node )
	{
		if ( !node.Selected ) SelectNode( node );

		var position = ScreenToCanvas( MousePosition );
		var members = SelectedNodes.Where( x => FindComment( x ) is not null ).ToArray();

		OpenMenu( menu =>
		{
			if ( members.Length > 0 )
			{
				menu.AddOption( "Remove from comment", "playlist_remove", () => RemoveFromComment( members ) );
			}

			AddCommentOptions( menu, position );
		} );
	}

	protected override void OnRightClick( MousePanelEvent e )
	{
		if ( CommentNodeType is null || Graph is null ) return;

		var position = ScreenToCanvas( MousePosition );
		OpenMenu( menu => AddCommentOptions( menu, position ) );
		e.StopPropagation();
	}

	public override void Tick()
	{
		base.Tick();
		_hasDropped = false;

		if ( _dropPreview != null && _dragDepth == 0 )
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

		if ( e.Target is TreeRow { UserData: DisplayInfo } )
		{
			_dragDepth++;

			// Also fires over placed cards, so keep the existing preview
			if ( _dropPreview == null && Graph is INodeGraph graph && NodeTypeFromPanelEvent( e ) is { } type )
			{
				// A throwaway node of the dragged type, never added to the graph
				if ( type.CreateNode( graph ) is INode node )
				{
					node.Position = ScreenToCanvas( MousePosition ).SnapToGrid( GridSize );

					_dropPreview = CreatePreview( node );
				}
			}
		}
	}

	protected virtual void OnDragHover()
	{
		if ( _hasDropped )
		{
			return;
		}

		_dropPreview.MoveTo( ScreenToCanvas( MousePosition ).SnapToGrid( GridSize ) );
	}

	protected override void OnDragLeave( PanelEvent e )
	{
		base.OnDragLeave( e );

		if ( _hasDropped )
		{
			return;
		}

		if ( e.Target is TreeRow { UserData: DisplayInfo } )
		{
			_dragDepth = Math.Max( 0, _dragDepth - 1 );
		}
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

		CreateNewNode( type, ScreenToCanvas( MousePosition ) );

		e.StopPropagation();

		OnDragDropFinish();
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
		_dragDepth = 0;
	}
}
