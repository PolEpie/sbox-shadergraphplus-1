using Editor;
using Sandbox.Rendering;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;
using System.Globalization;

using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlusPanelTest;

/// <summary>
/// The graph's parameters, grouped: a header per group, then its parameters one row each - type in its type color,
/// name, default value, and tags for the settings worth seeing at a glance (sRGB, attribute, range...).
/// Parameters and groups reorder by dragging, and parameters rename in place.
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

	private readonly List<ParameterRow> _rowPanels = [];
	private readonly List<ParameterGroupHeader> _headers = [];

	private Sandbox.UI.Menu _menu;
	private BlackboardItem _dragSource;

	// Drops, menus and graph edits change what the list should show from inside an event of a panel the
	// rebuild would delete, so the rebuild waits for the next tick
	private bool _rebuildPending;

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

		_collapsedGroups = EditorCookie.Get<List<string>>( CollapsedGroupsCookie, [] ).ToHashSet( StringComparer.OrdinalIgnoreCase );

		AddClass( ShaderGraphPlusCssClasses.EditorDockPane );
		AddClass( "sg-blackboard" );

		// For F2 and Delete: a click anywhere in the blackboard lands the focus here
		AcceptsFocus = true;

		BuildToolbar();

		_list = Add.Panel( "sg-params" );

		// Pressing empty space clears the selection
		_list.AddEventListener( "onmousedown", OnListMouseDown );
	}

	private void OnListMouseDown( PanelEvent e )
	{
		if ( e.Target == _list ) Select( null );
	}

	private void BuildToolbar()
	{
		var toolbar = AddChild( new Toolbar() );
		toolbar.AddClass( "panetools" );

		_searchFilter = toolbar.AddChild( new TextInput( "Filter..", "search" ) );
		_searchFilter.OnChange = OnFilterChanged;

		var addMenu = new Sandbox.UI.Menu();
		addMenu.AboutToShow += OnAddMenuAboutToShow;
		toolbar.AddMenu( "", "add", addMenu ).Tooltip = "Add parameter";
	}

	private void OnFilterChanged( string value )
	{
		_filter = value?.Trim() ?? "";
		RebuildFromGraph();
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

		var parameter = (BlackboardParameter)type.CreateParameter( Graph );

		if ( parameter is IGroupableBlackboardParameter groupable )
		{
			SetGroupData( group, group, groupable );
		}

		Graph.AddParameter( parameter );

		RebuildParameterOrder();

		OnDirty?.Invoke( true );

		Reselect( parameter );

		QueueRebuild();
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

	/// <summary>
	/// The toolbar's + menu, rebuilt every time it opens: parameter types, then the same again per existing group.
	/// </summary>
	private void OnAddMenuAboutToShow( Sandbox.UI.Menu menu )
	{
		menu.Clear();

		if ( Graph is null )
			return;

		AddParameterOptions( menu, "" );
		AddGroupedOptions( menu );
	}

	/// <summary>
	/// "Add to Group": one submenu per existing group, each offering every parameter type.
	/// </summary>
	private void AddGroupedOptions( Sandbox.UI.Menu menu )
	{
		var groups = ExistingGroups().ToList();
		if ( groups.Count == 0 )
			return;

		menu.AddSeparator();
		var grouped = menu.AddMenu( "Add to Group", "folder" );
		foreach ( var group in groups )
		{
			AddParameterOptions( grouped.AddMenu( group, "folder" ), group );
		}
	}

	/// <summary>
	/// Every parameter type that fits the graph, sorted into submenus. Choosing one adds it to <paramref name="group"/>.
	/// </summary>
	private void AddParameterOptions( Sandbox.UI.Menu root, string group )
	{
		void AddOption( Sandbox.UI.Menu menu, IBlackboardParameterType parameterType, string icon, string description )
		{
			var option = menu.AddOption( parameterType.Type.Title, !string.IsNullOrWhiteSpace( icon ) ? icon : null, () =>
			{
				CreateNewParameter( parameterType, group );
			} );

			option.Tooltip = description;
		}

		IBlackboardParameterType[] avalibleTypes = BlackboardParameter.GetRelevantParameters( _availableParameters, Graph.IsSubgraph ).ToArray();

		var target = root;

		if ( !_isSubgraph )
		{
			var materialParametersMenu = root.AddMenu( "Parameter", "edit_attributes" );
			var attributesMenu = root.AddMenu( "Attribute", "edit_attributes" );
			var materialCombosMenu = root.AddMenu( "Combo", "alt_route" );

			foreach ( var parameterType in avalibleTypes.OfType<ClassBlackboardParameterType>().OrderBy( x => x.Type.Order ) )
			{
				var targetType = parameterType.Type.TargetType;
				var icon = parameterType.DisplayInfo.Icon;
				var description = parameterType.DisplayInfo.Description;

				if ( targetType.IsAssignableTo( typeof( IBlackboardMaterialParameter ) ) || targetType.IsAssignableTo( typeof( BlackboardTextureMaterialParameter ) ) )
				{
					target = materialParametersMenu;
				}
				else if ( targetType.IsAssignableTo( typeof( IBlackboardShaderFeatureParameter ) ) )
				{
					target = materialCombosMenu;
				}
				else if ( targetType == typeof( SamplerStateParameter ) )
				{
					target = attributesMenu;
				}

				AddOption( target, parameterType, icon, description );
			}
		}
		else
		{
			var subgraphInputsMenu = root.AddMenu( "Input", "input" );
			var subgraphOutputsMenu = root.AddMenu( "Output", "output" );

			foreach ( var parameterType in avalibleTypes.OfType<ClassBlackboardParameterType>().OrderBy( x => x.Type.Order ) )
			{
				var targetType = parameterType.Type.TargetType;
				var icon = parameterType.DisplayInfo.Icon;
				var description = parameterType.DisplayInfo.Description;

				if ( targetType.IsAssignableTo( typeof( IBlackboardSubgraphInputParameter ) ) )
				{
					target = subgraphInputsMenu;
				}
				else if ( targetType.IsAssignableTo( typeof( IBlackboardSubgraphOutputParameter ) ) )
				{
					target = subgraphOutputsMenu;
				}

				AddOption( target, parameterType, icon, description );
			}
		}
	}

	/// <summary>
	/// "Move to Group": General, then each existing group with a check on the parameter's own, then a new one.
	/// </summary>
	private void AddGroupOptions( Sandbox.UI.Menu menu, IGroupableBlackboardParameter parameter )
	{
		var groups = menu.AddMenu( "Move to Group", "folder" );
		groups.AddOption( BlackboardGlobals.EmptyGroupName, null, () => MoveToGroup( parameter, "" ) ).Checked = string.IsNullOrEmpty( GroupName( parameter ) );

		foreach ( var group in ExistingGroups() )
		{
			groups.AddOption( group, null, () => MoveToGroup( parameter, group ) ).Checked = GroupName( parameter ).Equals( group, StringComparison.OrdinalIgnoreCase );
		}

		groups.AddSeparator();
		groups.AddOption( "New Group…", "create_new_folder", () => OpenGroupNameDialog( "New Parameter Group", "", name => MoveToGroup( parameter, name ) ) );
	}

	/// <summary>
	/// Open a context menu, filled in by <paramref name="build"/>, under the mouse. Replaces the one already open.
	/// </summary>
	private void OpenMenu( Panel source, Action<Sandbox.UI.Menu> build )
	{
		_menu?.Delete( true );

		var menu = new Sandbox.UI.Menu();
		_menu = menu;
		build( menu );
		menu.Closed += OnMenuClosed;
		menu.Open( source, Popup.PositionMode.UnderMouse );
	}

	private void OnMenuClosed( Sandbox.UI.Menu menu )
	{
		if ( _menu == menu ) _menu = null;
		menu.Delete( true );
	}

	/// <summary>
	/// A parameter's right-click menu: rename, move to a group, delete.
	/// </summary>
	internal void OpenParameterMenu( ParameterRow row )
	{
		var parameter = row.Parameter;

		OpenMenu( row, menu =>
		{
			menu.AddOption( "Rename", "edit", row.StartRename ).Shortcut = "F2";
			AddGroupOptions( menu, parameter );

			menu.AddSeparator();
			menu.AddOption( "Delete", "delete", () => Remove( parameter ) ).Shortcut = "Del";
		} );
	}

	/// <summary>
	/// A group header's right-click menu: add a parameter to it, and for any group but General, rename or remove it.
	/// </summary>
	internal void OpenGroupMenu( ParameterGroupHeader header )
	{
		var group = header.Group;

		OpenMenu( header, menu =>
		{
			AddParameterOptions( menu.AddMenu( "Add Parameter", "add" ), group.Name );

			if ( group.Name.Equals( BlackboardGlobals.EmptyGroupName, StringComparison.OrdinalIgnoreCase ) )
				return;

			menu.AddSeparator();
			menu.AddOption( "Rename Group", "edit", () => RenameGroup( group.Name ) );
			menu.AddOption( "Remove Group", "folder_off", () => ClearGroup( group.Name ) );
		} );
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

			RebuildParameterOrder();

			PruneCollapsedGroups();

			OnDirty?.Invoke( true );

			QueueRebuild();
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

		ApplySelection( parameter );
	}

	/// <summary>
	/// Select <paramref name="parameter"/> and tell the listeners even when it already was, so whatever shows
	/// it picks up the rename or move.
	/// </summary>
	private void Reselect( IBlackboardParameter parameter ) => ApplySelection( parameter );

	private void ApplySelection( IBlackboardParameter parameter )
	{
		Selected = (BlackboardParameter)parameter;

		foreach ( var row in _rowPanels )
		{
			row.SetSelected( row.Parameter == parameter );
		}

		OnSelected?.Invoke( Selected );
	}

	/// <summary>
	/// F2 renames the selected parameter and Delete removes it, unless the filter has the keyboard.
	/// </summary>
	public override void OnButtonTyped( ButtonEvent e )
	{
		if ( Selected is { } selected && !_searchFilter.HasFocus )
		{
			if ( e.Button == "f2" )
			{
				BeginRename( selected );
				e.StopPropagation = true;
				return;
			}

			if ( e.Button == "delete" )
			{
				Remove( selected );
				e.StopPropagation = true;
				return;
			}
		}

		base.OnButtonTyped( e );
	}

	public override void Tick()
	{
		base.Tick();

		if ( _rebuildPending )
			RebuildFromGraph();

		if ( _dragSource is { IsValid: true } )
			UpdateDropIndicator();
	}

	private void QueueRebuild() => _rebuildPending = true;

	/// <summary>
	/// Rename a parameter. Empty names, the same name, and names another parameter has are ignored.
	/// </summary>
	internal void Rename( IBlackboardParameter parameter, string name )
	{
		name = name?.Trim();

		if ( string.IsNullOrEmpty( name ) || name == parameter.Name )
			return;

		if ( Graph.Parameters.Any( p => p != parameter && p.Name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) )
			return;

		parameter.Name = name;

		OnDirty?.Invoke( true );

		Reselect( parameter );

		QueueRebuild();
	}

	internal void BeginRename( IBlackboardParameter parameter )
		=> _rowPanels.FirstOrDefault( r => r.Parameter == parameter )?.StartRename();

	/// <summary>
	/// Collapse or expand a group. Not while filtering, when every matching group shows.
	/// </summary>
	internal void ToggleGroup( string group )
	{
		if ( !string.IsNullOrWhiteSpace( _filter ) )
			return;

		if ( !_collapsedGroups.Add( group ) )
			_collapsedGroups.Remove( group );

		SaveCollapsedGroups();
		QueueRebuild();
	}

	internal void MoveToGroup( IGroupableBlackboardParameter parameter, string targetGroupName )
	{
		targetGroupName = NormalizeGroup( targetGroupName );
		if ( GroupName( parameter ).Equals( targetGroupName, StringComparison.OrdinalIgnoreCase ) )
			return;

		var sourceGroupName = parameter.Group;
		ExpandGroup( targetGroupName );
		PruneCollapsedGroups();

		SetGroupData( sourceGroupName, targetGroupName, parameter );

		RebuildParameterOrder();

		OnDirty?.Invoke( true );

		Reselect( parameter );

		QueueRebuild();
	}

	/// <summary>
	/// Move a group to index <paramref name="priority"/> in the graph's group order.
	/// </summary>
	internal void ReorderGroup( GroupData group, int priority )
	{
		Graph.ReOrderGroup( group, priority );

		RebuildParameterOrder();

		OnDirty?.Invoke( true );

		QueueRebuild();
	}

	/// <summary>
	/// A parameter row dropped on another: reorder within the group, or move into the target's group, next to the target.
	/// </summary>
	internal void DropParameter( ParameterRow source, ParameterRow target, DropMode mode )
	{
		var sourceParameter = source.Parameter;
		var targetParameter = target.Parameter;

		var sourceGroupName = GroupTitle( sourceParameter.Group );
		var targetGroupName = GroupTitle( targetParameter.Group );

		if ( !Graph.TryFindGroupData( sourceGroupName, out var sourceGroupData ) )
			return;

		if ( !sourceGroupName.Equals( targetGroupName, StringComparison.OrdinalIgnoreCase ) && Graph.TryFindGroupData( targetGroupName, out var targetGroupData ) )
		{
			var targetIndex = targetGroupData.ParameterReferences.IndexOf( targetParameter.Identifier );

			if ( mode == DropMode.Below )
			{
				targetIndex++;
			}

			sourceGroupData.ParameterReferences.Remove( sourceParameter.Identifier );

			if ( sourceGroupData.ParameterReferences.Count == 0 )
			{
				Graph.RemoveGroupData( sourceGroupData );
			}

			targetGroupData.ParameterReferences.Insert( Math.Clamp( targetIndex, 0, targetGroupData.ParameterReferences.Count ), sourceParameter.Identifier );

			sourceParameter.Group = targetParameter.Group;
		}
		else
		{
			var targetIndex = sourceGroupData.ParameterReferences.IndexOf( targetParameter.Identifier );

			sourceGroupData.ParameterReferences.Remove( sourceParameter.Identifier );
			sourceGroupData.ParameterReferences.Insert( Math.Clamp( targetIndex, 0, sourceGroupData.ParameterReferences.Count ), sourceParameter.Identifier );
		}

		PruneCollapsedGroups();

		RebuildParameterOrder();

		OnDirty?.Invoke( true );

		QueueRebuild();
	}

	/// <summary>
	/// Called as <paramref name="source"/> starts being dragged.
	/// </summary>
	internal void BeginDrag( BlackboardItem source )
	{
		ClearDropIndicators();
		_dragSource = source;
	}

	internal void EndDrag( BlackboardItem source )
	{
		if ( _dragSource == source )
			_dragSource = null;

		ClearDropIndicators();
	}

	/// <summary>
	/// Show where the dragged item would land on whatever is under the mouse, and nothing on the rest. Polled
	/// each tick instead of tracking drag enter and leave, which fire for every child the mouse crosses.
	/// </summary>
	private void UpdateDropIndicator()
	{
		BlackboardItem hovered = null;

		if ( IsUnderMouse( _list ) )
		{
			hovered = _headers.FirstOrDefault( IsUnderMouse );
			hovered ??= _rowPanels.FirstOrDefault( IsUnderMouse );
		}

		foreach ( var header in _headers )
		{
			header.DropIndicator = header == hovered ? header.GetDrop( _dragSource ) : DropMode.None;
		}

		foreach ( var row in _rowPanels )
		{
			row.DropIndicator = row == hovered ? row.GetDrop( _dragSource ) : DropMode.None;
		}
	}

	private static bool IsUnderMouse( Panel panel ) => panel.IsInside( panel.MousePosition + panel.Box.Rect.Position );

	private void ClearDropIndicators()
	{
		foreach ( var header in _headers )
		{
			header.DropIndicator = DropMode.None;
		}

		foreach ( var row in _rowPanels )
		{
			row.DropIndicator = DropMode.None;
		}
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

			OnDirty?.Invoke( true );

			QueueRebuild();
		} );
	}

	internal void ClearGroup( string group )
	{
		if ( string.IsNullOrEmpty( group ) )
			return;

		foreach ( var parameter in Graph.Parameters.Where( p => GroupName( p ).Equals( group, StringComparison.OrdinalIgnoreCase ) ) )
		{
			// Migrate each parameter to the 'General' group.
			SetGroupData( parameter.Group, "", parameter );
		}

		RebuildParameterOrder();

		_collapsedGroups.Remove( group );
		SaveCollapsedGroups();

		OnDirty?.Invoke( true );

		QueueRebuild();
	}

	/// <summary>
	/// Recreate the list from <paramref name="parameters"/>, applying the filter: a header per group in the graph's
	/// group order, then each expanded group's rows in the group's order.
	/// </summary>
	public void BuildFromParameters( IEnumerable<IBlackboardParameter> parameters, bool preserveSelection = false )
	{
		_list.DeleteChildren( true );
		_rowPanels.Clear();
		_headers.Clear();
		_dragSource = null;
		_rebuildPending = false;

		if ( Graph is null )
		{
			_list.SetClass( "is-empty", true );
			_list.Add.Label( "No graph open", "empty" );
			return;
		}

		var groups = OrderedGroups( parameters ).ToList();

		_list.SetClass( "is-empty", groups.Count == 0 );

		if ( groups.Count == 0 )
		{
			_list.Add.Label( Graph.Parameters.Any() ? "No matching parameters" : "No parameters yet - add one with +", "empty" );
			return;
		}

		var filtering = !string.IsNullOrWhiteSpace( _filter );

		foreach ( var group in groups )
		{
			var collapsed = !filtering && _collapsedGroups.Contains( group.Key );
			var groupData = GroupDataFor( group.Key );

			_headers.Add( _list.AddChild( new ParameterGroupHeader( this, groupData, group.Count(), collapsed, !filtering ) ) );

			if ( collapsed )
				continue;

			var body = _list.Add.Panel( "sg-group-body" );

			foreach ( var parameter in group )
			{
				var blackboardParameter = (BlackboardParameter)parameter;
				blackboardParameter.Graph = Graph;

				var row = body.AddChild( new ParameterRow( this, blackboardParameter, groupData ) );
				row.SetSelected( blackboardParameter == Selected );

				_rowPanels.Add( row );
			}
		}
	}

	public void RebuildFromGraph( bool preserveSelection = false )
	{
		if ( Graph is not null )
		{
			BuildFromParameters( Graph.Parameters, preserveSelection );
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
				parameterReferences.Add( parameterReference );
			}
		}

		foreach ( var (parameterIndex, parameterIdentifier) in parameterReferences.Index() )
		{
			Graph.ReOrderParameter( parameterIdentifier, parameterIndex );
		}
	}

	/// <summary>
	/// The graph changed. Rebuild the list if what it shows - the parameters, their order, groups and names - no longer matches.
	/// </summary>
	internal void OnGraphDirty()
	{
		if ( Graph is null )
			return;

		var parameters = DisplayedParameters( Graph.Parameters ).ToList();
		var stale = _rowPanels.Count != parameters.Count;

		for ( var i = 0; !stale && i < _rowPanels.Count; i++ )
		{
			var row = _rowPanels[i];
			var parameter = parameters[i];

			stale = !ReferenceEquals( row.Parameter, parameter ) || row.BuiltGroup != GroupName( parameter ) || row.BuiltName != parameter.Name;
		}

		if ( stale )
			QueueRebuild();
	}

	/// <summary>
	/// The parameters that pass the filter, grouped, groups in the graph's group order and each group's
	/// parameters in the group's order.
	/// </summary>
	private IEnumerable<IGrouping<string, IGroupableBlackboardParameter>> OrderedGroups( IEnumerable<IBlackboardParameter> parameters )
	{
		return parameters.OfType<IGroupableBlackboardParameter>()
			.Where( MatchesFilter )
			.OrderBy( p => Graph.GetParameterIndexInGroup( GroupName( p ), p.Identifier ) )
			.GroupBy( GroupName, StringComparer.OrdinalIgnoreCase )
			.OrderBy( group => GroupIndex( group.Key ) );
	}

	/// <summary>
	/// The parameters that have a row: everything matching the filter outside collapsed groups, in list order.
	/// </summary>
	private IEnumerable<IGroupableBlackboardParameter> DisplayedParameters( IEnumerable<IBlackboardParameter> parameters )
	{
		var filtering = !string.IsNullOrWhiteSpace( _filter );

		foreach ( var group in OrderedGroups( parameters ) )
		{
			if ( !filtering && _collapsedGroups.Contains( group.Key ) )
				continue;

			foreach ( var parameter in group )
				yield return parameter;
		}
	}

	private int GroupIndex( string groupName )
		=> Graph.TryFindGroupData( GroupTitle( groupName ), out var groupData ) ? Graph.GetGroupDataIndex( groupData ) : int.MaxValue;

	/// <summary>
	/// The graph's data for a group. A group the file has parameters for but no data - hand edited, or from before
	/// groups were stored - gets some, so it shows and can be reordered.
	/// </summary>
	private GroupData GroupDataFor( string groupName )
	{
		var title = GroupTitle( groupName );

		if ( Graph.TryFindGroupData( title, out var groupData ) )
			return groupData;

		groupData = new GroupData
		{
			Name = title,
			ParameterReferences = Graph.Parameters
				.Where( p => GroupName( p ).Equals( groupName, StringComparison.OrdinalIgnoreCase ) )
				.Select( p => p.Identifier )
				.ToList()
		};

		Graph.AddGroupData( groupData, title.Equals( BlackboardGlobals.EmptyGroupName, StringComparison.OrdinalIgnoreCase ) ? 0 : -1 );

		return groupData;
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

	/// <summary>
	/// Whether the filter matches the parameter's type, name, or group.
	/// </summary>
	private bool MatchesFilter( IGroupableBlackboardParameter parameter )
	{
		if ( string.IsNullOrEmpty( _filter ) )
			return true;

		return parameter.Name.Contains( _filter, StringComparison.OrdinalIgnoreCase )
			|| parameter.DisplayInfo.Name.Contains( _filter, StringComparison.OrdinalIgnoreCase )
			|| ParameterRow.TypeConfig( parameter ).Name.Contains( _filter, StringComparison.OrdinalIgnoreCase )
			|| GroupTitle( GroupName( parameter ) ).Contains( _filter, StringComparison.OrdinalIgnoreCase );
	}
}

