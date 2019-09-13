using System.Threading.Tasks;

namespace Service.Infra.MessageBus
{
    public interface IMessageHandler<in TIntegrationEvent> 
        where TIntegrationEvent : IntegrationMessage
    {
        Task Handle(TIntegrationEvent message);
    }
}
