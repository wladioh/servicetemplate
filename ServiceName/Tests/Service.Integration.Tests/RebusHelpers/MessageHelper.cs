using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Transport.InMem;

namespace Service.Integration.Tests.RebusHelpers
{
    public class MessageHelper
    {
        private readonly List<object> _replyMessages;
        private readonly List<IMessageWaiter> _waiters = new List<IMessageWaiter>();
        private readonly InMemNetwork _inMemNetwork;

        public MessageHelper(InMemNetwork inMemNetwork)
        {
            _replyMessages = new List<object>();
            _inMemNetwork = inMemNetwork;
        }

        public Task<T> WaitForMessage<T>(int timeout = 5000)
        {
            var waiter = new MessageWaiter<T>(m => true, timeout);
            _waiters.Add(waiter);
            var message = _replyMessages.FirstOrDefault();
            if (message != null)
                waiter.Done(message);
            return waiter.ToTask();
        }

        public Task<T> WaitForMessage<T>(Func<T, bool> specification, int timeout = 5000)
        {
            var waiter = new MessageWaiter<T>(specification, timeout);
            _waiters.Add(waiter);
            var message = _replyMessages.OfType<T>()
                .FirstOrDefault(specification);
            if (message != null)
                waiter.Done(message);
            return waiter.ToTask();
        }

        public void DeliveryMessage(object message)
        {
            var waiters = _waiters.Where(it => it.CheckMessage(message)).ToList();
            if (waiters.Any())
                waiters.ForEach(it => it.Done(message));
            else
                _replyMessages.Add(message);
        }

        public void ListenerQueue(string queue)
        {
            BusHelper.Create(this, _inMemNetwork, queue);
        }
    }
}
