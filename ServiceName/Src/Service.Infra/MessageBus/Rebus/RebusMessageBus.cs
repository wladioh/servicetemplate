using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Bus;

namespace Service.Infra.MessageBus.Rebus
{
    public class RebusMessageBus : IMessageBus
    {
        private readonly IBus _bus;

        public RebusMessageBus(IBus bus)
        {
            _bus = bus;
        }
        public async Task PublishAsync(IntegrationMessage message, Dictionary<string, string> optionalHeaders = null)
        {
            //todo: implements opentracing
            await _bus.Publish(message, optionalHeaders);
        }

        public async Task ReplyAsync(IntegrationMessage message, Dictionary<string, string> optionalHeaders = null)
        {
            //todo: implements opentracing
            await _bus.Reply(message, optionalHeaders);
        }

        public async Task SendAsync(IntegrationMessage message, Dictionary<string, string> optionalHeaders = null)
        { 
            //todo: implements opentracing
            await _bus.Send(message, optionalHeaders);
        }

        public async Task SubscribeAsync<T>()
            where T : IntegrationMessage
        {
            await _bus.Subscribe<T>();
        }

        public async Task UnsubscribeAsync<T>()
            where T : IntegrationMessage
        {
            await _bus.Unsubscribe<T>();
        }
    }
}
