using Editor;
using Sandbox.Rendering;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;
using System.Globalization;

using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlusPanelTest;

/// <summary>
/// The graph's parameters in a list, one row each: type in its type color, name, default value,
/// and tags for the settings worth seeing at a glance (sRGB, attribute, range...).
/// </summary>
public sealed class BlackboardPanel : Panel
{
	private TextInput _searchFilter;
	private string _filter = "";
	private readonly Panel _list;

	private readonly bool _isSubgraph;

	private readonly Dictionary<string, IBlackboardParameterType> _availableParameters = new( StringComparer.OrdinalIgnoreCase );

	private readonly HashSet<string> _collapsedGroups;
	private string CollapsedGroupsCookie => EditorCookieNames.ShaderGraphPlusBlackboardCollapsedGroupsCookie;

	/// <summary>
	/// The currently selected parameter.
	/// </summary>
	public BlackboardParameter Selected { get; private set; }

	/// <summary>
	/// Called when a parmeter is selected.
	/// </summary>
	public Action<BlackboardParameter> OnSelected { get; set; }

	public Action<bool> OnDirty { get; set; }

	public Action OnParameterNodesDeleted { get; set; }

	public ShaderGraphPlus.ShaderGraphPlus Graph
	{
		get => field;
		set
		{
			if ( field == value ) return;

			field = value;

			Select( null );

			RebuildFromGraph();
		}
	}

	public BlackboardPanel( bool isSubgraph )
	{
		_isSubgraph = isSubgraph;

		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );
		AddClass( "sg-blackboard" );

		BuildToolbar();

		_list = Add.Panel( "sg-params" );

