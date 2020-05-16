using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System;

namespace Abiosoft.DotNet.DevReload
{
	static class Js
	{
		public static string Script;

		public static void GenerateScript(DevReloadOptions options, IOptions<HubOptions> signalRHubOptions)
		{
			if (DevReloadOptions.UseSignalR == false || signalRHubOptions == null)
			{
				Script = @"'use strict';

(function() {

	window.addEventListener('load', function() {
		const content = '" + options.PopoutHtmlTemplate.Replace("'", "\"").Replace(Environment.NewLine, $" \\{Environment.NewLine}") + @"';

		document.body.insertAdjacentHTML('beforeend', content);
	});

	let time = '', intervalId = undefined, isRefreshing = false, failMaxCounter = " +
		(options.MaxConnectionFailedCount <= 0 ? "undefined" : options.MaxConnectionFailedCount.ToString()) + @";

	function check() {
		var xhr = new XMLHttpRequest();
		xhr.open('HEAD', '" + DevReloadOptions.DevReloadPath + @"');
		xhr.setRequestHeader('ping', 'true');
		if(isRefreshing === false) {
			xhr.send();
			isRefreshing = true;
		}
		xhr.onreadystatechange = function() {
			if(xhr.readyState === 2 && xhr.status === 200) {
				let body = xhr.getResponseHeader('" + DevReloadMiddleware.REQUEST_HEADER_NAME + @"');
				if (time == '')
					time = body;

				if (time != body) {
					console.log('time is different', time, body, 'reloading...');
					" + options.TemplateActivationJSFragment + @"
					time = body;
					clearInterval(intervalId);
					location.reload();
				}
				else
					failMaxCounter = " + options.MaxConnectionFailedCount + @" <= 0 ? undefined : " + options.MaxConnectionFailedCount + @";
			}
			else if(failMaxCounter !== undefined && --failMaxCounter <= 0) {
				clearInterval(intervalId);
			}
			isRefreshing = false;
		}
	}


	intervalId = setInterval(check, " + options.CheckIntervalDelay + @");
})();";
			}
			else
			{
				Script = @"'use strict';

(function() {
	let time = '', connection = null, failMaxCounter = " +
		(options.MaxConnectionFailedCount <= 0 ? "undefined" : options.MaxConnectionFailedCount.ToString()) + @";

	window.addEventListener('load', function() {
		const content = '" + options.PopoutHtmlTemplate.Replace("'", "\"").Replace(Environment.NewLine, $" \\{Environment.NewLine}") + @"';
		document.body.insertAdjacentHTML('beforeend', content);

		setupSignalR();
	});
	window.addEventListener('beforeunload', function (e) {
		if (connection !== null) {
			connection.stop();
			console.log('DevReload SignalR disconnecting');
		}
	});

	function pongProcessing(body) {
		if (time == '')
			time = body;

		if (time != body) {
			console.log('time is different', time, body, 'reloading...');
			" + options.TemplateActivationJSFragment + @"
			time = body;
			location.reload();
		}
		else
			failMaxCounter = " + (options.MaxConnectionFailedCount <= 0 ? "undefined" : options.MaxConnectionFailedCount.ToString()) + @";
	}

	function setupSignalR() {
		connection = new signalR.HubConnectionBuilder().withUrl('" + DevReloadOptions.SignalRHubPath + @"')
			.configureLogging(signalR.LogLevel.Warning)
			.build();
		connection.serverTimeoutInMilliseconds = " + signalRHubOptions.Value.ClientTimeoutInterval.GetValueOrDefault(TimeSpan.FromSeconds(30)).TotalMilliseconds + @";

		connection.on('" + nameof(IDevReloadClient.DatePong) + @"', pongProcessing);

		function startSignalR() {
			connection.start().then(function() {
				console.log('DevReload SignalR connected');
				connection.invoke('" + nameof(IDevReloadServer.DatePing) + @"', time).then(pongProcessing);
			}).catch(function(err) {
				console.error('DevReload SignalR error: ' + err);
				if(failMaxCounter !== undefined && --failMaxCounter <= 0) {
				} else
					setTimeout(startSignalR, " + options.CheckIntervalDelay + @");
			});
		};

		connection.onclose(function(err) {
			if (err !== null && err !== undefined) {
				console.error('DevReload SignalR onclose, err: ' + err);

				if(failMaxCounter !== undefined && --failMaxCounter <= 0) {
				} else
					setTimeout(startSignalR, " + options.CheckIntervalDelay + @");
			}
		});

		startSignalR();
	}
})();";
			}
		}

	}
}