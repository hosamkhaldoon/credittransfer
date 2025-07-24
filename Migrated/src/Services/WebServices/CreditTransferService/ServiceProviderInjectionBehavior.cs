using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Description;
using CoreWCF.Dispatcher;
using Microsoft.Extensions.DependencyInjection;

namespace CreditTransfer.Services.WcfService;

/// <summary>
/// WCF behavior that injects the service provider into instance contexts
/// Enables dependency injection for authentication and other services
/// </summary>
public class ServiceProviderInjectionBehavior : IServiceBehavior
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceProviderInjectionBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, 
        System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
    {
        // No binding parameters needed
    }

    public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
    {
        // Apply service provider injection to all endpoints
        foreach (var endpoint in serviceHostBase.ChannelDispatchers)
        {
            if (endpoint is ChannelDispatcher channelDispatcher)
            {
                foreach (var endpointDispatcher in channelDispatcher.Endpoints)
                {
                    // Add our custom instance provider that injects the service provider
                    endpointDispatcher.DispatchRuntime.InstanceProvider = 
                        new ServiceProviderInstanceProvider(_serviceProvider, endpointDispatcher.DispatchRuntime.InstanceProvider);
                }
            }
        }
    }

    public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
    {
        // Validation logic if needed
    }
}

/// <summary>
/// Custom instance provider that injects service provider into instance contexts
/// </summary>
public class ServiceProviderInstanceProvider : IInstanceProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IInstanceProvider? _originalProvider;

    public ServiceProviderInstanceProvider(IServiceProvider serviceProvider, IInstanceProvider? originalProvider)
    {
        _serviceProvider = serviceProvider;
        _originalProvider = originalProvider;
    }

    public object GetInstance(InstanceContext instanceContext)
    {
        // Inject service provider into instance context
        instanceContext.Extensions.Add(new ServiceProviderExtension(_serviceProvider));
        
        // Use original provider or create using service provider
        if (_originalProvider != null)
        {
            return _originalProvider.GetInstance(instanceContext);
        }
        
        // Fallback: create instance using service provider
        return _serviceProvider.GetRequiredService<CreditTransferWcfService>();
    }

    public object GetInstance(InstanceContext instanceContext, Message message)
    {
        // Inject service provider into instance context
        instanceContext.Extensions.Add(new ServiceProviderExtension(_serviceProvider));
        
        // Use original provider or create using service provider
        if (_originalProvider != null)
        {
            return _originalProvider.GetInstance(instanceContext, message);
        }
        
        // Fallback: create instance using service provider
        return _serviceProvider.GetRequiredService<CreditTransferWcfService>();
    }

    public void ReleaseInstance(InstanceContext instanceContext, object instance)
    {
        // Clean up service provider extension
        var extension = instanceContext.Extensions.Find<ServiceProviderExtension>();
        if (extension != null)
        {
            instanceContext.Extensions.Remove(extension);
        }
        
        // Use original provider cleanup if available
        _originalProvider?.ReleaseInstance(instanceContext, instance);
        
        // If instance is disposable, dispose it
        if (instance is IDisposable disposableInstance)
        {
            disposableInstance.Dispose();
        }
    }
} 