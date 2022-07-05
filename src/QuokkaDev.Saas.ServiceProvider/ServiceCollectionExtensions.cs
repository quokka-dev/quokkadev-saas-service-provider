using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using QuokkaDev.Saas.Abstractions;

namespace QuokkaDev.Saas.ServiceProvider
{
    public static class ServiceCollectionExtensions
    {
        public static IApplicationBuilder UseMultiTenantContainer<TTenant, TKey>(this IApplicationBuilder builder) where TTenant : Tenant<TKey>
                => builder.UseMiddleware<MultiTenantContainerMiddleware<TTenant, TKey>>();

        public static IHostBuilder UseMultiTenantServiceProviderFactory<T, TKey>(this IHostBuilder builder, Action<T, ContainerBuilder> tenantServicesConfiguration)
            where T : Tenant<TKey>
        {
            builder.UseServiceProviderFactory(new MultiTenantServiceProviderFactory<T, TKey>(tenantServicesConfiguration));
            return builder;
        }
    }
}
