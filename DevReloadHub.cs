using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Abiosoft.DotNet.DevReload
{
	/// <summary>
	/// Server side signalR methods
	/// </summary>
	public interface IDevReloadServer
	{
		/// <summary>
		/// Ping method for passing last change date
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		Task<string> DatePing(string time);
	}

	/// <summary>
	/// Client side signalR methods
	/// </summary>
	public interface IDevReloadClient
	{
		/// <summary>
		/// Ping response
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		Task DatePong(string time);
	}

	/// <summary>
	/// SignalR hub
	/// </summary>
	public class DevReloadHub : Hub<IDevReloadClient>, IDevReloadServer
	{
		/// <summary>
		/// Ping method for passing last change date
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		public Task<string> DatePing(string time)
		{
			time = DevReloadMiddleware.GetLastChangeDateTimeAsString;

			//await Clients.All.Pong(time);
			return Task.FromResult(time);
		}
	}
}
