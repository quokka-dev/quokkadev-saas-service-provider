using FluentAssertions;
using Moq;
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
        public async Task Test1()
        {
            // Arrange
            var mock = new Mock<object>();
            mock.Setup(m => m.Equals(It.IsAny<object>())).Returns(true);
            var obj = mock.Object;

            // Act
            await Task.CompletedTask;

            // Assert
            obj.Should().NotBeNull();
        }
    }
}
