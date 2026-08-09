using Microsoft.Extensions.DependencyInjection;
using NodeEditor.Net.Services.Registry;
using System;
using System.Collections.Generic;
using System.Text;

namespace NodeEditor.Blazor.Services
{
    public static class NodeRegistryServiceExtensions
    {
        extension(IServiceCollection services) 
        {
            public IServiceCollection AddKeyedNodeRegistry(
                object key,
                ServiceLifetime lifetime,
                Action<INodeRegistryService> configure)
            {
                services.Add(new ServiceDescriptor(
                    serviceType: typeof(INodeRegistryService),
                    serviceKey: key,
                    factory: (serviceProvider, _) =>
                    {
                        // DI supplies all constructor dependencies of NodeRegistryService.
                        var registry = ActivatorUtilities.CreateInstance<NodeRegistryService>(
                            serviceProvider);

                        // Registration occurs once when this keyed instance is created.
                        configure(registry);
                        //var registry = new NodeRegistryService(new NodeDiscoveryService());
                        //configure(registry);
                        
                        return registry;
                    },
                    lifetime: lifetime));

                return services;
            }
        }
    }
}
