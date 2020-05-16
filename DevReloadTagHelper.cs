using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Abiosoft.DotNet.DevReload
{
	/// <summary>
	/// Tag helper to inject auto-reload script on webpage.
	/// Add to main layout file.
	/// </summary>
	[HtmlTargetElement("devreload")]
	public class DevReloadTagHelper : TagHelper
	{
		/// <summary>
		/// Processing tag helper
		/// </summary>
		/// <param name="context"></param>
		/// <param name="output"></param>
		public override void Process(TagHelperContext context, TagHelperOutput output)
		{
			if (DevReloadOptions.UseSignalR)
			{
				//Adds <script async src='~/lib/signalr/dist/browser/signalr.js'></script> before
				output.PreElement.AppendHtmlLine($"<script async src='{DevReloadOptions.SignalRClientSide}'></script>");
			}

			output.TagName = "script";    // Replaces <devreload> with <script async src="...."> tag

			output.Attributes.Add(new TagHelperAttribute("async", null, HtmlAttributeValueStyle.Minimized));

			output.Attributes.Add("src", DevReloadOptions.DevReloadPath);
		}
	}
}