		// Pressing empty space clears the selection
		_list.AddEventListener( "onmousedown", e =>
		{
			if ( e.Target == _list ) Select( null );
		} );
	}

	private void BuildToolbar()
	{
		var toolbar = AddChild( new Toolbar() );
		toolbar.AddClass( "panetools" );

		_searchFilter = toolbar.AddChild( new TextInput( "Filter..", "search" ) );
		_searchFilter.OnChange = value =>
		{
			_filter = value?.Trim() ?? "";
			BuildFromParameters( Graph.Parameters );
		};

		var addMenu = new Sandbox.UI.Menu();
		addMenu.AboutToShow += AddParameterOptions;
		toolbar.AddMenu( "", "add", addMenu ).Tooltip = "Add parameter";
	}

	public void AddParameterType<T>() where T : BlackboardParameter
	{
		AddParameterType( EditorTypeLibrary.GetType<T>() );
	}

	public void AddParameterType( TypeDescription type )
	{
		var parameterType = ClassBlackboardParameterType.HookupParameterType( type );

		_availableParameters.TryAdd( parameterType.Identifier, parameterType );
	}

	private void CreateNewParameter( IBlackboardParameterType type, string group = "" )
	{
		group = NormalizeGroup( group );

		_searchFilter.SetValue( "" );
		_filter = "";
		ExpandGroup( group );

		// TODO
		//using var undoScope = UndoScope( "Add Parameter" );

		var parameter = (BlackboardParameter)type.CreateParameter( Graph );

		if ( parameter is IGroupableBlackboardParameter groupable )
		{
			SetGroupData( group, group, groupable );
		}

		Graph.AddParameter( parameter );

		RebuildParameterOrder();

		OnDirty?.Invoke( true );

		//_window.OnParameterSelected( parameter );

		RebuildFromGraph( true );
	}

	public IBlackboardParameter CreateNewParameter( IBlackboardParameterType type, string name = "", Action onCreated = null )
	{
		if ( type == null )
			return null;

		var group = NormalizeGroup( "" );

		_searchFilter.SetValue( "" );
		_filter = "";
		ExpandGroup( group );

		var parameter = type.CreateParameter( Graph, name );

		if ( parameter == null )
			return null;

		if ( parameter is IGroupableBlackboardParameter groupable )
		{
			SetGroupData( group, group, groupable );
		}

		onCreated?.Invoke();

		Graph.AddParameter( parameter );

		RebuildParameterOrder();

		return parameter;
	}

	private void AddParameterOptions( Sandbox.UI.Menu menu )
	{
		void AddOption( Sandbox.UI.Menu menu, IBlackboardParameterType parameterType, string icon, string description )
		{
			var option = menu.AddOption( parameterType.Type.Title, !string.IsNullOrWhiteSpace( icon ) ? icon : null, () =>
			{
				CreateNewParameter( parameterType, "" );//, group );
			} );

			option.Tooltip = description;
		}

		menu.Clear();

		if ( Graph is null )
			return;

		IBlackboardParameterType[] avalibleTypes = BlackboardParameter.GetRelevantParameters( _availableParameters, Graph.IsSubgraph ).ToArray();

		if ( !_isSubgraph )
		{
			var materialParametersMenu = menu.AddMenu( "Parameter", "edit_attributes" );
			var attributesMenu = menu.AddMenu( "Attribute", "edit_attributes" );
			var materialCombosMenu = menu.AddMenu( "Combo", "alt_route" );

			foreach ( var parameterType in avalibleTypes.OfType<ClassBlackboardParameterType>().OrderBy( x => x.Type.Order ) )
			{
				var targetType = parameterType.Type.TargetType;
				var icon = parameterType.DisplayInfo.Icon;
				var description = parameterType.DisplayInfo.Description;

				if ( targetType.IsAssignableTo( typeof( IBlackboardMaterialParameter ) ) || targetType.IsAssignableTo( typeof( BlackboardTextureMaterialParameter ) ) )
				{
					menu = materialParametersMenu;
				}
				else if ( targetType.IsAssignableTo( typeof( IBlackboardShaderFeatureParameter ) ) )
				{
					menu = materialCombosMenu;
				}
				else if ( targetType == typeof( SamplerStateParameter ) )
				{
					menu = attributesMenu;
				}

				AddOption( menu, parameterType, icon, description );
			}
		}
		else
		{
			var subgraphInputsMenu = menu.AddMenu( "Input", "input" );
			var subgraphOutputsMenu = menu.AddMenu( "Output", "output" );

			foreach ( var parameterType in avalibleTypes.OfType<ClassBlackboardParameterType>().OrderBy( x => x.Type.Order ) )
			{
				var targetType = parameterType.Type.TargetType;
				var icon = parameterType.DisplayInfo.Icon;
				var description = parameterType.DisplayInfo.Description;

				if ( targetType.IsAssignableTo( typeof( IBlackboardSubgraphInputParameter ) ) )
				{
					menu = subgraphInputsMenu;
				}
				else if ( targetType.IsAssignableTo( typeof( IBlackboardSubgraphOutputParameter ) ) )
				{
					menu = subgraphOutputsMenu;
				}

				AddOption( menu, parameterType, icon, description );
			}
		}
	}

	/// <summary>
	/// Remove a parameter, its nodes and its group entry. Asks first if nodes would go.
	/// </summary>
	internal void Remove( BlackboardParameter parameter )
	{
		var parameterNodes = Graph.Nodes
			.Where( x => x is IBlackboardNode node && node.ParameterIdentifier == parameter.Identifier )
			.ToArray();

		void Delete()
		{
			foreach ( var node in parameterNodes )
			{
				Graph.RemoveNode( node );
			}

			if ( parameterNodes.Any() )
			{
				OnParameterNodesDeleted?.Invoke();
			}

			Graph.RemoveParameter( parameter );

			if ( Selected == parameter )
				Select( null );

			RemoveParameterFromGroupData( parameter );

			if ( Graph.TryFindGroupData( parameter.GetGroupTitle(), out var group ) )
			{
				group.ParameterReferences.Remove( parameter.Identifier );

				if ( group.ParameterReferences.Count == 0 )
					Graph.RemoveGroupData( group );
			}

			RebuildParameterOrder();

			BuildFromParameters( Graph.Parameters );
		}

		if ( parameterNodes.Length == 0 )
		{
			Delete();
			return;
		}

		new Editor.PopupWindow(
			"Delete Parameter",
			$"Delete '{parameter.Name}'?\n\nIs referenced by {parameterNodes.Length} parameter node{(parameterNodes.Length == 1 ? "" : "s")}. These references will be cleared.",
			"Cancel",
			new Dictionary<string, Action> { ["Delete"] = Delete } ).Show();
	}

	/// <summary>
	/// Make <paramref name="parameter"/> the selected one, or clear the selection with null.
	/// </summary>
	public void Select( IBlackboardParameter parameter )
	{
		if ( Selected == parameter )
			return;

		Selected = (BlackboardParameter)parameter;

		foreach ( var row in _list.Children.OfType<ParameterRow>() )
		{
			row.SetSelected( row.Parameter == parameter );
		}

		OnSelected?.Invoke( (BlackboardParameter)parameter );
	}

	private void ExpandGroup( string group )
	{
		if ( _collapsedGroups.Remove( group ) )
			SaveCollapsedGroups();
	}

	private void PruneCollapsedGroups()
	{
		var groups = Graph.Parameters.OfType<IGroupableBlackboardParameter>().Select( GroupName ).ToHashSet( StringComparer.OrdinalIgnoreCase );
		_collapsedGroups.RemoveWhere( group => !groups.Contains( group ) );
		SaveCollapsedGroups();
	}

	private void SaveCollapsedGroups()
		=> EditorCookie.Set( CollapsedGroupsCookie, _collapsedGroups.OrderBy( x => x ).ToList() );

	private void SetGroupData( string sourceGroupName, string targetGroupName, IGroupableBlackboardParameter parameter, bool renaming = false )
	{
		void RemoveParameterFromGroup( GroupData group )
		{
			group.ParameterReferences.Remove( parameter.Identifier );

			if ( group.ParameterReferences.Count == 0 )
			{
				Graph.RemoveGroupData( group );
			}
		}

		if ( Graph is null )
			return;

		sourceGroupName = GroupTitle( sourceGroupName );
		parameter.Group = targetGroupName;
		targetGroupName = GroupTitle( targetGroupName );

		//SGPLogger.Info( $"Moving Parameter \"{parameter.Name}\" from group \"{sourceGroupName}\" to group \"{targetGroupName}\"" );

		if ( renaming )
		{
			if ( Graph.TryFindGroupData( targetGroupName, out var targetGroupData ) )
			{
				if ( Graph.TryFindGroupData( sourceGroupName, out var sourceGroupData ) )
				{
					RemoveParameterFromGroup( sourceGroupData );
				}

				if ( !targetGroupData.ParameterReferences.Contains( parameter.Identifier ) )
				{
					targetGroupData.ParameterReferences.Add( parameter.Identifier );
				}
			}
			else if ( Graph.TryFindGroupData( sourceGroupName, out var sourceGroupData ) )
			{
				// Just rename the source group to the targetGroupName
				// if a group with the targetGroupName does not exist yet.
				sourceGroupData.Name = targetGroupName;
			}
		}
		else
		{
			if ( Graph.TryFindGroupData( sourceGroupName, out var sourceGroupData ) && !sourceGroupName.Equals( targetGroupName, StringComparison.OrdinalIgnoreCase ) )
			{
				RemoveParameterFromGroup( sourceGroupData );
			}

			if ( Graph.TryFindGroupData( targetGroupName, out var existingGroup ) )
			{
				existingGroup.ParameterReferences.Add( parameter.Identifier );
			}
			else
			{
				var newGroup = new GroupData
				{
					Name = targetGroupName,
					ParameterReferences = [parameter.Identifier]
				};

				Graph.AddGroupData( newGroup, targetGroupName.Equals( BlackboardGlobals.EmptyGroupName, StringComparison.OrdinalIgnoreCase ) ? 0 : -1 );
			}
		}
	}

	private void RemoveParameterFromGroupData( BlackboardParameter parameter )
	{
		if ( Graph is null )
			return;

		var group = string.IsNullOrWhiteSpace( parameter.Group ) ? BlackboardGlobals.EmptyGroupName : parameter.Group;

		if ( Graph.TryFindGroupData( group, out var groupData ) )
		{
			groupData.ParameterReferences.Remove( parameter.Identifier );

			if ( groupData.ParameterReferences.Count == 0 )
			{
				Graph.RemoveGroupData( groupData );
			}
		}
	}

	internal void RenameGroup( string oldName )
	{
		if ( string.IsNullOrEmpty( oldName ) )
			return;

		OpenGroupNameDialog( "Rename Parameter Group", oldName, newName =>
		{
			newName = NormalizeGroup( newName );
			if ( newName.Equals( oldName, StringComparison.OrdinalIgnoreCase ) )
				return;

			// TODO
			//using var undoScope = UndoScope( "Rename Parameter Group" );

			foreach ( var parameter in Graph.Parameters.Where( p => GroupName( p ).Equals( oldName, StringComparison.OrdinalIgnoreCase ) ) )
			{
				SetGroupData( oldName, newName, parameter, true );
			}

			if ( GroupTitle( newName ).Equals( BlackboardGlobals.EmptyGroupName, StringComparison.OrdinalIgnoreCase ) )
			{
				Graph.ReOrderGroup( BlackboardGlobals.EmptyGroupName, 0 );
			}

			RebuildParameterOrder();

			if ( _collapsedGroups.Remove( oldName ) && !string.IsNullOrEmpty( newName ) )
				_collapsedGroups.Add( newName );

			PruneCollapsedGroups();
		} );
	}

	internal void ClearGroup( string group )
	{
		if ( string.IsNullOrEmpty( group ) )
			return;

		//using var undoScope = UndoScope( "Remove Parameter Group" );

		foreach ( var parameter in Graph.Parameters.Where( p => GroupName( p ).Equals( group, StringComparison.OrdinalIgnoreCase ) ) )
		{
			// Migrate each parameter to the 'General' group.
			SetGroupData( parameter.Group, "", parameter );
		}

		RebuildParameterOrder();

		_collapsedGroups.Remove( group );
		SaveCollapsedGroups();
	}

	/// <summary>
	/// Recreate the rows from the graph, applying the filter.
	/// </summary>
	public void BuildFromParameters( IEnumerable<IBlackboardParameter> parameters, bool preserveSelection = false )
	{
		_list.DeleteChildren( true );

		if ( parameters.Count() == 0 )
		{
			_list.Add.Label( Graph is null ? "No graph open" : !string.IsNullOrEmpty( _filter ) ? "No matching parameters" : "No parameters yet - add one with +", "empty" );
			return;
		}

		foreach ( var parameter in parameters )
		{
			var row = _list.AddChild( new ParameterRow( this, (BlackboardParameter)parameter ) );
			row.SetSelected( parameter == Selected );
		}
	}

	public void RebuildFromGraph( bool preserveSelection = false )
	{
		if ( Graph is not null )
		{
			var parameters = Graph?.Parameters.Where( MatchesFilter ).ToArray() ?? [];

			_list.SetClass( "is-empty", parameters.Length == 0 );

			BuildFromParameters( parameters, preserveSelection );
		}
	}

	internal void RebuildParameterOrder()
	{
		var parameterReferences = new HashSet<Guid>();

		// Group 1 : 0,1 ---> 0,1
		// Group 2 : 0,1,2,3 ---> 2,3,4,5
		// Group 3 : 0 ---> 6
		// Resulting new index's for each parameter in Graph.Parameters : 0,1,2,3,4,5,6

		foreach ( var group in Graph.GroupData )
		{
			foreach ( var parameterReference in group.ParameterReferences )
			{
				var parameter = Graph.FindParameter( parameterReference );

				if ( parameterReferences.Add( parameterReference ) )
				{
					/*
					var currentIndex = parameterReferences.Count;
					SGPLogger.Info( $"Parameter \"{parameter.Name}\" at index \"{( currentIndex > 0 ? currentIndex - 1 : currentIndex )}\"" );
					*/
				}
			}
		}

		foreach ( var (parameterIndex, parameterIdentifier) in parameterReferences.Index() )
		{
			Graph.ReOrderParameter( parameterIdentifier, parameterIndex );
		}
	}

	internal void OnGraphDirty()
	{
		if ( Graph is null )
			return;

		/*
		var parameters = Graph.Parameters.ToList();
		*/
	}

	private void OpenGroupNameDialog( string title, string initialValue, Action<string> accepted )
	{
		Dialog.AskString(
			name => accepted( name.Trim() ),
			"Group name:",
			okay: "Apply",
			initialName: initialValue,
			title: title,
			minLength: 1
		);
	}

	private IEnumerable<string> ExistingGroups() => Graph.Parameters.OfType<IGroupableBlackboardParameter>()
	.Select( GroupName )
	.Where( group => !string.IsNullOrEmpty( group ) )
	.Distinct( StringComparer.OrdinalIgnoreCase )
	.OrderBy( group => group, StringComparer.OrdinalIgnoreCase );

	/// <summary>
	/// Get a normalized version of <paramref name="group"/>.
	/// </summary>
	/// <param name="group"></param>
	/// <returns>The <paramref name="group"/> string as-is, or if <paramref name="group"/> is "General" just an empty string.</returns>
	private static string NormalizeGroup( string group )
	{
		group = group?.Trim() ?? "";
		return group.Equals( BlackboardGlobals.EmptyGroupName, StringComparison.OrdinalIgnoreCase ) ? "" : group;
	}

	/// <summary>
	/// Get a normalized version of the <paramref name="parameter"/> name.
	/// </summary>
	/// <param name="parameter"></param>
	/// <returns>The <paramref name="parameter"/> name as-is, or if the <paramref name="parameter"/> is named "General" just an empty string.</returns>
	internal static string GroupName( IGroupableBlackboardParameter parameter ) => NormalizeGroup( parameter.Group );

	/// <summary>
	/// Get a non null, empty or just whitespace version of the <paramref name="groupName"/>.
	/// </summary>
	/// <param name="groupName"></param>
	/// <returns>The <paramref name="groupName"/> if it isnt null, empty or just whitespace, otherwise the empty group name.</returns>
	internal static string GroupTitle( string groupName ) => string.IsNullOrWhiteSpace( groupName ) ? BlackboardGlobals.EmptyGroupName : groupName;

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
	private Sandbox.UI.Menu _menu;
	private readonly BlackboardPanel _blackboard;

	internal BlackboardParameter Parameter { get; private set; }

	public ParameterRow( BlackboardPanel blackboard, BlackboardParameter parameter )
	{
		_blackboard = blackboard;
		Parameter = parameter;

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
		AddTypeChip( head, TypeConfig( Parameter ) );
		head.Add.Label( Parameter.Name, "name" );

		if ( !string.IsNullOrWhiteSpace( Parameter.DisplayInfo.Description ) )
			tip.Add.Label( Parameter.DisplayInfo.Description, "description" );

		if ( TextureOf( Parameter ) is { DefaultTexture: { Length: > 0 } path } )
		{
			tip.Add.Panel( "preview" ).Style.BackgroundImage = Texture.Load( $"thumb:{path}" );
			Detail( tip, "Texture" ).Add.Label( path, "text" );
		}
		else if ( Parameter is not IBlackboardSubgraphOutputParameter )
		{
			AddValue( Detail( tip, "Default" ), Parameter );
		}

		if ( Parameter is IRangedBlackboardMaterialParameter ranged )
			Detail( tip, "Range" ).Add.Label( $"{Format( ranged.GetRangeMin() )} – {Format( ranged.GetRangeMax() )}", "text" );

		Detail( tip, "Group" ).Add.Label( Parameter.GetGroupTitle(), "text" );

		// The range has its own line above
		var tags = Tags( Parameter ).Where( x => !x.Contains( '–' ) ).ToList();
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
	/// Highlight the row in the type's color.
	/// </summary>
	internal void SetSelected( bool selected ) => SetClass( "selected", selected );

	protected override void OnMouseDown( MousePanelEvent e )
	{
		base.OnMouseDown( e );

		if ( e.Button == "mouseleft" )
			_blackboard.Select( Parameter );
	}

	protected override void OnRightClick( MousePanelEvent e )
	{
		e.StopPropagation();
		_blackboard.Select( Parameter );
		_menu?.Delete( true );

		var menu = new Sandbox.UI.Menu();
		_menu = menu;
		menu.AddOption( "Delete", "delete", () => _blackboard.Remove( (BlackboardParameter)Parameter ) );
		menu.Closed += _ => { if ( _menu == menu ) _menu = null; menu.Delete( true ); };
		menu.Open( this, Popup.PositionMode.UnderMouse );
	}

	/// <summary>
	/// The label and color of a parameter's type.
	/// </summary>
	internal static BlackboardConfig TypeConfig( IBlackboardParameter parameter )
	{
		return ShaderGraphPlusTheme.BlackboardConfigs.TryGetValue( parameter.GetType(), out var config )
			? config
			: new BlackboardConfig( parameter.DisplayInfo.Name, ShaderGraphPlusTheme.DefaultTypeColor );
	}

	internal static void AddTypeChip( Panel parent, BlackboardConfig config )
	{
		// Box that centers the label vertically
		var chip = parent.Add.Panel( "sg-type" );
		chip.Style.BackgroundColor = config.Color.WithAlpha( 0.12f );
		chip.Style.BorderColor = config.Color.WithAlpha( 0.45f );
		chip.Add.Panel( "dot" ).Style.BackgroundColor = config.Color;
		chip.Add.Label( config.Name, "text" ).Style.FontColor = config.Color;
	}

	private static void AddValue( Panel parent, IBlackboardParameter parameter )
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

	private static TextureInput? TextureOf( IBlackboardParameter parameter ) => parameter switch
	{
		BlackboardTextureMaterialParameter t => t.Value,
		IBlackboardSubgraphInputParameter when parameter.GetValue() is TextureInput t => t,
		_ => null,
	};

	/// <summary>
	/// Short labels for the settings that change how the parameter compiles or shows up.
	/// </summary>
	private static IEnumerable<string> Tags( IBlackboardParameter parameter )
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
