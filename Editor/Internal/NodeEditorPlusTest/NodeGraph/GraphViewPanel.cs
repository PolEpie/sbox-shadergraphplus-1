using Sandbox.UI;
using EngineNodeGraph;
using ShaderGraphPlus;
using ShaderGraphPlus.Nodes;
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
	private bool _freehandSelectingWires;
	private bool _canOpenContextMenu = true;

	private NodePanel _dropPreview;

	public GraphViewPanel() : base()
	{
	}

	/// <summary>
	/// The editor can read the clipboard, so empty space offers to paste.
	/// </summary>
	protected override string ReadClipboard() => Editor.EditorUtility.Clipboard.Paste();

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

	protected virtual EngineNodeGraph.INodeType NodeTypeFromPanelEvent( PanelEvent ev )
	{
		return null;
	}

	/// <summary>
	/// Whether a drag from <paramref name="source"/> drops nodes here. Palette rows do.
	/// </summary>
	protected virtual bool AcceptsDrag( Panel source ) => source is TreeRow { UserData: DisplayInfo };

	/// <summary>
	/// A new node for what <paramref name="e"/> drags, not in the graph yet: the palette's node type
	/// by default. Null when there's nothing to drop.
	/// </summary>
	protected virtual INode CreateDroppedNode( PanelEvent e )
	{
		return Graph is not null && NodeTypeFromPanelEvent( e ) is { } type ? type.CreateNode( base.Graph ) : null;
	}

	protected override NodePanel CreateNodePanel( INode node, bool isPreview = false )
	{
		if ( node is WireTieNode )
		{
			return new WireTiePanel( this, node, isPreview );
		}

		return base.CreateNodePanel( node, isPreview );
	}

	protected override void OnDragEnter( PanelEvent e )
	{
		base.OnDragEnter( e );

		if ( AcceptsDrag( e.Target ) )
		{
			_dragDepth++;

			// Also fires over placed cards, so keep the existing preview. It's a throwaway node, never added to the graph.
			if ( _dropPreview == null && CreateDroppedNode( e ) is { } node )
			{
				node.Position = ScreenToCanvas( MousePosition ).SnapToGrid( GridSize );

				_dropPreview = CreatePreview( node );
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

		if ( AcceptsDrag( e.Target ) )
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

		if ( CreateDroppedNode( e ) is not { } node )
		{
			return;
		}

		using var undoScope = UndoScope( "Add Node" );

		node.Position = ScreenToCanvas( MousePosition );
		AddNode( node );

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

	protected override void OnMouseUp( MousePanelEvent e )
	{
		if ( e.KeyboardModifiers == KeyboardModifiers.Ctrl && e.Button == "mouseright" )
		{
			_freehandSelectingWires = false;
			_canOpenContextMenu = false;

			SGPLogger.Info( "CTRL + Right Mouse Button Up" );
		}

		base.OnMouseUp( e );
	}

	protected override void OnMouseDown( MousePanelEvent e )
	{
		if ( e.KeyboardModifiers == KeyboardModifiers.Ctrl && e.Button == "mouseright" )
		{
			_freehandSelectingWires = true;
			
			SGPLogger.Info( "CTRL + Right Mouse Button Down" );
		}

		base.OnMouseDown( e );
	}

	protected override void OnMouseMove( MousePanelEvent e )
	{
		if ( _freehandSelectingWires )
		{
			SGPLogger.Info( "DrawingConnectionLine" );
		}

		base.OnMouseMove( e );
	}

	protected override void OpenContextMenu( Vector2 position, NodePanel node, PlugPanel targetPlug = null, Action onClose = null )
	{
		if ( !_canOpenContextMenu )
		{
			_canOpenContextMenu = true;
			return;
		}

		base.OpenContextMenu( position, node, targetPlug, onClose );
	}
}
