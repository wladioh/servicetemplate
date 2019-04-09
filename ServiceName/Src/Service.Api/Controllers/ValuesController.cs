using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Rebus.Bus;
using Service.Api.Resources;

namespace Service.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IStringLocalizer<SharedResource> _i18N;
        private readonly IMockApi _mock;
        private readonly IBus _bus;

        public ValuesController(IStringLocalizer<SharedResource> i18N, IMockApi mock, IBus bus)
        {
            _i18N = i18N;
            _mock = mock;
            _bus = bus;
        }
        // GET api/values
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MockValue>>> Get()
        {
            var random = new Random(Guid.NewGuid().GetHashCode());
            var x = 0;
            var sucess = 0;
            var error = 0;
            var results = new List<MockValue>();
            while (x < 100)
            {
                var re = await _mock.Get(random.Next(1, 500));
                if (re.IsSuccessStatusCode)
                {
                    results.Add(re.Content);
                    sucess++;
                }
                else
                    error++;
                x++;
            }
            Console.WriteLine($"Result = {sucess}");
            Console.WriteLine($"Errors = {error}");
            return results;
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var re = await _mock.Get(id);
            if (re.IsSuccessStatusCode)
                return Ok(re.Content);
            return BadRequest(re.Error.Content);
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
