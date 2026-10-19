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

	}

	protected virtual IEnumerable<INodeType> GetRelevantNodes( NodeQuery query )
	{
		return Enumerable.Empty<INodeType>();
	}

	/// <summary>
	/// The editor can read the clipboard, so empty space offers to paste.
	/// </summary>
	protected override string ReadClipboard() => Editor.EditorUtility.Clipboard.Paste();


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

	/// <summary>
	/// Add a node of <paramref name="type"/> at <paramref name="position"/> and select it. With
	/// <paramref name="plug"/>, wire the new node's matching plug to it, like the Qt editor.
	/// </summary>
	public NodePanel CreateNewNode( INodeType type, Vector2 position, PlugPanel plug )
	{
		if ( CreateNewNode( type, position ) is not { } panel ) return null;

		SelectNode( panel );

		switch ( plug )
		{
			case PlugInPanel input when type.TryGetOutput( input.Inner.Type, out var name ) && panel.Outputs.FirstOrDefault( x => x.Inner.Identifier == name ) is { } output:
				Connect( output, input );
				break;

			case PlugOutPanel output when type.TryGetInput( output.Inner.Type, out var name ) && panel.Inputs.FirstOrDefault( x => x.Inner.Identifier == name ) is { } input:
				Connect( output, input );
				break;
		}

		return panel;
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
		return Graph is not null && NodeTypeFromPanelEvent( e ) is { } type ? type.CreateNode( Graph ) as INode : null;
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
}
