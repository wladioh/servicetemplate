using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Moq;
using Rebus.Bus;
using Refit;
using Service.Api.Controllers;
using Service.Api.Resources;
using Service.Domain;
using Xunit;

namespace Service.Api.Tests
{
    public class ValuesControllerTests
    {
        private MockRepository _mockRepository;
        private Mock<IStringLocalizer<SharedResource>> _i18N;
        private Mock<IMockApi> _api;
        private Mock<IBus> _bus;
        private Mock<IClassRepository> _repository;
        private ValuesController _controller;

        public ValuesControllerTests()
        {
            _mockRepository = new MockRepository(MockBehavior.Default);
            _i18N = _mockRepository.Create<IStringLocalizer<SharedResource>>();
            _api = _mockRepository.Create<IMockApi>();
            _bus = _mockRepository.Create<IBus>();
            _repository = _mockRepository.Create<IClassRepository>();
            _controller = new ValuesController(_i18N.Object, _api.Object, _bus.Object, _repository.Object);
        }
        [Fact]
        public async Task SingleTest()
        {
            var id = 1;
            var response = new ApiResponse<MockValue>(new HttpResponseMessage(), new MockValue
            {
                Id = id
            });
            _api.Setup(it => it.Get(id))
                .ReturnsAsync(response);

            var result = await _controller.Get(id);

            result.Should().BeOfType<OkObjectResult>();
            var mockValue = result.As<OkObjectResult>().Value.As<MockValue>();
            mockValue.Should().BeEquivalentTo(new MockValue
            {
                Id = id
            });
            response.Dispose();
        }
    }
}
