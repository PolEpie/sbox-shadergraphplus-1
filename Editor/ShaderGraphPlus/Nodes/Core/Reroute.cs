using Editor;

namespace ShaderGraphPlus.Nodes;

/// <summary>
/// A reroute for both editors: <see cref="IRerouteNode"/> is the Qt graph's, <see cref="EngineNodeGraph.IRerouteNode"/>
/// the panel graph's, which shows it as a dot. Their members line up, so <see cref="Comment"/> serves both.
/// </summary>
public abstract class RerouteNode : BaseNodePlus, IRerouteNode, EngineNodeGraph.IRerouteNode
{
	/// <summary>
	/// Comment to show above this node
	/// </summary>
	public string Comment { get; set; }

	[Input, Hide, Title( "" )]
	public NodeInput Input { get; set; }

	[Hide, JsonIgnore]
	public override bool CanPreview => false;

	public override NodeUI CreateUI( GraphView view )
	{
		return new RerouteUI( view, this );
	}
}

public sealed class ReroutePlus : RerouteNode
{
	[Output, Hide, Title( "" )]
	public NodeResult.Func Result => ( GraphCompiler compiler ) =>
	{
		var result = compiler.ResultOrDefault( Input, 0.0f );
		result.Constant = true;
		return result;
	};
}
