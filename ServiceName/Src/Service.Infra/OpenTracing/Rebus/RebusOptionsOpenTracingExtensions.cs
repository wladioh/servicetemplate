using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OpenTracing;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;

namespace Service.Infra.OpenTracing.Rebus
{
    public static class RebusOptionsOpenTracingExtensions
    {
        public static void EnableOpenTracing(this OptionsConfigurer configurer, IServiceProvider serviceProvider)
        {
            configurer.Decorate<IPipeline>(c =>
            {
                var tracer = serviceProvider.GetService<ITracer>();
                var hostingEnvironment = serviceProvider.GetRequiredService<IHostingEnvironment>();
                var outgoingStep = new OpenTracingOutgoingStep(tracer, hostingEnvironment);
                var incomingStep = new OpenTracingIncomingStep(tracer, hostingEnvironment);

                var pipeline = c.Get<IPipeline>();

                return new PipelineStepInjector(pipeline)
                    .OnReceive(incomingStep, PipelineRelativePosition.After, typeof(DeserializeIncomingMessageStep))
                    .OnSend(outgoingStep, PipelineRelativePosition.Before, typeof(SerializeOutgoingMessageStep));
            });
        }
    }
}