/// <summary>
/// How a dragged item lands on the item it's over.
/// </summary>
internal enum DropMode
{
	None,
	Above,
	Below,

	/// <summary>
	/// Into the item itself: a parameter onto a group header.
	/// </summary>
	Onto,
}

/// <summary>
/// A parameter row or a group header: something that can be dragged within the blackboard and dropped onto.
/// </summary>
internal abstract class BlackboardItem : Panel
{
	protected BlackboardPanel Blackboard { get; }

	/// <summary>
	/// The drop indicator shown on this item: a line above or below it, or an outline around it.
	/// </summary>
	internal DropMode DropIndicator
	{
		get;
		set
		{
			if ( field == value ) return;

			field = value;
			SetClass( "drop-above", value == DropMode.Above );
			SetClass( "drop-below", value == DropMode.Below );
			SetClass( "drop-onto", value == DropMode.Onto );
		}
	}

	protected BlackboardItem( BlackboardPanel blackboard )
	{
		Blackboard = blackboard;

		// The line or outline itself, shown by .drop-above, .drop-below and .drop-onto
		Add.Panel( "drop" );
	}

	/// <summary>
	/// Where <paramref name="source"/> would land if it were dropped here, or None if it can't be.
	/// </summary>
	internal abstract DropMode GetDrop( BlackboardItem source );

