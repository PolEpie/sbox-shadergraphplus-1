using EngineNodeGraph;
using NodeEditorPlus;
using NodeEditorPlusTest;
using Sandbox.Internal;
using Sandbox.UI;
using ShaderGraphPlus;
using ShaderGraphPlus.Nodes;

namespace ShaderGraphPlusPanelTest;

public sealed partial class ShaderGraphPlusViewPanel : GraphViewPanel
{
	/// <summary>
	/// Most results a filtered node list shows, like the Qt editor's.
	/// </summary>
	const int MaxFilteredResults = 20;


	public new ShaderGraphPlus.ShaderGraphPlus Graph
	{
		get => (ShaderGraphPlus.ShaderGraphPlus)base.Graph;
		set
		{
			if ( base.Graph == value ) return;

			base.Graph = value;
		}
	}

	private readonly Dictionary<string, PanelNodeType> AvailableNodes = new( StringComparer.OrdinalIgnoreCase );


	protected override EngineNodeGraph.INodeType CommentNodeType { get; } = new PanelNodeType( new ClassNodeType( EditorTypeLibrary.GetType<CommentNode>() ) );

	protected override EngineNodeGraph.INodeType RerouteNodeType { get; } = new PanelNodeType( new ClassNodeType( EditorTypeLibrary.GetType<ReroutePlus>() ) );

	/// <summary>
	/// Same as the Qt editor's, so nodes copied in one paste into the other.
	/// </summary>
	protected override string ClipboardIdent => "shadergraphplus";

	/// <summary>
	/// Parameter nodes stay with their blackboard parameter, like the Qt editor's copy.
	/// </summary>
	protected override bool CanCopy( EngineNodeGraph.NodePanel node ) => base.CanCopy( node ) && node.Node is not IBlackboardNode;

	/// <summary>
	/// Blackboard parameters drag in as nodes too.
	/// </summary>
	protected override bool AcceptsDrag( Panel source ) => source is ParameterRow || base.AcceptsDrag( source );

	protected override EngineNodeGraph.INode CreateDroppedNode( PanelEvent e )
	{
		if ( e.Target is not ParameterRow row ) return base.CreateDroppedNode( e );

		var node = (BaseNodePlus)row.Parameter.ToNode();
		node.Graph = Graph;
		return node;
	}

	public ShaderGraphPlusViewPanel( BlackboardPanel blackboard ) : base()
	{
		SGPLogger.Info( $"TODO : ShaderGraphPlusViewPanel Constructor" );

		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );
		AddClass( "sg-graph" );


		_blackboard = blackboard;
		_blackboard.OnSelected += OnParameterSelected;
		SelectionChanged += OnGraphSelectionChanged;

	}

	bool _pickingParameter;

	/// <summary>
	/// Picking a parameter takes the selection from the graph, so the next click there - on a node
	/// or empty space - changes it and ends the highlight.
	/// </summary>
	void OnParameterSelected( BlackboardParameter parameter )
	{
		if ( parameter is null ) return;

		_pickingParameter = true;
		ClearSelection();
		_pickingParameter = false;
	}

	void OnGraphSelectionChanged()
	{
		if ( !_pickingParameter ) _blackboard?.Select( null );
	}

	/// <summary>
	/// Each input <paramref name="node"/> feeds, with the output feeding it.
	/// </summary>
	private Dictionary<IPlugIn, IPlugOut> ConnectedFrom( BaseNodePlus node )
	{
		var connected = new Dictionary<IPlugIn, IPlugOut>();

		foreach ( var other in Graph.Nodes )
		{
			foreach ( var input in other.Inputs )
			{
				if ( input.ConnectedOutput?.Node == node ) connected[input] = input.ConnectedOutput;
			}
		}

		return connected;
	}

	/// <summary>
	/// Nodes refresh their plugs in OnFrame, which the Qt view calls every frame too. Nodes reading
	/// the parameter picked in the blackboard are highlighted, so they're easy to spot.
	/// </summary>
	public override void Tick()
	{
		base.Tick();

		var parameter = _blackboard?.Selected?.Identifier;

		foreach ( var node in Nodes )
		{
			if ( node.Node is BaseNodePlus baseNode )
			{
				baseNode.OnFrame();
			}

			node.Highlighted = parameter is { } id && node.Node is IBlackboardNode blackboardNode && blackboardNode.ParameterIdentifier == id;
		}
	}

	public void AddNodeType<T>() where T : BaseNodePlus
	{
		AddNodeType( EditorTypeLibrary.GetType<T>() );
	}

	public void AddNodeType( TypeDescription type )
	{
		var nodeType = new PanelNodeType( new ClassNodeType( type ) );

		AvailableNodes.TryAdd( nodeType.Identifier, nodeType );
	}

	public void AddNodeType( string subgraphPath )
	{
		var subgraphTxt = Editor.FileSystem.Content.ReadAllText( subgraphPath );
		var subgraph = JsonSerializer.Deserialize<ShaderGraphPlus.ShaderGraphPlus>( subgraphTxt, ShaderGraphPlus.ShaderGraphPlus.SerializerOptions() );
		if ( !subgraph.AddToNodeLibrary ) return;
		var nodeType = new SubgraphNodeType( subgraphPath, EditorTypeLibrary.GetType<SubgraphNode>() );
		nodeType.SetDisplayInfo( subgraph );
		AvailableNodes.TryAdd( nodeType.Identifier, new PanelNodeType( nodeType ) );
	}

	public EngineNodeGraph.INodeType FindNodeType( Type type )
	{
		return AvailableNodes.TryGetValue( type.FullName!, out var nodeType ) ? nodeType : null;
	}

	protected override IEnumerable<EngineNodeGraph.INodeType> GetRelevantNodes( EngineNodeGraph.NodeQuery query )
	{
		return AvailableNodes.Values.Filter( query ).Where( x =>
		{
			if ( x is PanelNodeType { Inner: var classNodeType } )
			{
				var targetType = classNodeType.Type.TargetType;
				if ( classNodeType.Type.HasAttribute<HideAttribute>() ) return false;
				if ( classNodeType.Type.HasAttribute<InternalNodeAttribute>() ) return false;
				if ( Graph.IsSubgraph && targetType == typeof( Result ) ) return false;
				if ( targetType == typeof( SubgraphNode ) && classNodeType.DisplayInfo.Name == targetType.Name.ToTitleCase() ) return false;
			}
			return true;
		} );
	}

	protected override EngineNodeGraph.NodeHandleConfig OnGetHandleConfig( Type type )
	{
		if ( ShaderGraphPlusTheme.NodeHandleConfigs.TryGetValue( type, out var config ) )
		{
			var shape = Enum.TryParse<EngineNodeGraph.HandleShape>( config.Shape.ToString(), out var mapped )
				? mapped
				: EngineNodeGraph.HandleShape.Square;

			return new EngineNodeGraph.NodeHandleConfig( config.Name, config.Color, shape );
		}

		return base.OnGetHandleConfig( type );
	}

	protected override EngineNodeGraph.INodeType NodeTypeFromPanelEvent( PanelEvent ev )
	{
		if ( ev.Target is TreeRow row && row.UserData is DisplayInfo info )
		{
			return AvailableNodes.TryGetValue( info.Fullname, out var type )
				? type
				: null;
		}

		return null;
	}
}
