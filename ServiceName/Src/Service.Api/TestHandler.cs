using System.Threading.Tasks;
using Service.Infra.MessageBus;

namespace Service.Api
{
    public class TestHandler: IMessageHandleHandler<TestMessage>
    {
        public Task Handle(TestMessage message)
        {
            return Task.CompletedTask;
        }
    }

    public class TestMessage : IntegrationMessage
    {
        public string Name { get; set; }
    }
}
