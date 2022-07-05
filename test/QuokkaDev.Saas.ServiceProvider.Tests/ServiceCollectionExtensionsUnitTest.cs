using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Moq;
using QuokkaDev.Saas.Abstractions;
using System.Linq;
using Xunit;

namespace QuokkaDev.Saas.ServiceProvider.Tests;

public class ServiceCollectionExtensionsUnitTest
{
    [Fact(DisplayName = "MultiTenantServiceProviderFactory should be used")]
    public void MultiTenantServiceProviderFactory_Should_Be_Used()
    {
        // Arrange
        var mock = new Mock<IHostBuilder>();

        // Act
#pragma warning disable RCS1196 // Call extension method as instance method.
#pragma warning disable RCS1163 // Unused parameter.
        ServiceCollectionExtensions.UseMultiTenantServiceProviderFactory<Tenant<int>, int>(mock.Object, (tenant, container) => { });
#pragma warning restore RCS1163 // Unused parameter.
#pragma warning restore RCS1196 // Call extension method as instance method.

        // Assert
        mock.Verify(m => m.UseServiceProviderFactory(It.IsAny<MultiTenantServiceProviderFactory<Tenant<int>, int>>()), Times.Once);
    }

    [Fact(DisplayName = "UseMultiTenantContainer should register middleware")]
    public void UseMultiTenantContainer_Should_Register_Middleware()
    {
        // Arrange
        var mock = new Mock<IApplicationBuilder>();

        // Act
#pragma warning disable RCS1196 // Call extension method as instance method.
        ServiceCollectionExtensions.UseMultiTenantContainer<Tenant<int>, int>(mock.Object);
#pragma warning restore RCS1196 // Call extension method as instance method.

        // Assert
        mock.Invocations.Single(invocation => invocation.Method.Name.StartsWith("Use"))
           .Should().NotBeNull();
    }
}