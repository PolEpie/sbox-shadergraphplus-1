using Editor.NodeEditor;
using NodeEditorPlusTest;
using Sandbox.Internal;
using Sandbox.UI;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;


public sealed class ShaderGraphPlusViewPanel : GraphViewPanel
{
	private readonly Dictionary<string, INodeType> AvailableNodes = new( StringComparer.OrdinalIgnoreCase );
	private readonly Dictionary<string, IBlackboardParameterType> AvailableParameters = new( StringComparer.OrdinalIgnoreCase );

	public ShaderGraphPlusViewPanel() : base()
	{
		SGPLogger.Info( $"TODO : ShaderGraphPlusViewPanel Constructor" );
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
