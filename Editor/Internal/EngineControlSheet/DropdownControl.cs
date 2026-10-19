// Temporary copy of the panel-controlsheet-objects branch's Editor.DropdownControl<T>. Delete this folder
// and point the Properties panel back at Sandbox.UI.ControlSheet once it ships in the engine.
using Sandbox.UI;

namespace EngineControlSheet;

/// <summary>
/// Panel version of <see cref="DropdownControlWidget{T}"/>. Subclasses list the choices in
/// <see cref="GetDropdownValues"/> - plain values, or <see cref="Entry"/> for a label, icon and
/// description - and are found with [CustomEditor] the same way the widgets are.
/// </summary>
[StyleSheet.Inline( "localdropdowncontrol", Styles )]
public abstract class DropdownControl<T> : BaseControl
{
	const string Styles = """
		.dropdown-control
		{
			flex-grow: 1;
		}

		// The editor theme lays .dropdown out as a vertical menu (it shares the class with menus),
		// which stacks the icon over a centred label. Read like a text box instead: icon, then the
		// text from the left, chevron on the right.
		.dropdown-control > .button.popupbutton.dropdown
		{
			flex-grow: 1;
			flex-direction: row;
			align-items: center;
			justify-content: flex-start;
			min-width: 0px;
			min-height: 32px;
			padding: 0px 32px 0px 12px;
			border-radius: 8px;
			background-color: #000a;
		}

		.dropdown-control > .button.popupbutton.dropdown > .button-right-column
		{
			flex-grow: 1;
			min-width: 0px;
			align-items: flex-start;
		}

		.dropdown-control > .button.popupbutton.dropdown .button-label
		{
			white-space: nowrap;
			overflow: hidden;
			text-overflow: ellipsis;
		}

		.dropdown-control.multiple > .dropdown
		{
			opacity: 0.5;
			font-style: italic;
		}
		""";

	public override bool SupportsMultiEdit => true;

	public struct Entry
	{
		public T Value { get; set; }
		public string Label { get; set; }
		public string Icon { get; set; }
		public string Description { get; set; }
	}

	/// <summary>
	/// The listed <see cref="Entry"/> for the current value, worked out when the value changes.
	/// Null when the value isn't listed, or is listed as a plain value.
	/// </summary>
	protected Entry? CurrentEntry { get; private set; }

	Picker _picker;
	int _shownHash;

	public DropdownControl()
	{
		AddClass( "dropdown-control" );
	}

	protected abstract IEnumerable<object> GetDropdownValues();

	/// <summary>
	/// Returns the text shown in the dropdown button.
	/// </summary>
	protected virtual string GetDisplayText()
	{
		return CurrentEntry?.Label ?? Property.GetValue<object>()?.ToString() ?? "None";
	}

	/// <summary>
	/// Returns the icon shown in the dropdown button.
	/// </summary>
	protected virtual string GetDisplayIcon()
	{
		return CurrentEntry?.Icon;
	}

	/// <summary>
	/// Called when a dropdown item is selected. Override for custom selection behavior.
	/// </summary>
	protected virtual void OnItemSelected( object item )
	{
		if ( item is Entry e )
			Property.SetValue( e.Value );
		else
			Property.SetValue( item );
	}

	public override void Rebuild()
	{
		DeleteChildren( true );
		_picker = null;
		_shownHash = 0;

		if ( Property is null ) return;

		_picker = AddChild( new Picker( this ) );
		Refresh();
	}

	public override void Tick()
	{
		base.Tick();
		Refresh();
	}

	void Refresh()
	{
		if ( _picker is null ) return;

		var multiple = Property.IsMultipleDifferentValues;
		var value = Property.GetValue<object>();
		var hash = HashCode.Combine( value, multiple );
		if ( hash == _shownHash ) return;
		_shownHash = hash;

		SetClass( "multiple", multiple );

		if ( multiple )
		{
			CurrentEntry = null;
			_picker.Text = "Multiple Values";
			_picker.Icon = null;
			return;
		}

		CurrentEntry = null;
		foreach ( var item in GetDropdownValues() )
		{
			if ( item is Entry e && Equals( e.Value, value ) )
			{
				CurrentEntry = e;
				break;
			}
		}

		_picker.Text = GetDisplayText();
		_picker.Icon = GetDisplayIcon();
	}

	void Choose( object item )
	{
		Property.Parent?.NoteStartEdit( Property );
		OnItemSelected( item );
		Property.Parent?.NoteFinishEdit( Property );

		_shownHash = 0;
		Refresh();
	}

	/// <summary>
	/// The engine's <see cref="DropDown"/> look, filled from <see cref="GetDropdownValues"/> each
	/// time it opens rather than from its own option list.
	/// </summary>
	sealed class Picker : DropDown
	{
		readonly DropdownControl<T> _owner;

		public Picker( DropdownControl<T> owner )
		{
			_owner = owner;
		}

		public override void Open()
		{
			var property = _owner.Property;
			if ( property is null || !property.IsEditable ) return;

			Popup = new Sandbox.UI.Popup( this, Sandbox.UI.Popup.PositionMode.BelowStretch, 0.0f );
			Popup.CloseWhenParentIsHidden = true;
			Popup.AddClass( "flat-top" );

			var current = property.IsMultipleDifferentValues ? null : property.GetValue<object>();

			foreach ( var item in _owner.GetDropdownValues() )
			{
				var (value, label, icon, description) = item is Entry e
					? ((object)e.Value, e.Label, e.Icon, e.Description)
					: (item, item?.ToString(), null, null);

				var option = Popup.AddOption( label, icon, () => _owner.Choose( item ) );
				option.Tooltip = description?.Trim();
				option.SetClass( "active", Equals( value, current ) );
			}
		}
	}
}
