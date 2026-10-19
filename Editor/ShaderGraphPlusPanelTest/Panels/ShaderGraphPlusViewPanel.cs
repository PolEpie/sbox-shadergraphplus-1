using Editor.NodeEditor;
using NodeEditorPlus;
using NodeEditorPlusTest;
using Sandbox.Internal;
using Sandbox.UI;
using ShaderGraphPlus;
using ShaderGraphPlus.Nodes;

namespace ShaderGraphPlusPanelTest;


public sealed class ShaderGraphPlusViewPanel : GraphViewPanel
{
	public new ShaderGraphPlus.ShaderGraphPlus Graph
	{
		get => (ShaderGraphPlus.ShaderGraphPlus)base.Graph;
		set
		{
			if ( base.Graph == value ) return;

			base.Graph = value;
		}
	}

	private readonly Dictionary<string, INodeType> AvailableNodes = new( StringComparer.OrdinalIgnoreCase );
	private readonly Dictionary<string, IBlackboardParameterType> AvailableParameters = new( StringComparer.OrdinalIgnoreCase );

	protected override INodeType CommentNodeType { get; } = new ClassNodeType( EditorTypeLibrary.GetType<CommentNode>() );

	protected override INodeType RerouteNodeType { get; } = new ClassNodeType( EditorTypeLibrary.GetType<ReroutePlus>() );

	/// <summary>
	/// Same as the Qt editor's, so nodes copied in one paste into the other.
	/// </summary>
	protected override string ClipboardIdent => "shadergraphplus";

	/// <summary>
	/// Parameter nodes stay with their blackboard parameter, like the Qt editor's copy.
	/// </summary>
	protected override bool CanCopy( EngineNodeGraph.NodePanel node ) => base.CanCopy( node ) && node.Node is not IBlackboardNode;

	/// <summary>
	/// Where converted constants get their parameters. Set by the window.
	/// </summary>
	public BlackboardPanel Blackboard { get; set; }

	public ShaderGraphPlusViewPanel() : base()
	{
		SGPLogger.Info( $"TODO : ShaderGraphPlusViewPanel Constructor" );
	}

	protected override void BuildContextMenu( Sandbox.UI.Menu menu, EngineNodeGraph.NodePanel node, Vector2 position )
	{
		base.BuildContextMenu( menu, node, position );

		if ( node is null || Blackboard is null || Graph is null ) return;

		var selected = SelectedNodes.Select( x => x.Node ).ToArray();
		var constants = selected.OfType<IConstantNode>().Where( x => x is not IConstantMatrixNode ).ToArray();
		if ( constants.Length == 0 || constants.Length != selected.Length ) return;

		var target = Graph.IsSubgraph ? "Subgraph Input" : "Material Parameter";
		var name = constants.Length == 1 ? $"Convert Constant to {target}" : $"Convert {constants.Length} Constants to {target}s";

		if ( menu.HasOptions ) menu.AddSeparator();
		menu.AddOption( name, "swap_horiz", () => ConvertConstants( constants ) );
	}

	void ConvertConstants( IReadOnlyList<IConstantNode> constants )
	{
		BaseNodePlus last = null;

		foreach ( var constant in constants )
		{
			last = ConvertConstantToParameter( constant );
		}

		RebuildFromGraph();
		Blackboard.RebuildFromGraph();

		if ( last is not null && FindNode( last ) is { } panel ) SelectNode( panel );
	}

