using System;
using System.Threading;
using System.Threading.Tasks;

namespace Service.Integration.Tests.RebusHelpers
{
    public class MessageWaiter<T> : IMessageWaiter
    {
        private readonly int _timeout;
        public Func<T, bool> Specification { get; }
        private readonly TaskCompletionSource<T> _taskCompletionSource =
            new TaskCompletionSource<T>();
        public MessageWaiter(Func<T, bool> specification, int timeout = 5000)
        {
            _timeout = timeout;
            Specification = specification;
        }


        public void Done(object message)
        {
            _taskCompletionSource.TrySetResult((T)message);
        }

        public void Cancel()
        {
            _taskCompletionSource.TrySetCanceled();
        }

        public Task<T> ToTask()
        {
            var ct = new CancellationTokenSource(_timeout);
            ct.Token.Register(() => _taskCompletionSource.TrySetCanceled(), false);
            return _taskCompletionSource.Task;
        }

        public bool CheckMessage(object message)
        {
            if (message is T msg)
                return Specification.Invoke(msg);
            return false;
        }
    }
}