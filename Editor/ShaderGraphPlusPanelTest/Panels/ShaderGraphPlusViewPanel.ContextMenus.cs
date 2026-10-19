using EngineNodeGraph;
using NodeEditorPlus;
using NodeEditorPlusTest;
using Sandbox.Internal;
using Sandbox.UI;
using ShaderGraphPlus;
using ShaderGraphPlus.Nodes;

namespace ShaderGraphPlusPanelTest;

public sealed partial class ShaderGraphPlusViewPanel
{
	protected override void BuildContextMenu( Sandbox.UI.Menu menu, EngineNodeGraph.NodePanel node, Vector2 position )
	{
		base.BuildContextMenu( menu, node, position );

		void ConvertConstants( IReadOnlyList<IConstantNode> constants )
		{
			BaseNodePlus last = null;

			foreach ( var constant in constants )
			{
				last = ConvertConstantToParameter( constant );
			}

			RebuildFromGraph();
			_blackboard.RebuildFromGraph();

			if ( last is not null && FindNode( last ) is { } panel ) SelectNode( panel );
		}

		if ( _blackboard is null || Graph is null )
			return;

		var selectedNodes = SelectedNodes.Select( x => x.Node ).ToArray();
		var isSubgraph = Graph.IsSubgraph;

		if ( selectedNodes.Length >= 1 )
		{
			if ( selectedNodes.All( x => x is IConstantNode && x is not IConstantMatrixNode ) )
			{
				var constantNodes = selectedNodes.OfType<IConstantNode>().Where( x => x is not IConstantMatrixNode ).ToArray();

				if ( constantNodes.Length == 0 || constantNodes.Length != selectedNodes.Length )
					return;

				var parameterType = Graph.IsSubgraph ? "Subgraph Input" : "Material Parameter";
				var optionType = constantNodes.Length == 1 ? $"Convert Constant to {parameterType}" : $"Convert {constantNodes.Length} Constants to {parameterType}s";

				if ( menu.HasOptions )
					menu.AddSeparator();

				menu.AddOption( optionType, "swap_horiz", () => ConvertConstants( constantNodes ) );
			}
		}
		else
		{
			CreateParameterOptions( menu, position, isSubgraph );

			if ( menu.HasOptions )
				menu.AddSeparator();

			CreateNodeOptions( menu, position, null );
		}
	}

	/// <summary>
	/// A wire dropped on nothing offers the nodes that can take it, wired up once created.
	/// </summary>
	private void BuildConnectionDropContextMenu( PlugPanel plug, Vector2 position )
	{
		if ( Graph is null ) return;

		OpenMenu( menu =>
		{
			if ( RerouteNodeType is not null )
			{
				menu.AddOption( "Add Reroute", "route", () => CreateNewNode( RerouteNodeType, position, plug ) );
				menu.AddSeparator();
			}

			if ( plug is PlugOutPanel plugOut )
			{
				CreateNamedRerouteDeclarationOption( menu, position, plugOut );
			}
			else if ( plug is PlugInPanel plugIn )
			{
				CreateNamedRerouteOptions( menu, position, plugIn );
			}

			CreateNodeOptions( menu, position, plug );
		} );
	}

	private void CreateNamedRerouteDeclarationOption( Menu menu, Vector2 position, PlugOutPanel plugOut )
	{
		menu.AddOption( "Add Named Reroute Declaration", "route", () =>
		{
			Editor.Dialog.AskString( ( string namedRerouteName ) =>
			{
				if ( Graph.Nodes.OfType<NamedRerouteDeclarationNode>().Any( x => x.Name == namedRerouteName ) )
				{
					SGPLogger.Error( $"Named Reroute \"{namedRerouteName}\" already exists!" );
				}
				else
				{
					var nodeType = new NamedRerouteDeclarationNodeType( EditorTypeLibrary.GetType<NamedRerouteDeclarationNode>(), namedRerouteName );
					CreateNewNode( nodeType, position, plugOut );
				}
			},
			"Specify a Named Reroute name" );
		} );
	}

	private void CreateNamedRerouteOptions( Menu menu, Vector2 position, PlugInPanel plugIn )
	{
		var namedRerouteDeclarations = Graph.Nodes.OfType<NamedRerouteDeclarationNode>();

		if ( namedRerouteDeclarations.Any() )
		{
			var optionsMenu = menu.AddMenu( "Named Reroutes", "route" );

			foreach ( var namedRerouteDeclaration in namedRerouteDeclarations )
			{
				optionsMenu.AddOption( namedRerouteDeclaration.Name, "route", () =>
				{
					var nodeType = new NamedRerouteNodeType( EditorTypeLibrary.GetType<NamedRerouteNode>(), namedRerouteDeclaration.Name );

					CreateNewNode( nodeType, position, plugIn );
				} );
			}
		}
	}

