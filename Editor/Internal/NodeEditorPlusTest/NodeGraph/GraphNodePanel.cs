using Sandbox.UI;
using EngineNodeGraph;

namespace NodeEditorPlusTest;

/// <summary>
/// A node in a <see cref="GraphViewPanel"/>. Dropping it inside a comment puts it in that comment;
/// right-clicking it opens the graph's node menu.
/// </summary>
public class GraphNodePanel : NodePanel
{
	public GraphViewPanel View => (GraphViewPanel)Graph;

	public GraphNodePanel( GraphViewPanel graph, INode node, bool isPreview = false ) : base( graph, node, isPreview )
	{
	}

	protected override void OnDragMove( Vector2 delta )
	{
		base.OnDragMove( delta );
		View.UpdateCommentDrop();
	}

	protected override void OnDragFinish( bool cancelled )
	{
		base.OnDragFinish( cancelled );
		View.FinishCommentDrop( cancelled );
	}

	protected override void OnRightClick( MousePanelEvent e )
	{
		if ( IsPreview ) return;

		View.OpenNodeMenu( this );
		e.StopPropagation();
	}
}
