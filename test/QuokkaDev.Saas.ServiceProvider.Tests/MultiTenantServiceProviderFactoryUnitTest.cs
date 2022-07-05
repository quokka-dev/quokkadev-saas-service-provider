using Autofac;
using Autofac.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using QuokkaDev.Saas.Abstractions;
using Xunit;

namespace QuokkaDev.Saas.ServiceProvider.Tests
{
    public class MultiTenantServiceProviderFactoryUnitTest
    {
        public MultiTenantServiceProviderFactoryUnitTest()
        {
        }

        [Fact(DisplayName = "ContainerBuilder should be created")]
        public void ContainerBuilder_Should_Be_Created()
        {
            // Arrange
            MultiTenantServiceProviderFactory<Tenant<int>, int> factory = new((_, _) => { });

            IServiceCollection services = new ServiceCollection();

            // Act
            var builder = factory.CreateBuilder(services);

            // Assert
            builder.Should().NotBeNull();
            builder.Should().BeOfType<ContainerBuilder>();
        }

        [Fact(DisplayName = "ContainerBuilder should be created")]
        public void ServiceProvider_Should_Be_Created()
        {
            // Arrange
            MultiTenantServiceProviderFactory<Tenant<int>, int> factory = new((_, __) => { });

            IServiceCollection services = new ServiceCollection();

            // Act
            var provider = factory.CreateServiceProvider(factory.CreateBuilder(services));

            // Assert
            provider.Should().NotBeNull();
            provider.Should().BeOfType<AutofacServiceProvider>();
        }
    }
}
