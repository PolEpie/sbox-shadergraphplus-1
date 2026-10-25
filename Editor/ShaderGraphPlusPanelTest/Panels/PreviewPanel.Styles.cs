namespace ShaderGraphPlusPanelTest;

public sealed partial class PreviewPanel
{
	const string Styles = """
		.sg-preview
		{
			position: relative;
			flex-grow: 1;
			min-width: 0px;
			min-height: 0px;
			pointer-events: all;
			overflow: hidden;
			background-color: #121317;
		}
		.sg-preview > .scene
		{ 
			position: absolute;
			left: 0px;
			top: 0px;
			width: 100%;
			height: 100%;
		}
		.sg-preview > .overlay
		{
			position: absolute;
			left: 0px;
			top: 0px;
			right: 0px;
			padding: 8px 10px;
			flex-direction: row;
			align-items: center;
			gap: 4px;
		}
		.sg-preview > .overlay > .spacer { flex-grow: 1; }
		.sg-preview > .overlay > .separator
		{
			flex-shrink: 0;
			width: 1px;
			height: 16px;
			margin: 0px 4px;
			background-color: #3a3d45;
		}
		.sg-preview > .overlay > .iconbutton
		{
			flex-shrink: 0;
			width: 28px;
			height: 28px;
			border-radius: 6px;
			border: 1px solid #2f323a;
			background-color: #1a1c21;
			color: #c4c8cf;
		}
		.sg-preview > .overlay > .iconbutton:hover { background-color: #2a2e36; color: #ffffff; }
		.sg-preview > .overlay > .iconbutton .icon { font-size: 18px; color: #c4c8cf; }
		.sg-preview > .overlay > .iconbutton:hover .icon { color: #ffffff; }
		.sg-preview > .overlay > .iconbutton.on { border-color: #4c86f0; background-color: #3273eb; color: #ffffff; }
		.sg-preview > .overlay > .iconbutton.on .icon { color: #ffffff; }
		.sg-preview > .overlay > .iconbutton.speed { width: auto; min-width: 36px; padding: 0px 8px; }
		.sg-preview > .overlay > .iconbutton.speed > .speed-text { color: #c4c8cf; font-size: 12px; font-weight: 600; white-space: nowrap; }
		.sg-preview > .overlay > .iconbutton.speed:hover > .speed-text { color: #ffffff; }
		.sg-preview > .footer { position: absolute; left: 0px; right: 0px; bottom: 0px; padding: 6px 10px; pointer-events: none; }
		.sg-preview > .footer > .hint { color: #8a8f98; font-size: 11px; }
	""";
}
