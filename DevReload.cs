using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using System;

namespace Abiosoft.DotNet.DevReload
{

	/// <summary>
	/// Options to configure DevReload middleware.
	/// </summary>
	public class DevReloadOptions
	{
		/// <summary>
		/// Directory to watch for file changes.
		/// Default: "wwwroot".
		/// </summary>
		public string Directory { get; set; } = "./wwwroot";

		/// <summary>
		/// SubDirectories to ignore.
		/// </summary>
		public string[] IgnoredSubDirectories { get; set; } = new string[] { ".git", ".node_modules" };

		/// <summary>
		/// File extensions to watch, this should only be static files.
		/// Do not include dotnet files like razor and cshtml.
		/// Default: ["js", "css", "html"]
		/// </summary>
		public string[] StaticFileExtensions { get; set; } = new string[] { "js", "html", "css", };

		/// <summary>
		/// Delay between subsequent checkings of date (used for Javascript.setInterval)
		/// in millisecs
		/// </summary>
		public int CheckIntervalDelay { get; set; } = 1000;

		/// <summary>
		/// How many failed network errors is accepted before failing and stopping checking of  code change
		/// CheckIntervalDelay * MaxConnectionFailedCount gives maximum build or reload time before failing and stoping refresh script
		/// </summary>
		public int MaxConnectionFailedCount { get; set; } = 20;

		/// <summary>
		/// Html template to be placed in the body of the page and visible on page reloads
		/// It could contain some custom visuals and HTML elements to be shown.
		/// </summary>
		public string PopoutHtmlTemplate { get; set; } = "<div id='reload' style='display:none; position: absolute; left: 0; top: 0; background-color: #fff; z-index: 9999; padding: 2px; border: solid 1px #333'>DevReload - Reloading page...</div>";

		/// <summary>
		/// JS fragment that is executed when <see cref="PopoutHtmlTemplate"/> is to be shown.
		/// Activation of DOM elements, prepare some styles etc.
		/// </summary>
		public string TemplateActivationJSFragment { get; set; } = "document.getElementById('reload').style.display = 'block';";

		/// <summary>
		/// Path/rout at which we are serving date check request and refresh script
		/// </summary>
		public static string DevReloadPath { get; set; } = "/__DevReload";

		/// <summary>
		/// Whether to use SignalR websocket,server send events, long pooling mechanisms or request pooling
		/// </summary>
		public static bool UseSignalR { get; set; } = false;

		/// <summary>
		/// SignalR javascript library path
		/// </summary>
		public static string SignalRClientSide { get; set; } = "~/lib/signalr/dist/browser/signalr.js";

		/// <summary>
		/// SignalR hub name
		/// </summary>
		public static string SignalRHubPath { get; set; } = "/DevReloadSignalR";
	}

	/// <summary>
	/// Helpers
	/// </summary>
	public static class MiddlewareHelpers
	{
		/// <summary>
		/// Use DevReload middleware with the default configurations.
		/// </summary>
		public static IApplicationBuilder UseDevReload(this IApplicationBuilder app)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}

			return app.UseDevReload(new DevReloadOptions());
		}
		/// <summary>
		/// Use DevReload middleware with custom configuration.
		/// </summary>
		public static IApplicationBuilder UseDevReload(this IApplicationBuilder app, DevReloadOptions options)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}
			if(DevReloadOptions.UseSignalR && string.IsNullOrEmpty(DevReloadOptions.SignalRClientSide))
			{
				throw new ArgumentException("Parameter not set when using SignalR", nameof(DevReloadOptions.SignalRClientSide));
			}

			return app.UseMiddleware<DevReloadMiddleware>(Options.Create(options));
		}
	}

}
