using Rebus.Handlers;

namespace Service.Infra.MessageBus.Rebus
{
    public interface IRebusMessageHandler<TIntegrationEvent> : IMessageHandler<TIntegrationEvent>,
        IHandleMessages<TIntegrationEvent>
        where TIntegrationEvent : IntegrationMessage
    {
    }
}
