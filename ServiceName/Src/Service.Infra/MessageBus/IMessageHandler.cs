using System.Threading.Tasks;
using Rebus.Handlers;

namespace Service.Infra.MessageBus
{
    public interface IMessageHandler<in TIntegrationEvent> : IHandleMessages<TIntegrationEvent>
        where TIntegrationEvent : IntegrationMessage
    {
        new Task Handle(TIntegrationEvent message);
    }
}
