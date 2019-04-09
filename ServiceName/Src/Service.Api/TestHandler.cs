using System.Threading.Tasks;
using Rebus.Bus;
using Service.Infra.MessageBus;

namespace Service.Api
{
    public class TestHandler: IMessageHandleHandler<TestMessage>
    {
        private readonly IBus _bus;

        public TestHandler(IBus bus)
        {
            _bus = bus;
        }
        public Task Handle(TestMessage message)
        {
            return _bus.Reply(message);
        }
    }

    public class TestMessage : IntegrationMessage
    {
        public string Name { get; set; }
    }
}
