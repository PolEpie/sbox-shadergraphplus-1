using EngineControlSheet;
using Editor;
using ShaderGraphPlus.Nodes;
using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlus;

/// <summary>
/// Panel version of <see cref="NamedRerouteReferenceControlWidget"/>: "None", or one of the graph's
/// named reroute declarations.
/// </summary>
[CustomEditor( typeof( string ), NamedEditor = ControlWidgetCustomEditors.NamedRerouteReferenceEditor )]
internal sealed class NamedRerouteReferenceControl : DropdownControl<string>
{
	ShaderGraphPlus Graph => Property?.Parent?.Targets?.OfType<NamedRerouteNode>().FirstOrDefault()?.Graph as ShaderGraphPlus;

	public override void Rebuild()
	{
		if ( Graph is not null && string.IsNullOrWhiteSpace( Property.GetValue<string>() ) )
			Property.SetValue( "None" );

		base.Rebuild();
	}

	protected override IEnumerable<object> GetDropdownValues()
	{
		yield return "None";

		var graph = Graph;
		if ( graph is null ) yield break;

		foreach ( var declaration in graph.Nodes.OfType<NamedRerouteDeclarationNode>() )
		{
			yield return new Entry { Value = declaration.Name, Label = declaration.Name };
		}
	}
}
