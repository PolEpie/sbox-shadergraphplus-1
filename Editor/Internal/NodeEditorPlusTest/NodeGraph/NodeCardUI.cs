using Sandbox.UI;
using Sandbox.UI.Construct;

namespace NodeEditorPlusTest;

/// <summary>
/// A shader node placed on the graph canvas. Lives in <see cref="CanvasPanel.Content"/>, so it pans
/// and zooms with the grid. Drag the header to move it; it follows the cursor freely and snaps to the grid on release.
/// </summary>
public class NodeCardUI : Panel
{
	/// <summary>
	/// The <see cref="ShaderGraphPlus.ShaderNodePlus"/> type this card stands for.
	/// </summary>
	public TypeDescription Type { get; }

	/// <summary>
	/// Top left of the card, in canvas coordinates.
	/// </summary>
	public Vector2 Position => _header.Position;

	private readonly Header _header;

	public GraphViewPanel Graph { get; private set; }

	public NodeCardUI( GraphViewPanel graph )
	{
		Graph = graph;
	}

	/// <summary>
	/// A see-through stand-in shown while a palette row is dragged over the graph. It ignores the
	/// mouse so it never becomes the drop target or starts a card drag.
	/// </summary>
	public NodeCardUI( GraphViewPanel graph, TypeDescription type, Vector2 position, bool isPreview = false )
	{
		Graph = graph;
		Type = type;
		AddClass( "sg-node" );

		var info = DisplayInfo.ForType( type.TargetType );

		_header = AddChild( new Header( this ) );
		_header.AddClass( "header" );
		_header.Add.Icon( info.Icon ?? "category", "icon" );
		_header.Add.Label( info.Name, "title" );

		if ( isPreview )
		{
			AddClass( "preview" );
			Style.PointerEvents = PointerEvents.None;

			// Handle forces PointerEvents.All inline on itself, so override it there too
			_header.Style.PointerEvents = PointerEvents.None;
			_header.DragEnabled = false;
		}
		else
		{
			Tooltip = info.Description;
		}

		MoveTo( position );
	}

	/// <summary>
	/// No tooltip while the card is being dragged: the editor's tooltip window stays where it
	/// opened, so it would hang behind the moving card.
	/// </summary>
	public override bool HasTooltip => _header is not { IsDragging: true } && base.HasTooltip;

	/// <summary>
	/// Put the card's top left at <paramref name="position"/>, in canvas coordinates.
	/// </summary>
	public void MoveTo( Vector2 position )
	{
		_header.Position = position;
		Style.Left = position.x;
		Style.Top = position.y;
	}

	/// <summary>
	/// Drags the whole card. The handle's own Position mirrors the card's so Escape-cancel restores it.
	/// No <see cref="CanvasPanel.Handle.SnapIncrement"/>: snapping every move makes the card jump a whole
	/// grid cell at a time (and more when zoomed in), so it trails the cursor. Snap once on release instead.
	/// </summary>
	private sealed class Header : CanvasPanel.Handle
	{
		private readonly NodeCardUI _card;

		public Header( NodeCardUI card )
		{
			_card = card;
			Positioned = false;
		}

		protected override void OnDragMove( Vector2 delta )
		{
			var position = DragOrigin + delta;
			_card.MoveTo( position.SnapToGrid( _card.Graph.GridSize ) );
		}

		protected override void OnDragFinish( bool cancelled )
		{
			_card.MoveTo( cancelled ? DragOrigin : Position.SnapToGrid( _card.Graph.GridSize ) );
		}
	}
}
