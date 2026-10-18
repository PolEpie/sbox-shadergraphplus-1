using Sandbox.Rendering;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;
using System.Globalization;

namespace ShaderGraphPlusPanelTest;

/// <summary>
/// The graph's parameters as a list, one row each: type in its type color, name, default value,
/// and tags for the settings worth seeing at a glance (sRGB, attribute, range...).
/// </summary>
public sealed class BlackboardPanel : Panel
{
	private readonly bool _isSubgraph;
	private readonly Dictionary<string, IBlackboardParameterType> _parameterTypes = new();

	private TextInput _searchFilter;
	private string _filter = "";
	private readonly Panel _list;

	/// <summary>
	/// The parameter picked in the list, or null.
	/// </summary>
	public BlackboardParameter Selected { get; private set; }

	/// <summary>
	/// The selection changed - to null when it's cleared.
	/// </summary>
	public Action<BlackboardParameter> OnSelected { get; set; }

	/// <summary>
	/// The graph whose parameters are listed. Setting it rebuilds the list.
	/// </summary>
	public ShaderGraphPlus.ShaderGraphPlus Graph
	{
		get => field;
		set
		{
			field = value;
			Select( null );
			Rebuild();
		}
	}

	public BlackboardPanel( bool isSubgraph )
	{
		_isSubgraph = isSubgraph;

		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );
		AddClass( "sg-blackboard" );

		foreach ( var type in EditorTypeLibrary.GetTypes<BlackboardParameter>().Where( x => !x.IsAbstract ) )
		{
			var parameterType = ClassBlackboardParameterType.HookupParameterType( type );
			_parameterTypes.TryAdd( parameterType.Identifier, parameterType );
		}

		BuildToolbar();

		_list = Add.Panel( "sg-params" );

		// Pressing empty space clears the selection
		_list.AddEventListener( "onmousedown", e =>
		{
			if ( e.Target == _list ) Select( null );
		} );

