using System.Threading.Tasks;
using OpenTracing;
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
        private readonly ITracer _tracer;

        public TestHandler(IBus bus, ITracer  tracer)
        {
            _bus = bus;
            _tracer = tracer;
        }
        public Task Handle(TestMessage message)
        {
            //_tracer.ActiveSpan.Finish();
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
