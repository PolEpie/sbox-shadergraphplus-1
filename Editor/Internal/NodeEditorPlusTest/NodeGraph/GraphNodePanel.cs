using Sandbox.UI;
using EngineNodeGraph;

namespace NodeEditorPlusTest;

/// <summary>
/// A node in a <see cref="GraphViewPanel"/>. Dropping it inside a comment puts it in that comment;
/// right-clicking it opens the graph's node menu.
/// </summary>
public class GraphNodePanel : NodePanel
{
	public new GraphViewPanel Graph => (GraphViewPanel)base.Graph;

	public GraphNodePanel( GraphViewPanel graph, INode node, bool isPreview = false ) : base( graph, node, isPreview )
	{
	}

	protected override void OnDragMove( Vector2 delta )
	{
		base.OnDragMove( delta );
		Graph.UpdateCommentDrop();
	}

	protected override void OnDragFinish( bool cancelled )
	{
		base.OnDragFinish( cancelled );
		Graph.FinishCommentDrop( cancelled );
	}

	protected override void OnRightClick( MousePanelEvent e )
	{
		if ( IsPreview ) return;

		Graph.OpenNodeMenu( this );
		e.StopPropagation();
	}
}