	/// <summary>
	/// Swap <paramref name="constant"/> for a node of a new parameter holding its value, keeping its
	/// connections. Returns the new node, or the constant when there's no parameter type for it.
	/// </summary>
	BaseNodePlus ConvertConstantToParameter( IConstantNode constant )
	{
		var baseNode = (BaseNodePlus)constant;
		var position = baseNode.Position;
		var connected = Graph.IsSubgraph ? new Dictionary<IPlugIn, IPlugOut>() : ConnectedFrom( baseNode );

		var parameterTypeName = Graph.IsSubgraph
			? constant switch
			{
				BoolConstantNode => DisplayInfo.ForType( typeof( BoolSubgraphInputParameter ) ).Fullname,
				IntConstantNode => DisplayInfo.ForType( typeof( IntSubgraphInputParameter ) ).Fullname,
				FloatConstantNode => DisplayInfo.ForType( typeof( FloatSubgraphInputParameter ) ).Fullname,
				Float2ConstantNode => DisplayInfo.ForType( typeof( Float2SubgraphInputParameter ) ).Fullname,
				Float3ConstantNode => DisplayInfo.ForType( typeof( Float3SubgraphInputParameter ) ).Fullname,
				Float4ConstantNode => DisplayInfo.ForType( typeof( Float4SubgraphInputParameter ) ).Fullname,
				ColorConstantNode => DisplayInfo.ForType( typeof( ColorSubgraphInputParameter ) ).Fullname,
				_ => null,
			}
			: constant switch
			{
				BoolConstantNode => DisplayInfo.ForType( typeof( BoolParameter ) ).Fullname,
				IntConstantNode => DisplayInfo.ForType( typeof( IntParameter ) ).Fullname,
				FloatConstantNode => DisplayInfo.ForType( typeof( FloatParameter ) ).Fullname,
				Float2ConstantNode => DisplayInfo.ForType( typeof( Float2Parameter ) ).Fullname,
				Float3ConstantNode => DisplayInfo.ForType( typeof( Float3Parameter ) ).Fullname,
				Float4ConstantNode => DisplayInfo.ForType( typeof( Float4Parameter ) ).Fullname,
				ColorConstantNode => DisplayInfo.ForType( typeof( ColorParameter ) ).Fullname,
				_ => null,
			};

		if ( parameterTypeName is null || !AvailableParameters.TryGetValue( parameterTypeName, out var parameterType ) )
		{
			SGPLogger.Warning( $"No parameter type for {constant.GetType().Name}" );
			return baseNode;
		}

		Graph.RemoveNode( baseNode );

		var parameter = Blackboard.CreateNewParameter( parameterType );
		parameter.SetValue( constant.GetValue() );

		var parameterNode = (BaseNodePlus)parameter.ToNode();
		parameterNode.Graph = Graph;
		parameterNode.Position = position.SnapToGrid( GridSize );
		Graph.AddNode( parameterNode );

		// Whatever the constant fed, the parameter feeds now
		foreach ( var node in Graph.Nodes )
		{
			foreach ( var input in node.Inputs )
			{
				if ( input.ConnectedOutput is null && connected.TryGetValue( input, out var output ) )
				{
					node.ConnectNode( input.Identifier, output.Identifier, parameterNode.Identifier );
				}
			}
		}

		return parameterNode;
	}

	/// <summary>
	/// Each input <paramref name="node"/> feeds, with the output feeding it.
	/// </summary>
	Dictionary<IPlugIn, IPlugOut> ConnectedFrom( BaseNodePlus node )
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
	/// Nodes refresh their plugs in OnFrame, which the Qt view calls every frame too.
	/// </summary>
	public override void Tick()
	{
		base.Tick();

		foreach ( var node in Nodes )
		{
			if ( node.Node is BaseNodePlus baseNode )
			{
				baseNode.OnFrame();
			}
		}
	}

	public void AddNodeType<T>() where T : BaseNodePlus
	{
		AddNodeType( EditorTypeLibrary.GetType<T>() );
	}

	public void AddNodeType( TypeDescription type )
	{
		var nodeType = new ClassNodeType( type );

		AvailableNodes.TryAdd( nodeType.Identifier, nodeType );
	}

	public void AddNodeType( string subgraphPath )
	{
		var subgraphTxt = Editor.FileSystem.Content.ReadAllText( subgraphPath );
		var subgraph = JsonSerializer.Deserialize<ShaderGraphPlus.ShaderGraphPlus>( subgraphTxt, ShaderGraphPlus.ShaderGraphPlus.SerializerOptions() );
		if ( !subgraph.AddToNodeLibrary ) return;
		var nodeType = new SubgraphNodeType( subgraphPath, EditorTypeLibrary.GetType<SubgraphNode>() );
		nodeType.SetDisplayInfo( subgraph );
		AvailableNodes.TryAdd( nodeType.Identifier, nodeType );
	}

	public INodeType FindNodeType( Type type )
	{
		return AvailableNodes.TryGetValue( type.FullName!, out var nodeType ) ? nodeType : null;
	}

	protected override IEnumerable<INodeType> GetRelevantNodes( NodeQuery query )
	{
		return AvailableNodes.Values.Filter( query ).Where( x =>
		{
			if ( x is ClassNodeType classNodeType )
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

	public void AddParameterType<T>() where T : BlackboardParameter
	{
		AddParameterType( EditorTypeLibrary.GetType<T>() );
	}

	public void AddParameterType( TypeDescription type )
	{
		var parameterType = ClassBlackboardParameterType.HookupParameterType( type );

		AvailableParameters.TryAdd( parameterType.Identifier, parameterType );
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

	protected override INodeType NodeTypeFromPanelEvent( PanelEvent ev )
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
