using Sandbox.UI;
using EngineNodeGraph;
using ShaderGraphPlus;
using ShaderGraphPlusPanelTest;

namespace NodeEditorPlusTest;

public class GraphViewPanel : NodeGraphPanel
{

	public new INodeGraph Graph
	{
		get => (INodeGraph)base.Graph;
		set
		{
			if ( base.Graph == value ) return;

			base.Graph = (IGraph)value;
		}
	}

	/// <summary>
	/// Palette drag enter/leave count. Events bubble up from cards in any order, so the drag only
	/// left the view when this is 0 in <see cref="Tick"/>.
	/// </summary>
	private int _dragDepth;
	private bool _hasDropped;
	private NodePanel _dropPreview;

	/// <summary>
	/// The comment node to create from the right-click menu. Null for a graph without comments.
	/// </summary>
	protected virtual INodeType CommentNodeType { get; }
	protected virtual INodeType RerouteNodeType { get; }

	public GraphViewPanel() : base()
	{
		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );
		AddClass( "sg-graph" );

		// Derived initializers have run, so an overridden CommentNodeType is already set
		if ( CommentNodeType is not null ) CommentFactory = CreateCommentNode;
	}

	EngineNodeGraph.ICommentNode CreateCommentNode() => CommentNodeType.CreateNode( Graph ) as EngineNodeGraph.ICommentNode;

	protected virtual IEnumerable<INodeType> GetRelevantNodes( NodeQuery query )
	{
		return Enumerable.Empty<INodeType>();
	}

	public NodePanel CreateNewNode( INodeType type, Vector2 position )
	{
		if ( type is null || Graph is null )
			return null;

		var node = type.CreateNode( Graph );

		if ( node is null )
			return null;

		node.Position = position;

		return AddNode( (INode)node );
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
