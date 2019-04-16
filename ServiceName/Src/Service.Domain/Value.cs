using System;

namespace Service.Domain
{
    public class Value
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Name2 { get; set; }
        public string Name3 { get; set; }
        public string Name4 { get; set; }
        public string Name5 { get; set; }
        public string Name6 { get; set; }
        public string Name7 { get; set; }

        public Value()
        {
            Name = Name2 = Name3 = Name4 = Name5 = Name6 = Name7 = "Your code isn't doing anyting asynchronously. To utilize the await/async API, you need to be invoking an async method inside your own method (unless you want to implement the async logic yourself).In your case, if you're using Entity Framework, you can use it's async methods for querying:";
        }
    }
}
