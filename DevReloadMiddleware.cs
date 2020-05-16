using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace Abiosoft.DotNet.DevReload
{
	/// <summary>
	/// DevReload middleware class
	/// </summary>
	public class DevReloadMiddleware
	{
		private string _time;
		private readonly RequestDelegate _next;
		private readonly FileSystemWatcher _watcher;
		private readonly DevReloadOptions _options;
		private IHubContext<DevReloadHub, IDevReloadClient> _hubContext;

		internal const string REQUEST_HEADER_NAME = "date";

		internal static string GetLastChangeDateTimeAsString => DateTime.Now.ToString("R");//RFC1123Pattern

		/// <summary>
		/// DevReload middleware default DI constructor.
		/// </summary>
		public DevReloadMiddleware(RequestDelegate next, IOptions<DevReloadOptions> options,
			IHostingEnvironment env, IApplicationLifetime applicationLifetime,
			IOptions<HubOptions> signalRHubOptions,
			IHubContext<DevReloadHub, IDevReloadClient> hubContext)
		{
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}
			if (next == null)
			{
				throw new ArgumentNullException(nameof(next));
			}
			if (!env.IsDevelopment())
			{
				throw new SecurityException("WARNING: Non develop environment with DevReload active!");
			}

			_hubContext = hubContext;
			_time = GetLastChangeDateTimeAsString;
			_watcher = new FileSystemWatcher();
			_next = next;
			_options = options.Value;

			if (_options.IgnoredSubDirectories != null)
			{
				_options.IgnoredSubDirectories = _options.IgnoredSubDirectories
					.Select(i => $"{Path.DirectorySeparatorChar}{i}{Path.DirectorySeparatorChar}").ToArray();
			}
			if (_options.StaticFileExtensions != null)
			{
				_options.StaticFileExtensions = _options.StaticFileExtensions
					.Select(e => $".{e.TrimStart('.')}").ToArray();
			}

			applicationLifetime.ApplicationStopped.Register(OnShutDown, false);

			Js.GenerateScript(_options, signalRHubOptions);

			Task.Run((Action)Watch);
		}

		private void OnShutDown()
		{
			if (_watcher != null)
			{
				_watcher.EnableRaisingEvents = false;
				_watcher.Dispose();
			}
		}

		/// <summary>
		/// Execution of middleware through pipeline
		/// </summary>
		/// <param name="context">context</param>
		/// <returns></returns>
		public Task Invoke(HttpContext context)
		{
			if (context.Request.Path.StartsWithSegments(DevReloadOptions.DevReloadPath))
			{
				if (context.Request.Headers.ContainsKey("ping"))
				{
					if (HttpMethods.IsHead(context.Request.Method))
					{
						context.Request.Method = HttpMethods.Get;
						//changing default date header: https://developer.mozilla.org/pl/docs/Web/HTTP/Headers/Data
						context.Response.Headers[REQUEST_HEADER_NAME] = _time;
						context.Response.Body = Stream.Null;
						return Task.CompletedTask;
					}
					else
						return context.Response.WriteAsync(_time, context.RequestAborted);
				}

				context.Response.ContentType = "application/javascript";
				return context.Response.WriteAsync(Js.Script, context.RequestAborted);
			}
			return _next(context);
		}

		private void Watch()
		{
			_watcher.Path = _options.Directory;

			_watcher.NotifyFilter = NotifyFilters.LastWrite
								 | NotifyFilters.FileName
								 | NotifyFilters.DirectoryName;

			_watcher.Changed += OnChanged;
			_watcher.Created += OnChanged;
			_watcher.Deleted += OnChanged;
			_watcher.IncludeSubdirectories = true;

			_watcher.EnableRaisingEvents = true;
		}

		// Define the event handlers.
		private void OnChanged(object source, FileSystemEventArgs e)
		{
			// return if it's an ignored directory
			var sep = Path.DirectorySeparatorChar;
			string full_path = e.FullPath.Replace('\\', sep).Replace('/', sep);
			foreach (string ignoredDirectory in _options.IgnoredSubDirectories)
			{
				if (full_path.Contains(ignoredDirectory)) return;
			}

			FileInfo fileInfo = new FileInfo(e.FullPath);
			if (_options.StaticFileExtensions.Length > 0)
			{
				if (_options.StaticFileExtensions.Contains(fileInfo.Extension))
				{
					_time = GetLastChangeDateTimeAsString;
					if (DevReloadOptions.UseSignalR && _hubContext != null)
						_hubContext.Clients.All.DatePong(_time);
				}
			}
			else
			{
				_time = GetLastChangeDateTimeAsString;
				if (DevReloadOptions.UseSignalR && _hubContext != null)
					_hubContext.Clients.All.DatePong(_time);
			}
			// Specify what is done when a file is changed, created, or deleted.
			Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
		}
	}
}
