using EngineNodeGraph;
using NodeEditorPlus;
using NodeEditorPlusTest;
using Sandbox.Internal;
using Sandbox.UI;
using ShaderGraphPlus;
using ShaderGraphPlus.Nodes;

namespace ShaderGraphPlusPanelTest;

public sealed partial class ShaderGraphPlusViewPanel
{
	private readonly BlackboardPanel _blackboard;
	private readonly Dictionary<string, IBlackboardParameterType> AvailableParameters = new( StringComparer.OrdinalIgnoreCase );

	public void AddParameterType<T>() where T : BlackboardParameter
	{
		AddParameterType( EditorTypeLibrary.GetType<T>() );
	}

	public void AddParameterType( TypeDescription type )
	{
		var parameterType = ClassBlackboardParameterType.HookupParameterType( type );

		AvailableParameters.TryAdd( parameterType.Identifier, parameterType );
	}
	public IBlackboardParameterType FindParameterType( Type type )
	{
		return AvailableParameters.TryGetValue( type.FullName!, out var parameterType ) ? parameterType : null;
	}

	private IBlackboardParameter CreateNewParameter( IBlackboardParameterType type, string name = "" )
	{
		return _blackboard.CreateNewParameter( type, name );
	}

	private T CreatenNewParameter<T>( ShaderGraphPlus.ShaderGraphPlus graph ) where T : IBlackboardParameter
	{
		return (T)_blackboard.CreateNewParameter( FindParameterType( typeof( T ) ) );
	}

	private BaseNodePlus CreateNewParameterNode( IBlackboardParameter parameter, Vector2 position )
	{
		var node = parameter.ToNode() as BaseNodePlus;
		node.Graph = Graph;
		node.Position = position.SnapToGrid( GridSize );

		AddNode( node );

		_blackboard.RebuildFromGraph();

		return node;
	}

}
