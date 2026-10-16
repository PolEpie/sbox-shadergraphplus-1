using Editor;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;

public sealed class PalettePanel : Panel
{
	private bool _isSubgraph { get; }

	private TextInput _searchFilter;

	private readonly Sandbox.UI.TreeView<Node> _tree;

	public PalettePanel( bool isSubgraph ) : base()
	{
		_isSubgraph = isSubgraph;

		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );

		BuildToolbar();

		_tree = new Sandbox.UI.TreeView<Node>();
		_tree.AddClass( "demo-tree" );
		_tree.GetChildren = treeNode => treeNode.Children;
		_tree.GetParent = treeNode => treeNode.Parent;
		_tree.CanExpand = treeNode => treeNode.CanHaveChildren;
		_tree.GetHeight = treeNode => treeNode is CategoryNode ? 30 : Theme.RowHeight;
		_tree.GetTooltip = treeNode => treeNode.ToolTip;
		_tree.OnRow = OnRow;
		_tree.OnSelect = treeNode => SGPLogger.Info( $"selected {treeNode.Name}" );
		_tree.OnActivate = treeNode => SGPLogger.Info( $"activated {treeNode.Name}" );
		_tree.OnRename = ( treeNode, text ) => { treeNode.Name = text; SGPLogger.Info( $"renamed to {text}" ); };
		_tree.CanDrag = treeNode => treeNode is not CategoryNode;

		AddChild( _tree );

		UpdateTree();
	}

	private void OnRow( Sandbox.UI.TreeRow row, Node node )
	{
		row.Text = node.Name;
		row.IconName = node.Icon;
	}

	private void BuildToolbar()
	{
		var toolbar = AddChild( new Toolbar() );
		toolbar.AddClass( "panetools" );

		_searchFilter = toolbar.AddChild( new TextInput( "Filter Nodes..", "search" ) );
		_searchFilter.OnChange = ( value ) =>
		{
			UpdateTree();
		};
	}

	private IEnumerable<IGrouping<string, (TypeDescription Type, DisplayInfo Info)>> GetItems()
	{
		var types = EditorTypeLibrary.GetTypes<ShaderNodePlus>()
			.Where( x =>
			{
				if ( x.IsAbstract ) return false;
				if ( x.HasAttribute<HideAttribute>() ) return false;
				if ( x.HasAttribute<InternalNodeAttribute>() ) return false;
				if ( _isSubgraph && x.TargetType == typeof( Result ) )
				{
					return false;
				}
				if ( !_isSubgraph && x.HasAttribute<SubgraphOnlyAttribute>() ) return false;
				if ( x.TargetType == typeof( SubgraphNode ) )
				{
					return false;
				}
				return true;
			} )
			.Select( x => (Type: x, Info: DisplayInfo.ForType( x.TargetType )) );

		if ( !string.IsNullOrEmpty( _searchFilter.Value ) )
		{
			types = types.Where( e => e.Info.Name.Contains( _searchFilter.Value, StringComparison.OrdinalIgnoreCase ) );
		}

		return types.GroupBy( x => x.Info.Group?.ToLower() )
			.OrderBy( p => p.Key == null )
			.ThenBy( p => p.Key );
	}

	public void UpdateTree()
	{
		var roots = new List<Node>();
		foreach ( var category in GetItems() )
		{
			var categoryNode = new CategoryNode( category.Key ?? "Uncategorized" );

			foreach ( var (type, info) in category.OrderBy( x => x.Info.Name ) )
			{
				categoryNode.AddItem( new Node( info.Name ) { Icon = info.Icon, Type = type } );
			}

			roots.Add( categoryNode );
		}

		_tree.Roots = roots;
		_tree.Refresh();
	}
}

class CategoryNode : Node
{
	public override bool CanHaveChildren => true;

	public CategoryNode( string name ) : base( name )
	{
		Children = new List<Node>();
	}

	public void AddItem( Node item )
	{
		if ( item is null )
			return;

		if ( !CanHaveChildren )
			return;

		Children.Add( item );
	}
}

class Node
{
	public string Name { get; set; }

	public string Icon { get; set; }

	public string ToolTip { get; set; }

	/// <summary>
	/// The shader node type a leaf row creates when dropped on the graph. Null for categories.
	/// </summary>
	public TypeDescription Type { get; init; }

	public Node Parent { get; set; }

	public Node( string name )
	{
		Name = name;
	}

	public virtual bool CanHaveChildren => false;

	private List<Node> _children;
	public List<Node> Children
	{
		get
		{
			if ( _children is not null ) return _children;

			_children = new List<Node>();
			if ( !CanHaveChildren ) return _children;

			return _children;
		}
		set
		{
			_children = value;
		}
	}

	public bool IsDescendantOf( Node other )
	{
		for ( var p = Parent; p is not null; p = p.Parent )
		{
			if ( p == other ) return true;
		}

		return false;
	}
}
