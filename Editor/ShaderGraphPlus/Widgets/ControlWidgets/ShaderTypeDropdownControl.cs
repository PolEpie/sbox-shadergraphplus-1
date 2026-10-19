using Editor;
using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlus;

[CustomEditor( typeof( string ), NamedEditor = ControlWidgetCustomEditors.ShaderTypeDropdownEditor )]
internal sealed class ShaderTypeDropdownControl : DropdownControlWidget<string>
{
	private ShaderGraphPlus _graph;
	private Entry _currentEntry;

	public ShaderTypeDropdownControl( SerializedProperty property ) : base( property )
	{
		_graph = property.Parent?.Targets?.FirstOrDefault() as ShaderGraphPlus;

		var value = property.GetValue<string>();
		var template = AssetSystem.All.FirstOrDefault( x => x.Path == value );

		var label = "";
		var icon = "";

		if ( _graph.HasTemplate && _graph.ShaderTypeInfo.IsValid )
		{
			label = _graph.ShaderTypeInfo.Title;
			icon = _graph.ShaderTypeInfo.Icon;
		}
		else if ( _graph.HasTemplate && !_graph.ShaderTypeInfo.IsValid )
		{
			label = template.Name;
		}
		else if ( !_graph.HasTemplate && ShaderTemplate.BuiltInTemplateEntries.TryGetValue( value, out var builtInEntry ) )
		{
			label = builtInEntry.Name;
			icon = builtInEntry.Icon;
		}
		else if ( !_graph.HasTemplate && !ShaderTemplate.BuiltInTemplateEntries.ContainsKey( value ) )
		{
			var defaultEntry = ShaderTemplate.BuiltInTemplateEntries.FirstOrDefault();

			label = defaultEntry.Value.Name;
			icon = defaultEntry.Value.Icon;

			property.SetValue( defaultEntry.Key );
		}

		_currentEntry = new Entry()
		{
			Label = label,
			Icon = icon,
		};
	}

	protected override string GetDisplayText()
	{
		var value = SerializedProperty.GetValue<string>();

		if ( string.IsNullOrEmpty( value ) )
		{
			return ShaderTemplate.BuiltInTemplateEntries.FirstOrDefault().Key;
		}

		if ( _graph.HasTemplate && _graph.ShaderTypeInfo.IsValid )
		{
			return _graph.ShaderTypeInfo.Title;
		}
		else
		{
			return value;
		}
	}

	protected override void OnItemSelected( object item )
	{
		if ( item is Entry entry )
		{
			_currentEntry = entry;
		}

		base.OnItemSelected( item );
	}

	protected override IEnumerable<object> GetDropdownValues()
	{
		return ShaderTemplate.GetShaderTypeChoices().Select( x => (object)new Entry { Value = x.Value, Label = x.Label, Icon = x.Icon } );
	}

	protected override void PaintControl()
	{
		var color = IsControlHovered ? Theme.Blue : Theme.TextControl;
		var rect = new Rect( 0, 0, Width, Theme.RowHeight ).Shrink( 8, 0 );

		Paint.SetPen( color );
		Paint.SetDefaultFont();

		if ( !string.IsNullOrEmpty( _currentEntry.Icon ) )
		{
			var i = Paint.DrawIcon( rect, _currentEntry.Icon, 16, TextFlag.LeftCenter );
			rect.Left += i.Width + 8;
		}

		if ( SerializedProperty.IsMultipleDifferentValues )
		{
			Paint.SetPen( Theme.MultipleValues );
			Paint.DrawText( rect, "Multiple Values", TextFlag.LeftCenter );
		}
		else
		{
			Paint.DrawText( rect, GetDisplayText(), TextFlag.LeftCenter );
		}

		Paint.SetPen( color );
		Paint.DrawIcon( rect, "Arrow_Drop_Down", 17, TextFlag.RightCenter );
	}

	protected override void OnPaint()
	{
		base.OnPaint();
	}
}