	/// <summary>
	/// Carry out the drop <see cref="GetDrop"/> described.
	/// </summary>
	protected abstract void ApplyDrop( BlackboardItem source, DropMode mode );

	protected override void OnDragStart( DragEvent e )
	{
		AddClass( "dragging" );
		Blackboard.BeginDrag( this );
		e.StopPropagation();
	}

	/// <summary>
	/// The drag is ours, not a scroll of the list.
	/// </summary>
	protected override void OnDrag( DragEvent e ) => e.StopPropagation();

	protected override void OnDragEnd( DragEvent e )
	{
		RemoveClass( "dragging" );
		Blackboard.EndDrag( this );
		e.StopPropagation();
	}

	protected override void OnDragCancel( DragEvent e ) => OnDragEnd( e );

	/// <summary>
	/// Released over this item or one of its children, the event bubbling up to here. The source is the event's target.
	/// </summary>
	protected override void OnDrop( PanelEvent e )
	{
		// Files and text from outside the app aren't ours
		if ( e is DropEvent || e.Target is not BlackboardItem source ) return;

		e.StopPropagation();

		var mode = GetDrop( source );
		DropIndicator = DropMode.None;

		if ( mode != DropMode.None )
			ApplyDrop( source, mode );
	}
}

/// <summary>
/// A group's title in the list: collapse arrow, folder, name and parameter count. Click to collapse or expand,
/// drop parameters on it to move them in, drag it to reorder the groups, right-click for group actions.
/// </summary>
internal sealed class ParameterGroupHeader : BlackboardItem
{
	private readonly bool _collapsible;

