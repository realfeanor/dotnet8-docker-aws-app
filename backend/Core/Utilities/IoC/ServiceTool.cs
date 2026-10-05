using System;

namespace Core.Utilities.IoC
{
    // Attribute-based aspects use the application's existing container for singleton dependencies.
    public static class ServiceTool
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ServiceProvider = serviceProvider;
        }
    }
}
