using System;

namespace Service.Infra.MessageBus
{
    public class IntegrationMessage
    {
        public IntegrationMessage()
        {
            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;
        }
        public Guid Id { get; }
        public DateTime CreationDate { get; }
        public Guid CorrelationId { get; set; }
    }
}