		Rebuild();
	}

	private void BuildToolbar()
	{
		var toolbar = AddChild( new Toolbar() );
		toolbar.AddClass( "panetools" );

		_searchFilter = toolbar.AddChild( new TextInput( "Filter..", "search" ) );
		_searchFilter.OnChange = value =>
		{
			_filter = value?.Trim() ?? "";
			Rebuild();
		};

		var addMenu = new Sandbox.UI.Menu();
		addMenu.AboutToShow += BuildAddMenu;
		toolbar.AddMenu( "", "add", addMenu ).Tooltip = "Add parameter";
	}

	private void BuildAddMenu( Sandbox.UI.Menu menu )
	{
		menu.Clear();

		if ( Graph is null )
			return;

		var types = BlackboardParameter.GetRelevantParameters( _parameterTypes, _isSubgraph )
			.OfType<ClassBlackboardParameterType>()
			.OrderBy( x => x.Type.Order )
			.ToArray();

		if ( !_isSubgraph )
		{
			var parameters = menu.AddMenu( "Parameter", "edit_attributes" );
			var attributes = menu.AddMenu( "Attribute", "edit_attributes" );
			var combos = menu.AddMenu( "Combo", "alt_route" );

			foreach ( var type in types )
			{
				var target = type.Type.TargetType;
				var submenu = target.IsAssignableTo( typeof( IBlackboardShaderFeatureParameter ) ) ? combos
					: target == typeof( SamplerStateParameter ) ? attributes
					: parameters;

				AddTypeOption( submenu, type );
			}
		}
		else
		{
			var inputs = menu.AddMenu( "Input", "input" );
			var outputs = menu.AddMenu( "Output", "output" );

			foreach ( var type in types )
			{
				AddTypeOption( type.Type.TargetType.IsAssignableTo( typeof( IBlackboardSubgraphOutputParameter ) ) ? outputs : inputs, type );
			}
		}
	}

	private void AddTypeOption( Sandbox.UI.Menu menu, ClassBlackboardParameterType type )
	{
		var option = menu.AddOption( type.Type.Title, type.DisplayInfo.Icon, () => CreateParameter( type ) );
		option.Tooltip = type.DisplayInfo.Description;
	}

	private void CreateParameter( IBlackboardParameterType type )
	{
		var parameter = (BlackboardParameter)type.CreateParameter( Graph );

		// Default group, like the Qt blackboard
		var groupName = parameter.GetGroupTitle();
		if ( Graph.TryFindGroupData( groupName, out var group ) )
		{
			group.ParameterReferences.Add( parameter.Identifier );
		}
		else
		{
			Graph.AddGroupData( new GroupData( [parameter.Identifier] ) { Name = groupName }, 0 );
		}

		Graph.AddParameter( parameter );

		_searchFilter.SetValue( "" );
		_filter = "";
		Rebuild();
	}

	/// <summary>
	/// Remove a parameter, its nodes and its group entry. Asks first if nodes would go.
	/// </summary>
	internal void Remove( BlackboardParameter parameter )
	{
		var nodes = Graph.Nodes
			.Where( x => x is IBlackboardNode node && node.ParameterIdentifier == parameter.Identifier )
			.ToArray();

		void Delete()
		{
			foreach ( var node in nodes )
			{
				Graph.RemoveNode( node );
			}

			Graph.RemoveParameter( parameter );

			if ( Selected == parameter )
				Select( null );

			if ( Graph.TryFindGroupData( parameter.GetGroupTitle(), out var group ) )
			{
				group.ParameterReferences.Remove( parameter.Identifier );

				if ( group.ParameterReferences.Count == 0 )
					Graph.RemoveGroupData( group );
			}

			Rebuild();
		}

		if ( nodes.Length == 0 )
		{
			Delete();
			return;
		}

		new Editor.PopupWindow(
			"Delete Parameter",
			$"Delete '{parameter.Name}'?\n\nIs referenced by {nodes.Length} parameter node{(nodes.Length == 1 ? "" : "s")}. These references will be cleared.",
			"Cancel",
			new Dictionary<string, Action> { ["Delete"] = Delete } ).Show();
	}

	/// <summary>
	/// Make <paramref name="parameter"/> the selected one, or clear the selection with null.
	/// </summary>
	public void Select( BlackboardParameter parameter )
	{
		if ( Selected == parameter )
			return;

		Selected = parameter;

		foreach ( var row in _list.Children.OfType<ParameterRow>() )
		{
			row.SetSelected( row.Parameter == parameter );
		}

		OnSelected?.Invoke( parameter );
	}

	/// <summary>
	/// Recreate the rows from the graph, applying the filter.
	/// </summary>
	public void Rebuild()
	{
		_list.DeleteChildren( true );

		var parameters = Graph?.Parameters.Where( MatchesFilter ).ToArray() ?? [];

		_list.SetClass( "is-empty", parameters.Length == 0 );

		if ( parameters.Length == 0 )
		{
			_list.Add.Label( Graph is null ? "No graph open" : !string.IsNullOrEmpty( _filter ) ? "No matching parameters" : "No parameters yet - add one with +", "empty" );
			return;
		}

		foreach ( var parameter in parameters )
		{
			var row = _list.AddChild( new ParameterRow( this, parameter ) );
			row.SetSelected( parameter == Selected );
		}
	}

	private bool MatchesFilter( BlackboardParameter parameter )
	{
		if ( string.IsNullOrEmpty( _filter ) )
			return true;

		return parameter.Name.Contains( _filter, StringComparison.OrdinalIgnoreCase )
			|| ParameterRow.TypeConfig( parameter ).Name.Contains( _filter, StringComparison.OrdinalIgnoreCase );
	}
}

/// <summary>
/// One parameter: a strip and type label in the type's color, the name, its tags, and the default value.
/// </summary>
internal sealed class ParameterRow : Panel
{
	private readonly BlackboardPanel _owner;
	private readonly BlackboardParameter _parameter;
	private Sandbox.UI.Menu _menu;

	internal BlackboardParameter Parameter => _parameter;

	public ParameterRow( BlackboardPanel owner, BlackboardParameter parameter )
	{
		_owner = owner;
		_parameter = parameter;

		AddClass( "sg-param" );

		var config = TypeConfig( parameter );

		Style.BorderLeftColor = config.Color;

		// Selection tint, shown by .selected. Toggling the row's own background left it magenta.
		Add.Panel( "highlight" ).Style.BackgroundColor = config.Color.WithAlpha( 0.2f );

		AddTypeChip( this, config );

		Add.Label( parameter.Name, "name" );

		var tags = Add.Panel( "tags" );
		foreach ( var tag in Tags( parameter ) )
		{
			tags.Add.Label( tag, "tag" );
		}

		AddValue( this, parameter );

		OnTooltip = BuildTooltip;
	}

