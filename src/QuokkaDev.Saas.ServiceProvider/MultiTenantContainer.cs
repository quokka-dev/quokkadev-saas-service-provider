using Autofac;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Autofac.Core.Resolving;
using QuokkaDev.Saas.Abstractions;
using System.Diagnostics;

namespace QuokkaDev.Saas.ServiceProvider
{
    public class MultiTenantContainer<TTenant, TKey> : IContainer where TTenant : Tenant<TKey>
    {
        //This is the base application container
        private readonly IContainer _applicationContainer;
        //This action configures a container builder
        private readonly Action<TTenant, ContainerBuilder> _tenantContainerConfiguration;

        //This dictionary keeps track of all of the tenant scopes that we have created
        private readonly Dictionary<string, ILifetimeScope> _tenantLifetimeScopes = new();

        private readonly object _lock = new();
        private const string _multiTenantTag = "multitenantcontainer";

        public event EventHandler<LifetimeScopeBeginningEventArgs>? ChildLifetimeScopeBeginning;
        public event EventHandler<LifetimeScopeEndingEventArgs>? CurrentScopeEnding;
        public event EventHandler<ResolveOperationBeginningEventArgs>? ResolveOperationBeginning;

        public DiagnosticListener DiagnosticSource => _applicationContainer.DiagnosticSource;

        public IDisposer Disposer => GetCurrentTenantScope().Disposer;

        public object Tag => GetCurrentTenantScope().Tag;

        public IComponentRegistry ComponentRegistry => GetCurrentTenantScope().ComponentRegistry;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MultiTenantContainer()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            //Only for test purposes
        }

        public MultiTenantContainer(IContainer applicationContainer, Action<TTenant, ContainerBuilder> containerConfiguration)
        {
            _tenantContainerConfiguration = containerConfiguration;
            _applicationContainer = applicationContainer;
            _applicationContainer.ChildLifetimeScopeBeginning += ApplicationContainer_ChildLifetimeScopeBeginning;
            _applicationContainer.CurrentScopeEnding += ApplicationContainer_CurrentScopeEnding;
            _applicationContainer.ResolveOperationBeginning += ApplicationContainer_ResolveOperationBeginning;
        }

        /// <summary>
        /// Get the current teanant from the application container
        /// </summary>
        /// <returns></returns>
        private TTenant GetCurrentTenant()
        {
            //We have registered our TenantAccessService in Part 1, the service is available in the application container which allows us to access the current Tenant
            return _applicationContainer.Resolve<ITenantAccessService<TTenant, TKey>>().GetTenantAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get the scope of the current tenant
        /// </summary>
        /// <returns></returns>
        public virtual ILifetimeScope GetCurrentTenantScope()
        {
            return GetTenantScope(GetCurrentTenant().Identifier);
        }

        /// <summary>
        /// Get (configure on missing)
        /// </summary>
        /// <param name="tenantId"></param>
        /// <returns></returns>
        public ILifetimeScope GetTenantScope(string? tenantIdentifier)
        {
            //If no tenant (e.g. early on in the pipeline, we just use the application container)
            if (tenantIdentifier == null)
            {
                return _applicationContainer;
            }

            //If we have created a lifetime for a tenant, return
            if (_tenantLifetimeScopes.ContainsKey(tenantIdentifier))
            {
                return _tenantLifetimeScopes[tenantIdentifier];
            }

            lock (_lock)
            {
                if (_tenantLifetimeScopes.ContainsKey(tenantIdentifier))
                {
                    return _tenantLifetimeScopes[tenantIdentifier];
                }
                else
                {
                    //This is a new tenant, configure a new lifetimescope for it using our tenant sensitive configuration method
                    _tenantLifetimeScopes.Add(tenantIdentifier, _applicationContainer.BeginLifetimeScope(_multiTenantTag, a => _tenantContainerConfiguration(GetCurrentTenant(), a)));
                    return _tenantLifetimeScopes[tenantIdentifier];
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_lock)
                {
                    foreach (var scope in _tenantLifetimeScopes)
                    {
                        scope.Value.Dispose();
                    }

                    _applicationContainer.Dispose();
                }
            }
        }

        public ILifetimeScope BeginLifetimeScope()
        {
            return GetCurrentTenantScope().BeginLifetimeScope();
        }

        public ILifetimeScope BeginLifetimeScope(object tag)
        {
            return GetCurrentTenantScope().BeginLifetimeScope(tag);
        }

        public ILifetimeScope BeginLifetimeScope(Action<ContainerBuilder> configurationAction)
        {
            return GetCurrentTenantScope().BeginLifetimeScope(configurationAction);
        }

        public ILifetimeScope BeginLifetimeScope(object tag, Action<ContainerBuilder> configurationAction)
        {
            return GetCurrentTenantScope().BeginLifetimeScope(tag, configurationAction);
        }

        public object ResolveComponent(ResolveRequest request)
        {
            return GetCurrentTenantScope().ResolveComponent(request);
        }

        public ValueTask DisposeAsync()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        private void ApplicationContainer_ResolveOperationBeginning(object? sender, ResolveOperationBeginningEventArgs e)
        {
            this.ResolveOperationBeginning?.Invoke(sender, e);
        }

        private void ApplicationContainer_CurrentScopeEnding(object? sender, LifetimeScopeEndingEventArgs e)
        {
            this.CurrentScopeEnding?.Invoke(sender, e);
        }

        private void ApplicationContainer_ChildLifetimeScopeBeginning(object? sender, LifetimeScopeBeginningEventArgs e)
        {
            this.ChildLifetimeScopeBeginning?.Invoke(sender, e);
        }
    }
}
