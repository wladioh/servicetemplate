using System;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Persistence.InMem;
using Rebus.Retry.Simple;
using Rebus.Transport.InMem;

namespace Service.Integration.Tests.RebusHelpers
{
    public static class BusHelper
    {
        public static IBus Create(MessageHelper messageHelper, InMemNetwork network, string queue, Action<StandardConfigurer<Rebus.Routing.IRouter>> RouteConfigure = null)
        {
            var activator = new BuiltinHandlerActivator();
            activator.Register((bus, context) => new HandlerAllMessages(messageHelper));
            return Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(network, queue))
                .Routing(RouteConfigure ?? (configurer => { }))
                .Subscriptions(it => it.StoreInMemory())
                .Logging(l =>
                {
                    l.ColoredConsole();
                }).Options(it => it.SimpleRetryStrategy())
                .Start();
        }
    }
}
