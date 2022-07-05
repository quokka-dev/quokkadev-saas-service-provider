using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using QuokkaDev.Saas.Abstractions;

namespace QuokkaDev.Saas.ServiceProvider
{
    public class MultiTenantContainerMiddleware<TTenant, TKey> where TTenant : Tenant<TKey>
    {
        private readonly RequestDelegate next;

        public MultiTenantContainerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context, Func<MultiTenantContainer<TTenant, TKey>> multiTenantContainerAccessor)
        {
            //Set to current tenant container.
            //Begin new scope for request as ASP.NET Core standard scope is per-request
            context.RequestServices = new AutofacServiceProvider(multiTenantContainerAccessor().GetCurrentTenantScope().BeginLifetimeScope());
            await next.Invoke(context);
        }
    }
}
