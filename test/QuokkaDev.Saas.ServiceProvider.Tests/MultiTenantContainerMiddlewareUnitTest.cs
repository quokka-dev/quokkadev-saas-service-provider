using Autofac;
using FluentAssertions;
using HttpContextMoq;
using Microsoft.AspNetCore.Http;
using Moq;
using QuokkaDev.Saas.Abstractions;
using System.Threading.Tasks;
using Xunit;

namespace QuokkaDev.Saas.ServiceProvider.Tests
{
    public class MultiTenantContainerMiddlewareUnitTest
    {
        public MultiTenantContainerMiddlewareUnitTest()
        {
        }

        [Fact]
        public async Task Middleware_Should_Set_Service_Provider()
        {
            // Arrange

            var deleagetMock = new Mock<RequestDelegate>();
            MultiTenantContainerMiddleware<Tenant<int>, int> middleware = new(deleagetMock.Object);
            var httpContextMock = new HttpContextMock();

            var lifeTimeMockChild = new Mock<ILifetimeScope>();
            var lifeTimeMockParent = new Mock<ILifetimeScope>();
            lifeTimeMockParent.Setup(m => m.BeginLifetimeScope()).Returns(lifeTimeMockChild.Object);
            var containerMock = new Mock<MultiTenantContainer<Tenant<int>, int>>();
            containerMock.Setup(m => m.GetCurrentTenantScope()).Returns(lifeTimeMockParent.Object);
            // Act
            await middleware.Invoke(httpContextMock, () => containerMock.Object);

            // Assert
            httpContextMock.RequestServices.Should().NotBeNull();
            containerMock.Verify(m => m.GetCurrentTenantScope(), Times.Once);
            lifeTimeMockParent.Verify(m => m.BeginLifetimeScope(), Times.Once);

        }
    }
}