	/// <summary>
	/// Full name, type, description, a big preview for textures, then default, range, group and tags.
	/// </summary>
	private void BuildTooltip( Panel tip )
	{
		// Tooltips open in their own window, so bring the styles along
		tip.StyleSheet.AddInline( ShaderGraphPlusStyleSheets.ShaderGraphPlusScss, "shadergraphplus_style" );
		tip.AddClass( "sg-tip" );

		var head = tip.Add.Panel( "head" );
		AddTypeChip( head, TypeConfig( _parameter ) );
		head.Add.Label( _parameter.Name, "name" );

		if ( !string.IsNullOrWhiteSpace( _parameter.DisplayInfo.Description ) )
			tip.Add.Label( _parameter.DisplayInfo.Description, "description" );

		if ( TextureOf( _parameter ) is { DefaultTexture: { Length: > 0 } path } )
		{
			tip.Add.Panel( "preview" ).Style.BackgroundImage = Texture.Load( $"thumb:{path}" );
			Detail( tip, "Texture" ).Add.Label( path, "text" );
		}
		else if ( _parameter is not IBlackboardSubgraphOutputParameter )
		{
			AddValue( Detail( tip, "Default" ), _parameter );
		}

		if ( _parameter is IRangedBlackboardMaterialParameter ranged )
			Detail( tip, "Range" ).Add.Label( $"{Format( ranged.GetRangeMin() )} – {Format( ranged.GetRangeMax() )}", "text" );

		Detail( tip, "Group" ).Add.Label( _parameter.GetGroupTitle(), "text" );

		// The range has its own line above
		var tags = Tags( _parameter ).Where( x => !x.Contains( '–' ) ).ToList();
		if ( tags.Count > 0 )
		{
			var row = tip.Add.Panel( "tags" );
			foreach ( var tag in tags ) row.Add.Label( tag, "tag" );
		}

		tip.Add.Label( "Right-click to delete", "hint" );
	}

	private static Panel Detail( Panel tip, string key )
	{
		var row = tip.Add.Panel( "detail" );
		row.Add.Label( key, "key" );
		return row;
	}

	/// <summary>
	/// Highlight in the type's color, like the Qt blackboard's selected pill.
	/// </summary>
	internal void SetSelected( bool selected ) => SetClass( "selected", selected );

	protected override void OnMouseDown( MousePanelEvent e )
	{
		base.OnMouseDown( e );

		if ( e.Button == "mouseleft" )
			_owner.Select( _parameter );
	}

	protected override void OnRightClick( MousePanelEvent e )
	{
		e.StopPropagation();
		_owner.Select( _parameter );
		_menu?.Delete( true );

		var menu = new Sandbox.UI.Menu();
		_menu = menu;
		menu.AddOption( "Delete", "delete", () => _owner.Remove( _parameter ) );
		menu.Closed += _ => { if ( _menu == menu ) _menu = null; menu.Delete( true ); };
		menu.Open( this, Popup.PositionMode.UnderMouse );
	}

	/// <summary>
	/// The label and color the Qt blackboard uses for this parameter's type.
	/// </summary>
	internal static BlackboardConfig TypeConfig( BlackboardParameter parameter )
	{
		return ShaderGraphPlusTheme.BlackboardConfigs.TryGetValue( parameter.GetType(), out var config )
			? config
			: new BlackboardConfig( parameter.DisplayInfo.Name, ShaderGraphPlusTheme.DefaultTypeColor );
	}

	/// <summary>
	/// The type label with its type-colored dot, as the Qt blackboard paints them. Shared with the
	/// properties heading so a selection reads the same in both places.
	/// </summary>
	internal static void AddTypeChip( Panel parent, BlackboardConfig config )
	{
		// Box that centers the label vertically
		var chip = parent.Add.Panel( "sg-type" );
		chip.Style.BackgroundColor = config.Color.WithAlpha( 0.12f );
		chip.Style.BorderColor = config.Color.WithAlpha( 0.45f );
		chip.Add.Panel( "dot" ).Style.BackgroundColor = config.Color;
		chip.Add.Label( config.Name, "text" ).Style.FontColor = config.Color;
	}

	private static void AddValue( Panel parent, BlackboardParameter parameter )
	{
		var value = parent.Add.Panel( "value" );

		switch ( parameter )
		{
			case ColorParameter color:
				AddSwatch( value, color.Value );
				break;

			case ColorSubgraphInputParameter color:
				AddSwatch( value, color.Value );
				break;

			case BlackboardTextureMaterialParameter texture:
				AddTexture( value, texture.Value );
				break;

			case IBlackboardSubgraphInputParameter when parameter.GetValue() is TextureInput texture:
				AddTexture( value, texture );
				break;

			case ShaderFeatureBooleanParameter feature:
				value.Add.Label( feature.Preview ? "On" : "Off", "text" );
				break;

			case ShaderFeatureEnumParameter feature:
				value.Add.Label( feature.Options.ElementAtOrDefault( feature.PreviewIndex )?.Name ?? "", "text" );
				break;

			case SamplerStateParameter sampler:
				value.Add.Label( sampler.Value.Filter.ToString(), "text" );
				break;

			case IBlackboardSubgraphInputParameter when parameter.GetValue() is Sampler sampler:
				value.Add.Label( sampler.Filter.ToString(), "text" );
				break;

			default:
				if ( parameter is not IBlackboardSubgraphOutputParameter && Format( parameter.GetValue() ) is { } text )
					value.Add.Label( text, "text" );
				break;
		}
	}

