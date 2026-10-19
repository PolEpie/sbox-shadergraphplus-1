// Temporary copy of the engine's BaseControl.CreateFor (panel-controlsheet-objects branch). Delete this
// folder and point the Properties panel back at Sandbox.UI.ControlSheet once it ships in the engine.
using Sandbox.UI;

namespace EngineControlSheet;

/// <summary>
/// Layout hints the branch adds to BaseControl. The engine's BaseControl doesn't have them yet, so the
/// controls in this folder implement this instead and <see cref="ControlSheetRow"/> reads it.
/// </summary>
public interface IControlLayout
{
	/// <summary>
	/// Take the whole row instead of the value column.
	/// </summary>
	bool IsWideMode => false;

	/// <summary>
	/// Show the property's name with this control.
	/// </summary>
	bool IncludeLabel => true;
}

/// <summary>
/// Picks the control for a property in a local <see cref="ControlSheet"/>. Same rules as the
/// branch's BaseControl.CreateFor, but it can't change that method, so rows call this instead.
/// </summary>
public static class ControlResolver
{
	/// <summary>
	/// Create a BaseControl for a given SerializedProperty.
	/// We'll look at BaseControls with [CustomEditor] attributes and see if any of them can handle this property.
	/// </summary>
	public static BaseControl CreateFor( SerializedProperty property )
	{
		ArgumentNullException.ThrowIfNull( property, nameof( property ) );

		var type = property.PropertyType;

		// Can't be written - show the value. A get-only class reference still has editable fields.
		if ( !property.IsEditable && !property.IsMethod && (type is null || type.IsValueType || type == typeof( string )) )
			return new ValueText( property );

		var allEditors = EditorTypes()
							.Where( x => x.Type.TargetType.IsAssignableTo( typeof( BaseControl ) ) )
							.Select( x => new { score = x.Attribute.GetEditorScore( property ), editor = x } )
							.Where( x => x.score > 0 )
							.OrderByDescending( x => x.score )
							.ToArray();

		// Use the first editor we can successfully create
		foreach ( var entry in allEditors )
		{
			var c = entry.editor.Type.Create<BaseControl>();
			if ( c is not null )
			{
				if ( property.IsMultipleValues && !c.SupportsMultiEdit )
				{
					c.Delete( true );
					return new MultiEditNotSupported( property );
				}

				c.Property = property;
				return c;
			}
		}

		// Nope - sorry, nothing for you
		if ( property.IsMethod )
			return null;

		// No control for this class or struct - edit its properties
		if ( GenericControl.CanEdit( property ) && !property.IsMultipleValues )
			return new GenericControl { Property = property };

		return new ValueText( property );
	}

	/// <summary>
	/// Types with [CustomEditor] - the game's, and the editor's too, so editor-only controls like asset
	/// pickers and this library's own dropdowns are found. The engine reads its internal
	/// TypeLibrary.Editor for this; <see cref="EditorTypeLibrary"/> is the public handle to the same library.
	/// </summary>
	static IEnumerable<(TypeDescription Type, CustomEditorAttribute Attribute)> EditorTypes()
	{
		var game = Game.TypeLibrary.GetTypesWithAttribute<CustomEditorAttribute>( false );
		var editor = EditorTypeLibrary;

		if ( editor is null || editor == Game.TypeLibrary )
			return game;

		return game.Concat( editor.GetTypesWithAttribute<CustomEditorAttribute>( false ) )
			.DistinctBy( x => (x.Type.TargetType, x.Attribute) );
	}
}

file class MultiEditNotSupported : BaseControl
{
	Label label;

	public MultiEditNotSupported( SerializedProperty property )
	{
		label = AddChild<Label>();
		label.Text = "MultiEditNotSupported";

		Property = property;
	}
}

/// <summary>
/// The value as dimmed read-only text - for types with no control, and properties that can't be
/// written. Like the editor's MissingSerializedPropertyWidget.
/// </summary>
file class ValueText : BaseControl
{
	Label label;
	object shown;

	public ValueText( SerializedProperty property )
	{
		AddClass( "value-text" );
		Style.Opacity = 0.5f;
		Style.Padding = 6;

		label = AddChild<Label>();
		Property = property;
		Refresh();
	}

	public override void Tick()
	{
		base.Tick();
		Refresh();
	}

	void Refresh()
	{
		var value = Property?.GetValue<object>( null );
		if ( Equals( value, shown ) && label.Text is not null ) return;

		shown = value;
		label.Text = value?.ToString() ?? "None";
	}
}
