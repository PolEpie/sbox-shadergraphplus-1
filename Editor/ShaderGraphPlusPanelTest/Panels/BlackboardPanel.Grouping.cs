using Editor;
using Sandbox.Rendering;
using Sandbox.UI;
using Sandbox.UI.Construct;
using ShaderGraphPlus;
using System.Globalization;

using static ShaderGraphPlus.ShaderGraphPlusGlobals;

namespace ShaderGraphPlusPanelTest;

public sealed partial class BlackboardPanel
{
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

	private int GroupIndex( string groupName )
		=> Graph.TryFindGroupData( GroupTitle( groupName ), out var groupData ) ? Graph.GetGroupDataIndex( groupData ) : int.MaxValue;

	private IOrderedEnumerable<IGrouping<string, IGroupableBlackboardParameter>> OrderedGroups( IEnumerable<IBlackboardParameter> parameters )
	{
		return parameters.OfType<IGroupableBlackboardParameter>()
			.Where( MatchesFilter )
			.OrderBy( p => Graph.GetParameterIndexInGroup( GroupName( p ), p.Identifier ) )
			.GroupBy( GroupName, StringComparer.OrdinalIgnoreCase )
			.OrderBy( group => GroupIndex( group.Key ) );
	}

	private IOrderedEnumerable<string> ExistingGroups() => Graph.Parameters.OfType<IGroupableBlackboardParameter>()
	.Select( GroupName )
	.Where( group => !string.IsNullOrEmpty( group ) )
	.Distinct( StringComparer.OrdinalIgnoreCase )
	.OrderBy( group => group, StringComparer.OrdinalIgnoreCase );

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
}