	private BaseNodePlus ConvertConstantToParameter( IConstantNode constant )
	{
		var baseNode = (BaseNodePlus)constant;
		var position = baseNode.Position;
		var connected = Graph.IsSubgraph ? new Dictionary<IPlugIn, IPlugOut>() : ConnectedFrom( baseNode );

		var parameterTypeName = Graph.IsSubgraph
			? constant switch
			{
				BoolConstantNode => DisplayInfo.ForType( typeof( BoolSubgraphInputParameter ) ).Fullname,
				IntConstantNode => DisplayInfo.ForType( typeof( IntSubgraphInputParameter ) ).Fullname,
				FloatConstantNode => DisplayInfo.ForType( typeof( FloatSubgraphInputParameter ) ).Fullname,
				Float2ConstantNode => DisplayInfo.ForType( typeof( Float2SubgraphInputParameter ) ).Fullname,
				Float3ConstantNode => DisplayInfo.ForType( typeof( Float3SubgraphInputParameter ) ).Fullname,
				Float4ConstantNode => DisplayInfo.ForType( typeof( Float4SubgraphInputParameter ) ).Fullname,
				ColorConstantNode => DisplayInfo.ForType( typeof( ColorSubgraphInputParameter ) ).Fullname,
				_ => null,
			}
			: constant switch
			{
				BoolConstantNode => DisplayInfo.ForType( typeof( BoolParameter ) ).Fullname,
				IntConstantNode => DisplayInfo.ForType( typeof( IntParameter ) ).Fullname,
				FloatConstantNode => DisplayInfo.ForType( typeof( FloatParameter ) ).Fullname,
				Float2ConstantNode => DisplayInfo.ForType( typeof( Float2Parameter ) ).Fullname,
				Float3ConstantNode => DisplayInfo.ForType( typeof( Float3Parameter ) ).Fullname,
				Float4ConstantNode => DisplayInfo.ForType( typeof( Float4Parameter ) ).Fullname,
				ColorConstantNode => DisplayInfo.ForType( typeof( ColorParameter ) ).Fullname,
				_ => null,
			};

		if ( parameterTypeName is null || !AvailableParameters.TryGetValue( parameterTypeName, out var parameterType ) )
		{
			SGPLogger.Warning( $"No parameter type for {constant.GetType().Name}" );
			return baseNode;
		}

		Graph.RemoveNode( baseNode );

		var parameter = _blackboard.CreateNewParameter( parameterType );
		parameter.SetValue( constant.GetValue() );

		var parameterNode = (BaseNodePlus)parameter.ToNode();
		parameterNode.Graph = Graph;
		parameterNode.Position = position.SnapToGrid( GridSize );
		Graph.AddNode( parameterNode );

		// Whatever the constant fed, the parameter feeds now
		foreach ( var node in Graph.Nodes )
		{
			foreach ( var input in node.Inputs )
			{
				if ( input.ConnectedOutput is null && connected.TryGetValue( input, out var output ) )
				{
					node.ConnectNode( input.Identifier, output.Identifier, parameterNode.Identifier );
				}
			}
		}

		return parameterNode;
	}

