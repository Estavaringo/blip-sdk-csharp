using Lime.Protocol;
using System.Threading;
using System.Threading.Tasks;
using Take.Blip.Client;

namespace Blip.HttpClient.Services
{
    /// <summary>
    /// BLiP ISender implementation that uses Http calls instead of Tcp
    /// </summary>
    public class HttpBlipClient : ISender
    {
        private readonly IHttpBlipClient _httpBlipClient;

        /// <summary>
        /// Base ctor using a given http client
        /// </summary>
        /// <param name="httpBlipClient"></param>
        public HttpBlipClient(IHttpBlipClient httpBlipClient)
        {
            _httpBlipClient = httpBlipClient;
        }

        /// <summary>
        /// Sends a command to BLiP and returns its response.
        /// </summary>
        /// <param name="requestCommand"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<Command> ProcessCommandAsync(Command requestCommand, CancellationToken cancellationToken)
        {
            return _httpBlipClient.ProcessCommandAsync(requestCommand, cancellationToken);
        }

        /// <summary>
        /// Sends a command to BLiP
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task SendCommandAsync(Command command, CancellationToken cancellationToken)
        {
            return _httpBlipClient.SendCommandAsync(command, cancellationToken);
        }

        /// <summary>
        /// Sends a message through BLiP to the set recipient. Message's content is a LIME Document Type.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task SendMessageAsync(Message message, CancellationToken cancellationToken)
        {
            return _httpBlipClient.SendMessageAsync(message, cancellationToken);
        }

        /// <summary>
        /// Sends a notification to BLiP
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task SendNotificationAsync(Notification notification, CancellationToken cancellationToken)
        {
            return _httpBlipClient.SendNotificationAsync(notification, cancellationToken);
        }
    }
}
