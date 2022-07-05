using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using QuokkaDev.Saas.Abstractions;

namespace QuokkaDev.Saas.ServiceProvider
{
    public class MultiTenantServiceProviderFactory<T, TKey> : IServiceProviderFactory<ContainerBuilder> where T : Tenant<TKey>
    {
        private readonly Action<T, ContainerBuilder> _tenantServicesConfiguration;

        public MultiTenantServiceProviderFactory(Action<T, ContainerBuilder> tenantServicesConfiguration)
        {
            _tenantServicesConfiguration = tenantServicesConfiguration;
        }

        /// <summary>
        /// Create a builder populated with global services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public ContainerBuilder CreateBuilder(IServiceCollection services)
        {
            var builder = new ContainerBuilder();

            builder.Populate(services);

            return builder;
        }

        /// <summary>
        /// Create our serivce provider
        /// </summary>
        /// <param name="containerBuilder"></param>
        /// <returns></returns>
        public IServiceProvider CreateServiceProvider(ContainerBuilder containerBuilder)
        {
            MultiTenantContainer<T, TKey>? container = null;

            MultiTenantContainer<T, TKey> containerAccessor() => container!;

            containerBuilder
                .RegisterInstance(containerAccessor)
                .SingleInstance();

            container = new MultiTenantContainer<T, TKey>(containerBuilder.Build(), _tenantServicesConfiguration);

            return new AutofacServiceProvider(containerAccessor());
        }
    }
}