	internal GroupData Group { get; }

	/// <summary>
	/// The group everything starts in. It always stays first.
	/// </summary>
	private bool IsGeneral => Group.Name.Equals( BlackboardGlobals.EmptyGroupName, StringComparison.OrdinalIgnoreCase );

	public ParameterGroupHeader( BlackboardPanel blackboard, GroupData group, int count, bool collapsed, bool collapsible ) : base( blackboard )
	{
		Group = group;
		group.Graph = blackboard.Graph;
		_collapsible = collapsible;

		var title = BlackboardPanel.GroupTitle( group.Name );

		AddClass( "sg-group" );
		SetClass( "collapsed", collapsed );
		SetClass( "fixed", !collapsible );

		this.Icon( collapsed ? "chevron_right" : "expand_more", "arrow" );
		this.Icon( "folder", "folder" );
		Add.Label( title, "title" );
		Add.Label( count.ToString(), "count" );

		var reorder = IsGeneral ? "" : "\nDrag to reorder this group.";

		Tooltip = collapsible
			? $"{title}\nClick to collapse or expand.\nDrag parameters here to move them into this group.{reorder}\nRight-click for group actions."
			: $"{title}\nMatching groups are expanded while filtering.\nDrag parameters here to move them into this group.{reorder}\nRight-click for group actions.";
	}

