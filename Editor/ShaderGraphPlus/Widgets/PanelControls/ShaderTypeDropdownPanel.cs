using Editor;
using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlus;

/// <summary>
/// Panel version of <see cref="ShaderTypeDropdownControl"/>: the built-in shader types, then the
/// project's shader templates.
/// </summary>
[CustomEditor( typeof( string ), NamedEditor = ControlWidgetCustomEditors.ShaderTypeDropdownEditor )]
internal sealed class ShaderTypeDropdownPanel : DropdownControlPlus<string>
{
	public override void Rebuild()
	{
		// Same fallback as the widget: a graph with no template and an unknown type gets the first built-in
		if ( Property?.Parent?.Targets?.FirstOrDefault() is ShaderGraphPlus { HasTemplate: false }
			&& !ShaderTemplate.BuiltInTemplateEntries.ContainsKey( Property.GetValue<string>() ?? "" ) )
		{
			Property.SetValue( ShaderTemplate.BuiltInTemplateEntries.Keys.First() );
		}

		base.Rebuild();
	}

	protected override IEnumerable<object> GetDropdownValues()
	{
		return ShaderTemplate.GetShaderTypeChoices().Select( x => (object)new Entry { Value = x.Value, Label = x.Label, Icon = x.Icon } );
	}
}
