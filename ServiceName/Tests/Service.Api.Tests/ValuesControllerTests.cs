using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Moq;
using Rebus.Bus;
using Refit;
using Service.Api.Controllers;
using Service.Api.Integrations;
using Service.Api.Resources;
using Service.Domain;
using Xunit;

namespace Service.Api.Tests
{
    public class ValuesControllerTests
    {
        private readonly MockRepository _mockRepository;
        private readonly Mock<IStringLocalizer<SharedResource>> _i18N;
        private readonly Mock<IPokemonApi> _api;
        private readonly Mock<IBus> _bus;
        private readonly Mock<IValueRepository> _repository;
        private readonly Mock<IDistributedCache> _cache;
        private readonly ValuesController _controller;

        public ValuesControllerTests()
        {
            _mockRepository = new MockRepository(MockBehavior.Default);
            _i18N = _mockRepository.Create<IStringLocalizer<SharedResource>>();
            _api = _mockRepository.Create<IPokemonApi>();
            _bus = _mockRepository.Create<IBus>();
            _repository = _mockRepository.Create<IValueRepository>();
            _cache = _mockRepository.Create<IDistributedCache>();
            _controller = new ValuesController(_i18N.Object, _api.Object, _bus.Object, _repository.Object, _cache.Object);
        }
        [Fact]
        public async Task SingleTest()
        {
            var id = 1;
            var genders = new Genders { id = id };
            var response = new ApiResponse<Genders>(new HttpResponseMessage(System.Net.HttpStatusCode.OK), genders);
            _api.Setup(it => it.Get(id.ToString()))
                .ReturnsAsync(response);

            var result = await _controller.Get(id);

            result.Should().BeOfType<OkObjectResult>();
            var mockValue = result.As<OkObjectResult>().Value.As<Genders>();
            mockValue.Should().BeEquivalentTo(genders);
            response.Dispose();
        }
    }
}