	/// <summary>
	/// The General group stays put, so it isn't dragged; that leaves the drag to scroll the list.
	/// </summary>
	public override bool WantsDrag => !IsGeneral;

	protected override void OnClick( MousePanelEvent e )
	{
		if ( !_collapsible ) return;

		e.StopPropagation();
		Blackboard.ToggleGroup( IsGeneral ? "" : Group.Name );
	}

	protected override void OnRightClick( MousePanelEvent e )
	{
		e.StopPropagation();
		Blackboard.OpenGroupMenu( this );
	}

	/// <summary>
	/// A parameter dropped here moves into the group. A group dropped here takes its place: below when it came from
	/// above, above when it came from below. Nothing lands on or moves the General group.
	/// </summary>
	internal override DropMode GetDrop( BlackboardItem source )
	{
		switch ( source )
		{
			case ParameterRow row:
				return BlackboardPanel.GroupTitle( BlackboardPanel.GroupName( row.Parameter ) ).Equals( Group.Name, StringComparison.OrdinalIgnoreCase )
					? DropMode.None
					: DropMode.Onto;

			case ParameterGroupHeader header when header != this && !header.IsGeneral && !IsGeneral:
				var graph = Blackboard.Graph;

				return graph.GetGroupDataIndex( header.Group ) <= graph.GetGroupDataIndex( Group )
					? DropMode.Below
					: DropMode.Above;

			default:
				return DropMode.None;
		}
	}

