namespace ShaderGraphPlusPanelTest;

public sealed partial class BlackboardPanel
{
	const string Styles = """
		.sg-blackboard { flex-direction: column; }
		.sg-type
		{
			flex-direction: row;
			align-items: center;
			justify-content: center;
			flex-shrink: 0; gap: 5px;
			height: 18px;
			padding: 0px 6px;
			border-width: 1px;
			border-radius: 3px;
		}
		.sg-type > .dot { flex-shrink: 0; width: 6px; height: 6px; border-radius: 3px; }
		.sg-type > .text { font-size: 11px; font-weight: 600; white-space: nowrap; }
		.sg-params
		{
			flex-direction: column;
			flex-grow: 1;
			flex-shrink: 1;
			min-height: 0;
			overflow: scroll;
			padding: 4px;
			gap: 2px;
			pointer-events: all;
		}
		.sg-params.is-empty { align-items: center; justify-content: center; }
		.sg-params > .empty { padding: 12px; color: #6e737c; font-size: 11px; text-align: center; }
		.sg-param
		{
			position: relative;
			flex-direction: row;
			align-items: center;
			flex-shrink: 0;
			gap: 8px;
			min-height: 28px;
			padding: 0px 8px;
			border-left-width: 3px;
			border-radius: 4px;
			background-color: #23262c;
			pointer-events: all;
		}
		.sg-param:hover { background-color: #2a2e36; }
		.sg-param.selected > .name { color: #ffffff; }
		.sg-param > .highlight
		{
			position: absolute;
			top: 0px;
			left: 0px;
			right: 0px;
			bottom: 0px;
			border-radius: 0px 4px 4px 0px;
			pointer-events: none;
			display: none;
		}
		.sg-param.selected > .highlight { display: flex; }
		.sg-param > .name
		{
			flex-grow: 1;
			flex-shrink: 1;
			min-width: 0;
			font-size: 12px;
			white-space: nowrap;
			overflow: hidden;
			text-overflow: ellipsis;
		}
		.sg-param > .tags { flex-direction: row; flex-shrink: 0; gap: 4px; }
		.sg-param > .tags > .tag
		{
			padding: 0px 5px;
			border-radius: 3px;
			background-color: #31353e;
			color: #9aa0aa;
			font-size: 10px;
			white-space: nowrap;
		}
		.sg-param > .value { flex-direction: row; align-items: center; flex-shrink: 1; min-width: 0; gap: 5px; }
		.sg-param > .value > .text { color: #c4c8cf; font-size: 11px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
		.sg-param > .value > .swatch { flex-shrink: 0; width: 12px; height: 12px; border-radius: 2px; border: 1px solid #00000080; }
		.sg-param > .value > .thumb
		{
			flex-shrink: 0;
			width: 22px;
			height: 22px;
			border-radius: 3px;
			background-size: cover;
			background-position: center;
			background-color: #ffffff0d;
		}
		.sg-param.dragging, .sg-group.dragging { opacity: 0.5; }
		.sg-param > .drop, .sg-group > .drop
		{
			position: absolute;
			left: 0px;
			right: 0px;
			height: 2px;
			border-radius: 1px;
			background-color: #4b8bf5;
			pointer-events: none;
			display: none;
		}
		.sg-param.drop-above > .drop, .sg-group.drop-above > .drop { display: flex; top: -2px; }
		.sg-param.drop-below > .drop, .sg-group.drop-below > .drop { display: flex; bottom: -2px; }
		.sg-param.drop-onto > .drop, .sg-group.drop-onto > .drop
		{
			display: flex;
			top: 0px;
			bottom: 0px;
			height: 100%;
			border-radius: 4px;
			border: 1px solid #4b8bf5;
			background-color: #4b8bf526;
		}
		.sg-param.renaming > .name { display: none; }
		.sg-param > .rename
		{
			flex-grow: 1;
			flex-shrink: 1;
			min-width: 0;
			height: 20px;
			padding: 0px 4px;
			border-radius: 3px;
			border: 1px solid #4b8bf5;
			background-color: #14161a;
			color: #ffffff;
			font-size: 12px; }
		.sg-group
		{
			position: relative;
			flex-direction: row;
			align-items: center;
			flex-shrink: 0;
			gap: 6px;
			height: 26px;
			margin-top: 4px;
			padding: 0px 8px 0px 4px;
			border-radius: 4px;
			background-color: #1d2025;
			cursor: pointer;
			pointer-events: all;
		}
		.sg-group:hover { background-color: #262a31; }
		.sg-group.fixed { cursor: default; }
		.sg-group > .arrow { flex-shrink: 0; font-size: 16px; color: #8a8f98; }
		.sg-group.fixed > .arrow { opacity: 0.25; }
		.sg-group > .folder { flex-shrink: 0; font-size: 15px; color: #4b8bf5; }
		.sg-group > .title
		{
			flex-grow: 1;
			flex-shrink: 1;
			min-width: 0;
			color: #c4c8cf;
			font-size: 12px;
			font-weight: 600;
			white-space: nowrap;
			overflow: hidden;
			text-overflow: ellipsis;
		}
		.sg-group > .count
		{
			flex-shrink: 0;
			min-width: 18px;
			padding: 0px 5px;
			border-radius: 3px;
			background-color: #ffffff14;
			color: #8a8f98;
			font-size: 10px;
			text-align: center;
		}
		.sg-group-body
		{
			flex-direction: column;
			flex-shrink: 0;
			gap: 2px;
			padding: 2px 0px 2px 10px;
			pointer-events: all;
		}
	""";
}
