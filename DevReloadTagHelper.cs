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
			output.TagName = "script";    // Replaces <devreload> with <script> tag
            
            var async_attr = new TagHelperAttribute("async", null, HtmlAttributeValueStyle.Minimized);
            output.Attributes.Add(async_attr);

            output.Attributes.Add("src", DevReloadOptions.DevReloadPath);
        }
    }
}