using System.Threading.Tasks;
using Rebus.Bus;
using Service.Infra.MessageBus;
using Service.Infra.MessageBus.Rebus;

namespace Service.Api.Handlers
{
    public class TestHandler: IRebusMessageHandler<TestMessage>,
        IRebusMessageHandler<OtherMessage>,
        IRebusMessageHandler<OtherMessagePublish>
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

        public Task Handle(OtherMessage message)
        {
            return _bus.Send(message);
        }

        public Task Handle(OtherMessagePublish message)
        {
            return _bus.Publish(message);
        }
    }

    public class OtherMessagePublish : IntegrationMessage
    {
        public string Name { get; set; }
    }
    public class OtherMessage: IntegrationMessage
    {
        public string Name { get; set; }
    }

    public class TestMessage : IntegrationMessage
    {
        public string Name { get; set; }
    }
}
