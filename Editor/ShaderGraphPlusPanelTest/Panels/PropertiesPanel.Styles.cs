namespace ShaderGraphPlusPanelTest;

public sealed partial class PropertiesPanel
{
	const string Styles = """
		.sg-props { flex-direction: column; flex-grow: 1; }
		.sg-props > .heading
		{
			flex-direction: column;
			flex-shrink: 0;
			gap: 4px;
			padding: 10px 12px;
			border-bottom: 1px solid #26282e;
			}
		.sg-props > .heading > .row { flex-direction: row; align-items: center; gap: 8px; }
		.sg-props > .heading > .row > .title
		{
			flex-shrink: 1;
			min-width: 0;
			font-size: 14px;
			font-weight: 600;
			white-space: nowrap;
			overflow: hidden;
			ext-overflow: ellipsis;
		}
		.sg-props > .heading > .row > .icon { flex-shrink: 0; font-size: 16px; color: #8a8f98; }
		.sg-props > .heading > .subtitle { color: #8a8f98; font-size: 11px; }
		.sg-props > .body
		{
			flex-direction: column;
			flex-grow: 1;
			flex-shrink: 1;
			min-height: 0;
			overflow: scroll;
			padding: 4px 6px 12px 6px;
		}
		.sg-props > .body > * { flex-shrink: 0; }
		.sg-props > .body.is-empty { align-items: center; justify-content: center; }
		.sg-props > .body > .empty { padding: 12px; color: #6e737c; font-size: 11px; text-align: center; }
		.sg-props .controlgroup { padding: 0px; border-radius: 0px; }
		.sg-props .controlgroup > .body { gap: 2px; }
		.sg-props GenericControl > .title { margin-top: 6px; color: #8a8f98; font-weight: 600; }
		.sg-props ControlSheetRow.wide { align-items: stretch; }
		.sg-props ControlSheet { font-size: 12px; }
		.sg-props ControlSheetRow { align-items: center; min-height: 28px; }
		.sg-props ControlSheetRow > .left { width: 110px; padding: 4px 6px; }
		.sg-props ControlSheetRow > .left > .title { font-size: 12px; color: #b4b9c2; }
		.sg-props ControlSheetGroupHeader > .lead { width: 104px; }
		.sg-props ControlSheetGroupHeader.has-toggle > .lead { width: 76px; }
		.sg-props .pin { height: 26px; align-items: center; gap: 8px; padding: 0px 12px; }
	""";
}
