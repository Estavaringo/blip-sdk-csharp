using Lime.Protocol;
using RestEase;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Blip.HttpClient.Services
{
    /// <summary>
    /// BLiP Client that uses Http calls
    /// </summary>
    public interface IHttpBlipClient
    {
        /// <summary>
        /// Authorization key
        /// </summary>
        [Header("Authorization")]
        AuthenticationHeaderValue Authorization { get; set; }

        /// <summary>
        /// Processes a Lime <paramref name="requestCommand"/> and returns a command response
        /// </summary>
        /// <param name="requestCommand"></param>
        /// <param name="cancellationToken"></param>
        [Post("commands")]
        Task<Command> ProcessCommandAsync([Body] Command requestCommand, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a lime <paramref name="command"/> without awaiting for a response
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        [Post("commands")]
        Task SendCommandAsync([Body] Command command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a lime <paramref name="message"/> 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        [Post("messages")]
        Task SendMessageAsync([Body] Message message, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a lime <paramref name="notification"/> 
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Post("notifications")]
        Task SendNotificationAsync([Body] Notification notification, CancellationToken cancellationToken);
    }
}