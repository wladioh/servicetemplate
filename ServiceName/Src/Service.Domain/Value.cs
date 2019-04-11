using System;

namespace Service.Domain
{
    public class Value
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
    }
}