	protected override void ApplyDrop( BlackboardItem source, DropMode mode )
	{
		switch ( source )
		{
			case ParameterRow row:
				Blackboard.MoveToGroup( row.Parameter, Group.Name );
				break;

			case ParameterGroupHeader header:
				Blackboard.ReorderGroup( header.Group, Blackboard.Graph.GetGroupDataIndex( Group ) );
				break;
		}
	}
}

/// <summary>
/// The text field that stands in for a row's name while it's renamed. Enter or losing focus commits, Escape cancels.
/// </summary>
internal sealed class RenameEntry : Sandbox.UI.TextEntry
{
	private readonly ParameterRow _row;
	private bool _cancelled;
	private bool _selected;

	public RenameEntry( ParameterRow row, string text )
	{
		_row = row;
		AddClass( "rename" );
		Text = text;
	}

	public override void Tick()
	{
		base.Tick();

		// Selecting before the label has had a frame to lay its text out does nothing
		if ( _selected || !HasFocus ) return;

		_selected = true;
		Label.ShouldDrawSelection = true;
		Label.SetSelection( 0, TextLength );
		CaretPosition = TextLength;
	}

	public override void OnButtonTyped( ButtonEvent e )
	{
		// The base cancels by blurring, and blurring commits, so say so first
		if ( e.Button == "escape" ) _cancelled = true;

		base.OnButtonTyped( e );
	}

