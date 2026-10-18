
namespace ShaderGraphPlusPanelTest;

public static class ShaderGraphPlusStyleSheets
{

	/// <summary>
	/// Found in : sbox\addons\editor\assets\styles
	/// </summary>
	public const string EditorScss = "/styles/editor.scss";

	/// <summary>
	/// Found in : sbox\addons\editor\assets\styles
	/// </summary>
	public const string GalleryScss = "/styles/gallery.scss";

	public const string ShaderGraphPlusScss = @"
		ShaderGraphPlusPanel > .main-toolbar { flex-shrink: 0; height: 40px; padding: 0px 8px; border-bottom: 1px solid #26282e; }
		ShaderGraphPlusPanel .status { color: #6e737c; font-size: 11px; align-self: center; }

		.sg-graph { background-color: #1b1d22; }

		.sg-node { position: absolute; flex-direction: column; min-width: 160px; min-height: 48px; background-color: #25282f; border: 1px solid #3a3e47; border-radius: 6px; }
		.sg-node > .header { flex-direction: row; align-items: center; gap: 6px; height: 28px; padding: 0px 10px; background-color: #323743; border-radius: 6px 6px 0px 0px; cursor: move; }
		.sg-node > .header > * { pointer-events: none; }
		.sg-node > .header > .icon { font-size: 15px; color: #8a8f98; }
		.sg-node > .header > .title { font-size: 12px; font-weight: 600; white-space: nowrap; }
		.sg-node.preview { opacity: 0.45; border-color: #4b8bf5; }

		.console > .body { flex-direction: row; flex-grow: 1; min-height: 0; }
		.console > .body > .list { min-width: 0; }
		.console > .body > .detail-splitter { width: 5px; flex-shrink: 0; cursor: ew-resize; background-color: #1a1c21; }
		.console > .body > .detail-splitter:hover { background-color: #4b8bf5; }
		.console > .body > .detail-splitter.dragging { background-color: #4b8bf5; }
		.console > .body > .detail { flex-shrink: 0; height: 100%; padding: 0px; overflow: hidden; border-top-width: 0px; }
		.console .detail-header { flex-direction: row; align-items: center; flex-shrink: 0; height: 26px; padding: 0px 4px 0px 10px; border-bottom: 1px solid #26282e; }
		.console .detail-title { flex-grow: 1; color: #8a8f98; font-size: 11px; white-space: nowrap; overflow: hidden; }
		.console .detail-body { flex-direction: column; flex-grow: 1; min-height: 0; overflow: scroll; padding: 7px 10px; }

		.palette-tree { flex-grow: 1; flex-shrink: 1; min-height: 0; }

		.sg-blackboard { flex-direction: column; }
		.sg-params { flex-direction: column; flex-grow: 1; flex-shrink: 1; min-height: 0; overflow: scroll; padding: 4px; gap: 2px; pointer-events: all; }
		.sg-params.is-empty { align-items: center; justify-content: center; }
		.sg-params > .empty { padding: 12px; color: #6e737c; font-size: 11px; text-align: center; }
		.sg-param { position: relative; flex-direction: row; align-items: center; flex-shrink: 0; gap: 8px; min-height: 28px; padding: 0px 8px; border-left-width: 3px; border-radius: 4px; background-color: #23262c; pointer-events: all; }
		.sg-param:hover { background-color: #2a2e36; }
		.sg-param.selected > .name { color: #ffffff; }
		.sg-param > .highlight { position: absolute; top: 0px; left: 0px; right: 0px; bottom: 0px; border-radius: 0px 4px 4px 0px; pointer-events: none; display: none; }
		.sg-param.selected > .highlight { display: flex; }
		.sg-type { flex-direction: row; align-items: center; justify-content: center; flex-shrink: 0; gap: 5px; height: 18px; padding: 0px 6px; border-width: 1px; border-radius: 3px; }
		.sg-type > .dot { flex-shrink: 0; width: 6px; height: 6px; border-radius: 3px; }
		.sg-type > .text { font-size: 11px; font-weight: 600; white-space: nowrap; }
		.sg-param > .name { flex-grow: 1; flex-shrink: 1; min-width: 0; font-size: 12px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
		.sg-param > .tags { flex-direction: row; flex-shrink: 0; gap: 4px; }
		.sg-param > .tags > .tag { padding: 0px 5px; border-radius: 3px; background-color: #31353e; color: #9aa0aa; font-size: 10px; white-space: nowrap; }
		.sg-param > .value { flex-direction: row; align-items: center; flex-shrink: 1; min-width: 0; gap: 5px; }
		.sg-param > .value > .text { color: #c4c8cf; font-size: 11px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
		.sg-param > .value > .swatch { flex-shrink: 0; width: 12px; height: 12px; border-radius: 2px; border: 1px solid #00000080; }
		.sg-param > .value > .thumb { flex-shrink: 0; width: 22px; height: 22px; border-radius: 3px; background-size: cover; background-position: center; background-color: #ffffff0d; }

		.sg-tip { flex-direction: column; gap: 6px; max-width: 320px; }
		.sg-tip > .head { flex-direction: row; align-items: center; gap: 8px; }
		.sg-tip > .head > .name { font-size: 13px; font-weight: 600; }
		.sg-tip > .description { color: #8a8f98; font-size: 11px; }
		.sg-tip > .preview { width: 96px; height: 96px; border-radius: 4px; background-size: cover; background-position: center; background-color: #ffffff0d; }
		.sg-tip > .detail { flex-direction: row; align-items: center; gap: 8px; font-size: 11px; }
		.sg-tip > .detail > .key { flex-shrink: 0; width: 52px; color: #8a8f98; }
		.sg-tip > .detail > .text, .sg-tip .value > .text { color: #c4c8cf; }
		.sg-tip .value { flex-direction: row; align-items: center; gap: 5px; }
		.sg-tip .value > .swatch { flex-shrink: 0; width: 12px; height: 12px; border-radius: 2px; border: 1px solid #00000080; }
		.sg-tip > .tags { flex-direction: row; flex-wrap: wrap; gap: 4px; }
		.sg-tip > .tags > .tag { padding: 0px 5px; border-radius: 3px; background-color: #31353e; color: #9aa0aa; font-size: 10px; }
		.sg-tip > .hint { color: #6e737c; font-size: 10px; }

		.sg-list { flex-direction: column; flex-grow: 1; padding: 4px 0px; }
		.sg-list .category { padding: 8px 10px 4px 10px; color: #8a8f98; font-size: 11px; font-weight: 600; text-transform: uppercase; }
		.sg-list .item { height: 26px; align-items: center; gap: 8px; padding: 0px 14px; }
		.sg-list .item > .icon { font-size: 15px; color: #8a8f98; }
		
		.sg-preview { flex-grow: 1; align-items: center; justify-content: center; background-color: #121317; }
		.sg-preview > .ball { width: 160px; height: 160px; border-radius: 80px; background-color: #d9773a; }
		
		.sg-props { flex-direction: column; flex-grow: 1; }
		.sg-props > .heading { flex-direction: column; flex-shrink: 0; gap: 4px; padding: 10px 12px; border-bottom: 1px solid #26282e; }
		.sg-props > .heading > .row { flex-direction: row; align-items: center; gap: 8px; }
		.sg-props > .heading > .row > .title { flex-shrink: 1; min-width: 0; font-size: 14px; font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
		.sg-props > .heading > .row > .icon { flex-shrink: 0; font-size: 16px; color: #8a8f98; }
		.sg-props > .heading > .subtitle { color: #8a8f98; font-size: 11px; }
		.sg-props > .body { flex-direction: column; flex-grow: 1; flex-shrink: 1; min-height: 0; overflow: scroll; padding: 4px 6px 12px 6px; }
		.sg-props > .body > * { flex-shrink: 0; }
		.sg-props > .body.is-empty { align-items: center; justify-content: center; }
		.sg-props > .body > .empty { padding: 12px; color: #6e737c; font-size: 11px; text-align: center; }
		.sg-props .controlgroup { padding: 0px; border-radius: 0px; }
		.sg-props .controlgroup > .body { gap: 2px; }
		.sg-props .controlgroup > .header { flex-shrink: 0; margin-top: 10px; padding: 0px 2px 2px 0px; color: #c4c8cf; font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; text-shadow: none; }
		.sg-props GenericControl .controlgroup > .header, .sg-props GenericControl > .title { margin-top: 6px; color: #8a8f98; font-weight: 600; text-transform: none; letter-spacing: 0px; }
		.sg-props ControlSheetRow.wide { align-items: stretch; }
		.sg-props ControlSheet { font-size: 12px; }
		.sg-props ControlSheetRow { align-items: center; min-height: 28px; }
		.sg-props ControlSheetRow > .left { width: 110px; padding: 4px 6px; }
		.sg-props ControlSheetRow > .left > .title { width: 98px; font-size: 12px; color: #b4b9c2; text-overflow: ellipsis; }
		.sg-props .pin { height: 26px; align-items: center; gap: 8px; padding: 0px 12px; }
	";
}
