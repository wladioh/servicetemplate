using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Infra.MessageBus
{
    public interface IMessageBus
    {
        Task PublishAsync(IntegrationMessage message, Dictionary<string, string> optionalHeaders = null);

        Task SubscribeAsync<T>()
            where T : IntegrationMessage;

        Task UnsubscribeAsync<T>() where T : IntegrationMessage;

        Task SendAsync(IntegrationMessage message, Dictionary<string, string> optionalHeaders = null);

        Task ReplyAsync(IntegrationMessage message, Dictionary<string, string> optionalHeaders = null);
    }
}