	protected override void OnBlur( PanelEvent e )
	{
		base.OnBlur( e );
		_row.EndRename( _cancelled ? null : Text );
	}
}

/// <summary>
/// One parameter: a strip and type label in the type's color, the name, its tags, and the default value.
/// </summary>
internal sealed class ParameterRow : BlackboardItem
{
	private readonly Sandbox.UI.Label _nameLabel;
	private RenameEntry _rename;

	internal BlackboardParameter Parameter { get; }

	/// <summary>
	/// The group data the row was built under.
	/// </summary>
	internal GroupData Group { get; }

	/// <summary>
	/// The group and name the row was built with, to tell when the parameter has moved on and the list is stale.
	/// </summary>
	internal string BuiltGroup { get; }

	/// <inheritdoc cref="BuiltGroup"/>
	internal string BuiltName { get; }

	internal bool IsRenaming => _rename is not null;

	public ParameterRow( BlackboardPanel blackboard, BlackboardParameter parameter, GroupData group ) : base( blackboard )
	{
		Parameter = parameter;
		Group = group;
		BuiltGroup = BlackboardPanel.GroupName( parameter );
		BuiltName = parameter.Name;

		AddClass( "sg-param" );

		var config = TypeConfig( parameter );

		Style.BorderLeftColor = config.Color;

		// Selection tint, shown by .selected. Toggling the row's own background left it magenta.
		Add.Panel( "highlight" ).Style.BackgroundColor = config.Color.WithAlpha( 0.2f );

		AddTypeChip( this, config );

		_nameLabel = Add.Label( parameter.Name, "name" );

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

		tip.Add.Label( "Drag onto the graph to create a node, or onto another parameter to reorder. Double-click or F2 to rename. Right-click for more.", "hint" );
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

	/// <summary>
	/// Dragged onto the graph, the row becomes a node for its parameter; dragged within the blackboard
	/// it moves. While it's being renamed it's a text field, not a drag handle.
	/// </summary>
	public override bool WantsDrag => !IsRenaming;

	protected override void OnMouseDown( MousePanelEvent e )
	{
		base.OnMouseDown( e );

		if ( e.Button == "mouseleft" )
			Blackboard.Select( Parameter );
	}

	protected override void OnDoubleClick( MousePanelEvent e )
	{
		if ( e.Button != "mouseleft" ) return;

		e.StopPropagation();
		StartRename();
	}

	protected override void OnRightClick( MousePanelEvent e )
	{
		e.StopPropagation();
		Blackboard.Select( Parameter );
		Blackboard.OpenParameterMenu( this );
	}

	/// <summary>
	/// Swap the name for a text field until Enter, Escape, or focus loss.
	/// </summary>
	internal void StartRename()
	{
		if ( IsRenaming ) return;

		_rename = new RenameEntry( this, Parameter.Name );
		AddChild( _rename );
		SetChildIndex( _rename, GetChildIndex( _nameLabel ) + 1 );
		AddClass( "renaming" );

		_rename.Focus();
	}

	/// <summary>
	/// Put the name back. <paramref name="text"/> is what to rename the parameter to, or null to keep its name.
	/// </summary>
	internal void EndRename( string text )
	{
		if ( _rename is null ) return;

		var entry = _rename;
		_rename = null;
		entry.Delete( true );
		RemoveClass( "renaming" );

		if ( text is not null )
			Blackboard.Rename( Parameter, text );
	}

	/// <summary>
	/// Same group: the dragged row takes the target's slot, so it lands below when it came from above and above
	/// when it came from below. Another group: below when that group is further down, above when it's further up.
	/// </summary>
	internal override DropMode GetDrop( BlackboardItem source )
	{
		if ( source is not ParameterRow dragged || dragged.Parameter == Parameter )
			return DropMode.None;

		if ( dragged.Group.Name.Equals( Group.Name, StringComparison.OrdinalIgnoreCase ) )
		{
			var references = Group.ParameterReferences;

			return references.IndexOf( dragged.Parameter.Identifier ) < references.IndexOf( Parameter.Identifier )
				? DropMode.Below
				: DropMode.Above;
		}

		var graph = Blackboard.Graph;

		return graph.GetGroupDataIndex( Group ) > graph.GetGroupDataIndex( dragged.Group )
			? DropMode.Below
			: DropMode.Above;
	}

	protected override void ApplyDrop( BlackboardItem source, DropMode mode )
	{
		if ( source is ParameterRow dragged )
			Blackboard.DropParameter( dragged, this, mode );
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
			thumb.Style.BackgroundImage = Texture.Load( $"{texture.DefaultTexture}" );
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
