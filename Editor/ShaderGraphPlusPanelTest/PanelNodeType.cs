using EngineNodeGraph;
using ShaderGraphPlus;

namespace ShaderGraphPlusPanelTest;

/// <summary>
/// Shows a <see cref="ClassNodeType"/>, which the Qt editor's node menu uses, to the panel graph's
/// node menu.
/// </summary>
public sealed class PanelNodeType : EngineNodeGraph.INodeType
{
	public ClassNodeType Inner { get; }

	public string Identifier => Inner.Identifier;

	public bool IsCommon => ((NodeEditorPlus.INodeType)Inner).IsCommon;

	public NodePathElement[] Path => Inner.Path.Select( x => new NodePathElement( x.Name, x.Icon, x.Description, x.Order, x.IsHeading ) ).ToArray();

	public PanelNodeType( ClassNodeType inner )
	{
		Inner = inner;
	}

	public bool TryGetInput( Type valueType, out string name ) => Inner.TryGetInput( valueType, out name );

	public bool TryGetOutput( Type valueType, out string name ) => Inner.TryGetOutput( valueType, out name );

	public EngineNodeGraph.INode CreateNode( IGraph graph ) => Inner.CreateNode( (NodeEditorPlus.INodeGraph)graph ) as EngineNodeGraph.INode;
}