	private void CreateParameterOptions( Sandbox.UI.Menu menu, Vector2 position, bool isSubgraph )
	{
		void NewParameterMenuOption( Menu menu, IBlackboardParameterType classType, string undoScopeName )
		{
			var option = menu.AddOption( classType.Type.Title, classType.Type.Icon, () =>
			{
				//using var undoScope = UndoScope( undoScopeName );

				var parameter = CreateNewParameter( classType );
				var node = CreateNewParameterNode( parameter, position );

				SelectNode( node );
			} );
		}

		if ( !isSubgraph )
		{
			var createParameterMenu = menu.AddMenu( "Create Parameter", "add" );

			var newMaterialParameterMenu = createParameterMenu.AddMenu( $"Material", "edit_attributes" );
			var newAttributeMenu = createParameterMenu.AddMenu( $"Attribute", "edit_attributes" );
			var newComboMenu = createParameterMenu.AddMenu( $"Combo", "alt_route" );

			var parameterTypes = BlackboardParameter.GetRelevantParameters( AvailableParameters, false ).OrderBy( x =>
				x.Type.GetAttribute<OrderAttribute>().Value );

			foreach ( var classType in parameterTypes )
			{
				if ( classType.Type.TargetType.IsAssignableTo( typeof( IBlackboardShaderFeatureParameter ) ) )
				{
					NewParameterMenuOption( newComboMenu, classType, "Add Material Shader Feature" );
				}
				if ( classType.Type.TargetType == typeof( SamplerStateParameter ) )
				{
					NewParameterMenuOption( newAttributeMenu, classType, "Add Attribute Parameter" );
				}
				else if ( !classType.Type.TargetType.IsAssignableTo( typeof( IBlackboardShaderFeatureParameter ) ) )
				{
					NewParameterMenuOption( newMaterialParameterMenu, classType, "Add Material Parameter" );
				}
			}
		}
		else
		{
			var newSubgraphInputParameterMenu = menu.AddMenu( $"Create Subgraph Input", "add" );

			var inputParameterTypes = BlackboardParameter.GetRelevantParameters( AvailableParameters, true ).Where( x =>
				x.Type.TargetType.IsAssignableTo( typeof( IBlackboardSubgraphInputParameter ) ) ).OrderBy( x =>
					x.Type.GetAttribute<OrderAttribute>().Value );

			foreach ( var classType in inputParameterTypes )
			{
				NewParameterMenuOption( newSubgraphInputParameterMenu, classType, "Add Subgraph Input Parameter" );
			}

			var newSubgraphOutputParameterMenu = menu.AddMenu( $"Create Subgraph Output", "add" );

			var outputParameterTypes = BlackboardParameter.GetRelevantParameters( AvailableParameters, true ).Where( x =>
				x.Type.TargetType.IsAssignableTo( typeof( IBlackboardSubgraphOutputParameter ) ) ).OrderBy( x =>
					x.Type.GetAttribute<OrderAttribute>().Value );

			foreach ( var classType in outputParameterTypes )
			{
				NewParameterMenuOption( newSubgraphOutputParameterMenu, classType, "Add Subgraph Output Parameter" );
			}
		}
	}

	/// <summary>
	/// A filter box, then the nodes <see cref="GetRelevantNodes"/> offers for <paramref name="plug"/>:
	/// in their categories, or the best matches flat while filtering.
	/// </summary>
	void CreateNodeOptions( Sandbox.UI.Menu menu, Vector2 position, PlugPanel plug )
	{
		menu = menu.AddMenu( "Create Node", "add" );

		var results = new List<Panel>();
		var filter = menu.AddWidget( new TextEntry { Placeholder = "Filter Nodes.." } );

		void Fill( string text )
		{
			foreach ( var row in results )
			{
				menu.Remove( row );
				row.Delete( true );
			}

			results.Clear();

			var query = new NodeQuery( Graph, plug?.Inner as IPlug, text );
			var types = GetRelevantNodes( query ).ToArray();

			if ( query.Filter.Count > 0 )
			{
				foreach ( var type in types.OrderByDescending( x => x.GetScore( query ) ?? 0 ).Take( MaxFilteredResults ) )
				{
					var name = string.Join( " → ", type.Path.Where( x => !x.IsHeading ).Select( x => x.Name ) );
					results.Add( menu.AddOption( name, type.Path[^1].Icon, () => CreateNewNode( type, position, plug ) ) );
				}

				if ( types.Length > MaxFilteredResults )
				{
					results.Add( menu.AddWidget( new Label( $"... and {types.Length - MaxFilteredResults} more" ) ) );
				}

				return;
			}

			foreach ( var type in types.OrderBy( x => string.Join( "/", x.Path.Select( p => p.Name ) ), StringComparer.OrdinalIgnoreCase ) )
			{
				var parent = menu;

				foreach ( var part in type.Path[..^1] )
				{
					if ( part.IsHeading ) continue;

					if ( parent.FindOption( part.Name ) is not { } submenu )
					{
						submenu = parent.AddMenu( part.Name, part.Icon );
						if ( parent == menu ) results.Add( submenu );
					}

					parent = submenu;
				}

				var option = parent.AddOption( type.Path[^1].Name, type.Path[^1].Icon, () => CreateNewNode( type, position, plug ) );
				if ( parent == menu ) results.Add( option );
			}
		}

		filter.OnTextEdited = Fill;
		menu.AboutToShow += _ => filter.Focus();
		Fill( null );
	}
}
