using Rebus.Handlers;

namespace Service.Infra.MessageBus
{
    public interface IMessageHandleHandler<in TIntegrationEvent> :
        IMessageHandleHandler, IHandleMessages<TIntegrationEvent>
        where TIntegrationEvent : IntegrationMessage
    {
    }
    public interface IMessageHandleHandler
    {
    }
}
