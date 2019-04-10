using Rebus.Handlers;

namespace Service.Infra.MessageBus
{
    public interface IMessageHandler<in TIntegrationEvent> :
        IMessageHandler, IHandleMessages<TIntegrationEvent>
        where TIntegrationEvent : IntegrationMessage
    {
    }
    public interface IMessageHandler
    {
    }
}
