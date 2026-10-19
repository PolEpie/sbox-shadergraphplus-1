using Editor;
using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlus;

/// <summary>
/// Panel version of <see cref="PortTypeControlWidget"/>.
/// </summary>
[CustomEditor( typeof( string ), NamedEditor = ControlWidgetCustomEditors.PortTypeChoiceEditor )]
sealed class PortTypeControl : DropdownControl<string>
{
	public override void Rebuild()
	{
		if ( Property is not null && Property.GetValue<string>() is null )
			Property.SetValue( "float" );

		base.Rebuild();
	}

	protected override IEnumerable<object> GetDropdownValues()
	{
		return PortTypeControlWidget.PortTypes;
	}
}
