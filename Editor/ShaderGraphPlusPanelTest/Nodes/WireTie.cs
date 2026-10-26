using EngineNodeGraph;

namespace ShaderGraphPlus.Nodes;

public sealed class WireTieNode : ShaderNodePlus
{
	[Hide]
	public override bool HasTitleBar => false;

	public WireTieNode()
	{
		ExpandSize = new Vector2( 0f, 0 );
	}
}


public class WireTiePanel : NodePanel
{
	public const float Diameter = 16f;

	public WireTiePanel( NodeGraphPanel graph, INode node, bool isPreview = false ) : base( graph, node, isPreview )
	{
		//AddClass( "reroute" );


	}

	protected override void Layout()
	{
		Size = new Vector2( 16f, 32f ).SnapToGrid( Graph.GridSize );
		Style.Width = Size.x;
		Style.Height = Size.y;
	}

	public override void OnDraw( Painter painter )
	{
		base.OnDraw( painter );


		var paint = new NodePaint( this, painter ) { HasMouseOver = HasHovered };
		var rect = new Rect( 0f, Size );

		paint.DrawIcon( rect, "menu", 8f );
	}
}
