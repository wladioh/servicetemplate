using System.Threading.Tasks;
using Rebus.Handlers;

namespace Service.Integration.Tests.RebusHelpers
{
    public class HandlerAllMessages : IHandleMessages<object>
    {
        private readonly MessageHelper _reply;

        public HandlerAllMessages(MessageHelper reply)
        {
            _reply = reply;
        }
        public Task Handle(object message)
        {
            _reply.DeliveryMessage(message);
            return Task.CompletedTask;
        }
    }
}
