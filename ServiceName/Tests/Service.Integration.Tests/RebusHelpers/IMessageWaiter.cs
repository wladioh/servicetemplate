namespace Service.Integration.Tests.RebusHelpers
{
    public interface IMessageWaiter
    {
        bool CheckMessage(object message);
        void Done(object message);
    }
}