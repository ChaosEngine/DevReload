using System;

namespace Abiosoft.DotNet.DevReload
{
    static class Js
    {
        public static string Script;

        public static void GenerateScript(DevReloadOptions options)
        {
            Script = @"(function() {

	window.addEventListener('load', function() {
		const content = '" + options.PopoutHtmlTemplate.Replace("'", "\"").Replace(Environment.NewLine, $" \\{Environment.NewLine}") + @"';

		document.body.insertAdjacentHTML('beforeend', content);
	});

	var time = '', intervalId = undefined, isRefreshing = false,
		failMaxCounter = (" + options.MaxConnectionFailedCount + @" <= 0 ? undefined : " + options.MaxConnectionFailedCount + @");

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
				let body = xhr.getResponseHeader('pong');
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
					failMaxCounter = (" + options.MaxConnectionFailedCount + @" <= 0 ? undefined : " + options.MaxConnectionFailedCount + @");
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

    }
}