	private static void AddSwatch( Panel value, Color color )
	{
		var swatch = value.Add.Panel( "swatch" );
		swatch.Style.BackgroundColor = color;
		value.Add.Label( color.Hex, "text" );
	}

	private static void AddTexture( Panel value, TextureInput texture )
	{
		if ( !string.IsNullOrWhiteSpace( texture.DefaultTexture ) )
		{
			// The asset browser's thumbnail; the path is on hover
			var thumb = value.Add.Panel( "thumb" );
			thumb.Style.BackgroundImage = Texture.Load( $"thumb:{texture.DefaultTexture}" );
			thumb.Tooltip = texture.DefaultTexture;
			return;
		}

		AddSwatch( value, texture.DefaultColor );
	}

	private static TextureInput? TextureOf( BlackboardParameter parameter ) => parameter switch
	{
		BlackboardTextureMaterialParameter t => t.Value,
		IBlackboardSubgraphInputParameter when parameter.GetValue() is TextureInput t => t,
		_ => null,
	};

	/// <summary>
	/// Short labels for the settings that change how the parameter compiles or shows up.
	/// </summary>
	private static IEnumerable<string> Tags( BlackboardParameter parameter )
	{
		if ( parameter is IBlackboardMaterialParameter { IsAttribute: true } )
			yield return "Attribute";

		if ( parameter is IRangedBlackboardMaterialParameter ranged && ranged.GetRangeMin() is int or float )
			yield return $"{Format( ranged.GetRangeMin() )} – {Format( ranged.GetRangeMax() )}";

		if ( parameter is IBlackboardMaterialParameter { UI: FloatParameterUI { Type: UIType.Slider } } )
			yield return "Slider";

		if ( TextureOf( parameter ) is { } tex )
		{
			if ( tex.SrgbRead ) yield return "sRGB";
			if ( tex.ColorSpace == TextureColorSpace.Linear ) yield return "Linear";
			yield return tex.ImageFormat.ToString();
			if ( tex.Processor != TextureProcessor.None ) yield return tex.Processor.ToString();
			if ( tex.IsAttribute ) yield return "Attribute";
		}

		var sampler = parameter switch
		{
			SamplerStateParameter s => s.Value,
			IBlackboardSubgraphInputParameter when parameter.GetValue() is Sampler s => s,
			_ => (Sampler?)null,
		};

		if ( sampler is { } smp )
		{
			yield return smp.AddressModeU == smp.AddressModeV && smp.AddressModeV == smp.AddressModeW
				? smp.AddressModeU.ToString()
				: $"{smp.AddressModeU}/{smp.AddressModeV}/{smp.AddressModeW}";
			if ( smp.Filter == FilterMode.Anisotropic ) yield return $"x{smp.MaxAnisotropy}";
			if ( smp.IsAttribute ) yield return "Attribute";
		}

		if ( parameter is IBlackboardSubgraphInputParameter { IsRequired: true } )
			yield return "Required";

		if ( parameter is IBlackboardSubgraphOutputParameter { Preview: not SubgraphOutputPreviewType.None } output )
			yield return $"Preview {output.Preview}";

		if ( parameter is ShaderFeatureEnumParameter feature )
			yield return $"{feature.Options.Count} options";
	}

	private static string Format( object value ) => value switch
	{
		bool b => b ? "true" : "false",
		int i => i.ToString( CultureInfo.InvariantCulture ),
		float f => Number( f ),
		Vector2 v => $"{Number( v.x )}, {Number( v.y )}",
		Vector3 v => $"{Number( v.x )}, {Number( v.y )}, {Number( v.z )}",
		Vector4 v => $"{Number( v.x )}, {Number( v.y )}, {Number( v.z )}, {Number( v.w )}",
		_ => null,
	};

	private static string Number( float value ) => value.ToString( "0.###", CultureInfo.InvariantCulture );
}
