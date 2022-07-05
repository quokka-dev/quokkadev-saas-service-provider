using Autofac;
using Autofac.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using QuokkaDev.Saas.Abstractions;
using QuokkaDev.Saas.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace QuokkaDev.Saas.ServiceProvider.Tests
{
    public class MultiTenantContainerUnitTest
    {
        public MultiTenantContainerUnitTest()
        {
        }

        [Fact(DisplayName = "Different scopes should be created for different tenants")]
        public void Different_Scopes_Should_Be_Created_For_Different_Tenants()
        {
            // Arrange
            FakeTenantAccessService service = new();
            MultiTenantContainer<Tenant<int>, int> container;
            IContainer applicationContainer;
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            (container, applicationContainer) = GetMultiTenantContainer(service);
#pragma warning restore IDE0059 // Unnecessary assignment of a value

            // Act
            var scope1 = container.GetCurrentTenantScope();
            service.SetTenant("other-tenant-identifier");
            var scope2 = container.GetCurrentTenantScope();
            service.SetTenant("my-tenant-identifier");
            var scope3 = container.GetCurrentTenantScope();
            // Assert
            scope1.Should().NotBeNull();
            scope2.Should().NotBeNull();
            scope3.Should().NotBeNull();

            scope1.Should().NotBeSameAs(scope2);
            scope1.Should().BeSameAs(scope3);
        }

        [Fact(DisplayName = "No tenants should return root scope")]
        public void No_Tenants_Should_Return_Root_Scope()
        {
            // Arrange
            FakeTenantAccessService service = new();
            MultiTenantContainer<Tenant<int>, int> container;
            IContainer applicationContainer;
            (container, applicationContainer) = GetMultiTenantContainer(service);

            // Act            
            service.SetTenant(null!);
            var scope1 = container.GetCurrentTenantScope();

            // Assert
            scope1.Should().NotBeNull();
            scope1.Should().BeSameAs(applicationContainer);
        }

        [Fact(DisplayName = "MultiTenantContainer should work as expected")]
        public void MultiTenantContainer_Should_Work_As_Expected()
        {
            // Arrange
            FakeTenantAccessService service = new();
            MultiTenantContainer<Tenant<int>, int> container;
            IContainer applicationContainer;
            (container, applicationContainer) = GetMultiTenantContainer(service);

            // Act
            service.SetTenant(null!);

            var scope1 = container.BeginLifetimeScope();
            var scope2 = container.BeginLifetimeScope("MyTag");
            var scope3 = container.BeginLifetimeScope("MyTag2", builder => builder.RegisterType<FakeGenericService>().AsSelf().InstancePerDependency());
            var scope4 = container.BeginLifetimeScope(builder => builder.RegisterType<FakeGenericService>().AsSelf().InstancePerLifetimeScope());

            var service1 = container.ResolveOptional<FakeGenericService>();
            var service2 = scope3.Resolve<FakeGenericService>();
            var service3 = scope4.Resolve<FakeGenericService>();
            var service4 = scope4.Resolve<FakeGenericService>();

            // Assert
            scope1.Should().NotBeNull();
            scope2.Should().NotBeNull();
            scope2.Tag.Should().Be("MyTag");
            scope3.Should().NotBeNull();
            scope3.Tag.Should().Be("MyTag2");
            scope4.Should().NotBeNull();

            service1.Should().BeNull();
            service2.Should().NotBeNull();
            service3.Should().NotBeNull();
            service4.Should().NotBeNull();

            service2.Should().NotBeSameAs(service3);
            service2.Should().NotBeSameAs(service4);
            service3.Should().BeSameAs(service4);
        }

        [Fact(DisplayName = "Wrapped properties should works as expected")]
        public void Wrapped_Properties_Should_Works_As_Expected()
        {
            // Arrange
            FakeTenantAccessService service = new();
            MultiTenantContainer<Tenant<int>, int> container;
            IContainer applicationContainer;
            (container, applicationContainer) = GetMultiTenantContainer(service);

            // Act

            service.SetTenant(null!);
            var scope1 = container.GetCurrentTenantScope();

            // Assert
            container.DiagnosticSource.Should().BeSameAs(applicationContainer.DiagnosticSource);
            container.Disposer.Should().BeSameAs(scope1.Disposer);
            container.Tag.Should().BeSameAs(scope1.Tag);
        }

        [Fact(DisplayName = "Container should be properly disposed")]
        public async Task Container_Should_Be_Properly_Disposed()
        {
            // Arrange
            FakeTenantAccessService service = new();
            MultiTenantContainer<Tenant<int>, int> container, container2;
            IContainer applicationContainer, applicationContainer2;
            (container, applicationContainer) = GetMultiTenantContainer(service, (_, builder) => builder.RegisterType<FakeGenericService>().AsSelf().InstancePerLifetimeScope());
            (container2, applicationContainer2) = GetMultiTenantContainer(service, (_, builder) => builder.RegisterType<FakeGenericService>().AsSelf().InstancePerLifetimeScope());

            // Act
            var scope1 = container.GetCurrentTenantScope();
            container.Dispose();
            var service1Invocation = () => scope1.Resolve<FakeGenericService>();

            service.SetTenant("other-tenant");
            var scope2 = container2.GetCurrentTenantScope();
            await container2.DisposeAsync();
            var service2Invocation = () => scope2.Resolve<FakeGenericService>();

            // Assert
            service1Invocation.Should().Throw<ObjectDisposedException>();
            service2Invocation.Should().Throw<ObjectDisposedException>();
        }

        [Fact(DisplayName = "Events should be properly fired")]
        public void Events_Should_Be_Properly_Fired()
        {
            // Arrange
            FakeTenantAccessService service = new();
            MultiTenantContainer<Tenant<int>, int> container;
            IContainer applicationContainer;
            (container, applicationContainer) = GetMultiTenantContainer(service, (_, builder) => builder.RegisterType<FakeGenericService>().AsSelf().InstancePerLifetimeScope());

            using var monitor = container.Monitor();

            // Act
            var scope1 = container.GetCurrentTenantScope();
            scope1.Resolve<FakeGenericService>();
            container.Dispose();

            // Assert
            monitor.Should().Raise(nameof(container.ChildLifetimeScopeBeginning));
            monitor.Should().Raise(nameof(container.ResolveOperationBeginning));
            monitor.Should().Raise(nameof(container.CurrentScopeEnding));
        }

        private static (MultiTenantContainer<Tenant<int>, int> Container, IContainer RootContainer) GetMultiTenantContainer(FakeTenantAccessService service, Action<Tenant<int>, ContainerBuilder>? config = null)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddMultiTenancy<Tenant<int>, int>()
                .WithService<FakeTenantAccessService>(service);

            ContainerBuilder builder = new();
            builder.Populate(services);
            var applicationContainer = builder.Build();

#pragma warning disable RCS1163 // Unused parameter.
            MultiTenantContainer<Tenant<int>, int> container = new(applicationContainer, config ?? ((tenant, builder) => { }));
#pragma warning restore RCS1163 // Unused parameter.
            return (container, applicationContainer);
        }
    }

    public class FakeTenantAccessService : ITenantAccessService<Tenant<int>, int>
    {
        private string currentTenantIdentifier = "my-tenant-identifier";

        public void SetTenant(string newTenantIdentifier)
        {
            currentTenantIdentifier = newTenantIdentifier;
        }

        public Tenant<int> GetTenant()
        {
            return new Tenant<int>(1, currentTenantIdentifier);
        }

        public Task<Tenant<int>> GetTenantAsync()
        {
            return Task.FromResult(GetTenant());
        }
    }

    public class FakeGenericService
    {
    }
}
