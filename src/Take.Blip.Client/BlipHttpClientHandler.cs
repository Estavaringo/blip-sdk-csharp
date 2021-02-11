using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Serilog;
using Serilog.Events;

namespace Take.Blip.Client
{
    class BlipHttpClientHandler : HttpClientHandler
    {
        public BlipHttpClientHandler()
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Log.Logger != null && Log.Logger.IsEnabled(LogEventLevel.Verbose))
            {
                Log.Logger.Verbose("{Operation}: " + request, DataOperation.Send);
                var response = await base.SendAsync(request, cancellationToken);
                Log.Logger.Verbose("{Operation}: " + response, DataOperation.Receive);
                return response;
            }
            else
            {
                return await base.SendAsync(request, cancellationToken);
            }
        }
    }
}
