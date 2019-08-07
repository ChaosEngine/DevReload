using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.IO;
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

		/// <summary>
		/// DevReload middleware default DI constructor.
		/// </summary>
		public DevReloadMiddleware(RequestDelegate next, IOptions<DevReloadOptions> options,
			IHostingEnvironment env, IApplicationLifetime applicationLifetime)
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

			_time = DateTime.Now.ToString();
			_watcher = new FileSystemWatcher();
			_next = next;
			_options = options.Value;

			applicationLifetime.ApplicationStopped.Register(OnShutDown, false);

			Js.GenerateScript(_options);

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
		/// <param name="c">context</param>
		/// <returns></returns>
		public Task Invoke(HttpContext c)
		{
			if (c.Request.Path.StartsWithSegments(DevReloadOptions.DevReloadPath))
			{
				if (c.Request.Headers.ContainsKey("ping"))
				{
					if (HttpMethods.IsHead(c.Request.Method))
					{
						c.Request.Method = HttpMethods.Get;
						c.Response.Headers.Add("pong", _time);
						c.Response.Body = Stream.Null;
						return Task.CompletedTask;
					}
					else
						return c.Response.WriteAsync(_time);
				}

				c.Response.ContentType = "application/javascript";
				return c.Response.WriteAsync(Js.Script);
			}
			return _next(c);
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
			foreach (string ignoredDirectory in _options.IgnoredSubDirectories)
			{
				if (e.FullPath.Contains($"{sep}{ignoredDirectory}{sep}")) return;
			}

			FileInfo fileInfo = new FileInfo(e.FullPath);
			if (_options.StaticFileExtensions.Length > 0)
			{
				foreach (string extension in _options.StaticFileExtensions)
				{
					if (fileInfo.Extension.Equals($".{extension.TrimStart('.')}"))
					{
						_time = DateTime.Now.ToString();
						break;
					}
				}
			}
			else
			{
				_time = DateTime.Now.ToString();
			}
			// Specify what is done when a file is changed, created, or deleted.
			Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
		}

	}
}
