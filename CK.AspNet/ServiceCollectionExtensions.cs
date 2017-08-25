using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.AspNet
{
    /// <summary>
    /// Adds extension methods on <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="RequestMonitorStartupFilter" /> to the provided <paramref name="services"/>.
        /// It will correctly registers the <see cref="Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware"/> and the
        /// <see cref="RequestMonitorMiddleware"/> into the application pipeline.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddRequestMonitoring( this IServiceCollection services )
        {
            return services.AddRequestMonitoring( options => options.SwallowErrors = true );
        }

        /// <summary>
        /// Adds the <see cref="RequestMonitorStartupFilter" /> to the provided <paramref name="services"/>.
        /// It will correctly registers the <see cref="Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware"/> and the
        /// <see cref="RequestMonitorMiddleware"/> into the application pipeline.
        /// </summary>
        /// <param name="configure">Configuration lambda</param>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddRequestMonitoring( this IServiceCollection services, Action<RequestMonitorMiddlewareOptions> configure )
        {
            services.AddOptions();
            services.Configure( configure );
            services.AddSingleton<IStartupFilter, RequestMonitorStartupFilter>();
            return services;
        }

        /// <summary>
        /// Replaces all registrations of a <typeparamref name="TRegisteredType"/> with a singleton instance.
        /// Since this is low level functionnality, no check are done (replacement can be null) and it returns
        /// the number of replacement instead of the services collection itself as usual.
        /// </summary>
        /// <typeparam name="TRegisteredType">The registered type.</typeparam>
        /// <param name="services">This services.</param>
        /// <param name="replacement">Replacement instance. Can be null.</param>
        /// <returns>The number of replacements made.</returns>
        public static int Replace<TRegisteredType>( this IServiceCollection services, TRegisteredType replacement )
        {
            int count = 0;
            for( var i = 0; i < services.Count; i++ )
            {
                if( services[i].ServiceType == typeof( TRegisteredType ) )
                {
                    services[i] = new ServiceDescriptor( typeof( TRegisteredType ), replacement );
                    ++count;
                }
            }
            return count;
        }

        /// <summary>
        /// Replaces all registrations of a <typeparamref name="TRegisteredType"/> with a new associated concrete type,
        /// keeping the original lifetime.
        /// </summary>
        /// <typeparam name="TRegisteredType">The registered type.</typeparam>
        /// <typeparam name="TNewType">The new mapped type.</typeparam>
        /// <param name="services">This services.</param>
        /// <returns>The number of replacements made.</returns>
        public static int Replace<TRegisteredType, TNewType>( this IServiceCollection services )
        {
            int count = 0;
            for( var i = 0; i < services.Count; i++ )
            {
                if( services[i].ServiceType == typeof( TRegisteredType ) )
                {
                    services[i] = new ServiceDescriptor( typeof( TRegisteredType ), typeof( TNewType ), services[i].Lifetime );
                    ++count;
                }
            }
            return count;
        }
    }
}
