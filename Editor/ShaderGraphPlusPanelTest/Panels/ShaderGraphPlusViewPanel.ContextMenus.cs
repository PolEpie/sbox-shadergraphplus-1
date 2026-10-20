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
	/// <summary>
	/// Offers to convert the selected constants to parameters, like the Qt view's. Not for a wire
	/// dropped from a plug, whose menu is about what that wire can take.
	/// </summary>
	protected override void OnOpenContextMenu( Sandbox.UI.Menu menu, PlugPanel targetPlug )
	{
		base.OnOpenContextMenu( menu, targetPlug );

		if ( _blackboard is null || Graph is null || targetPlug.IsValid() )
			return;

		var selectedNodes = SelectedNodes.Select( x => x.Node ).ToArray();

		if ( selectedNodes.Length == 0 || !selectedNodes.All( x => x is IConstantNode && x is not IConstantMatrixNode ) )
			return;

		var constantNodes = selectedNodes.OfType<IConstantNode>().ToArray();
		var parameterType = Graph.IsSubgraph ? "Subgraph Input" : "Material Parameter";
		var optionName = constantNodes.Length == 1 ? $"Convert Constant to {parameterType}" : $"Convert {constantNodes.Length} Constants to {parameterType}s";

		if ( menu.HasOptions )
			menu.AddSeparator();

		menu.AddOption( optionName, "swap_horiz", () =>
		{
			BaseNodePlus last = null;

			foreach ( var constant in constantNodes )
			{
				last = ConvertConstantToParameter( constant );
			}

			RebuildFromGraph();
			_blackboard.RebuildFromGraph();

			SelectNode( last );
		} );
	}

	/// <summary>
	/// Same entries as the Qt view's after the engine's: on empty space the parameters to create and
	/// the named reroutes to place; for a wire dropped from a plug, the named reroute that fits it.
	/// </summary>
	protected override void OnPopulateNodeMenuSpecialOptions( Sandbox.UI.Menu menu, Vector2 position, PlugPanel targetPlug, string filter )
	{
		base.OnPopulateNodeMenuSpecialOptions( menu, position, targetPlug, filter );

		if ( _blackboard is null || Graph is null )
			return;

		if ( !targetPlug.IsValid() )
		{
			CreateParameterOptions( menu, position, Graph.IsSubgraph );
			CreateNamedRerouteOptions( menu, position, null );
		}
		else if ( targetPlug is PlugInPanel plugIn )
		{
			CreateNamedRerouteOptions( menu, position, plugIn );
		}
		else if ( targetPlug is PlugOutPanel plugOut )
		{
			CreateNamedRerouteDeclarationOption( menu, position, plugOut );
		}

		menu.AddSeparator();
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
					CreateNewNode( new PanelNodeType( nodeType ), position, plugOut );
				}
			},
			"Specify a Named Reroute name" );
		} );
	}

	/// <summary>
	/// The named reroutes to place: wired to <paramref name="plug"/>, or on their own when it's null.
	/// </summary>
	private void CreateNamedRerouteOptions( Menu menu, Vector2 position, PlugPanel plug )
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

					CreateNewNode( new PanelNodeType( nodeType ), position, plug );
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
}
