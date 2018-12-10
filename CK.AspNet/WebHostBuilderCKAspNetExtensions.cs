using CK.AspNet;
using CK.Core;
using CK.Monitoring;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.AspNetCore.Hosting
{

    /// <summary>
    /// Adds extension methods on <see cref="IWebHostBuilder"/>.
    /// </summary>
    public static class WebHostBuilderCKAspNetExtensions
    {
        /// <summary>
        /// Uses <see cref="CK.Monitoring"/> during the web host building and initializes the default <see cref="GrandOutput"/>,
        /// and bounds the configuration from the given configuration section.
        /// This automatically registers a <see cref="IActivityMonitor"/> as a scoped service in the services.
        /// </summary>
        /// <param name="builder">Web host builder</param>
        /// <param name="configurationPath">The path of the monitoring configuration in the global configuration.</param>
        /// <returns>The builder.</returns>
        public static IWebHostBuilder UseMonitoring( this IWebHostBuilder builder, string configurationPath = "Monitoring" )
        {
            return DoUseMonitoring( builder, null, configurationPath );
        }

        /// <summary>
        /// Uses <see cref="CK.Monitoring"/> during the web host building and initializes an instance of the <see cref="GrandOutput"/>
        /// that must not be null nor be the <see cref="GrandOutput.Default"/> and bounds the configuration from the given configuration section.
        /// This automatically registers a <see cref="IActivityMonitor"/> as a scoped service in the services.
        /// </summary>
        /// <param name="builder">Web host builder</param>
        /// <param name="grandOutput">The target <see cref="GrandOutput"/>.</param>
        /// <param name="configurationPath">The path of the monitoring configuration in the global configuration.</param>
        /// <returns>The builder.</returns>
        public static IWebHostBuilder UseMonitoring( this IWebHostBuilder builder, GrandOutput grandOutput, string configurationPath = "Monitoring" )
        {
            if( grandOutput == null ) throw new ArgumentNullException( nameof( grandOutput ) );
            if( grandOutput == GrandOutput.Default ) throw new ArgumentException( "The GrandOutput must not be the default one.", nameof( grandOutput ) );
            return DoUseMonitoring( builder, grandOutput, configurationPath );
        }

        /// <summary>
        /// Uses <see cref="CK.Monitoring"/> during the web host building and initializes the default <see cref="GrandOutput"/>,
        /// and bounds the configuration to the given configuration section.
        /// This automatically registers a <see cref="IActivityMonitor"/> as a scoped service in the services.
        /// </summary>
        /// <param name="builder">Web host builder</param>
        /// <param name="section">The configuration section.</param>
        /// <returns>The builder.</returns>
        public static IWebHostBuilder UseMonitoring( this IWebHostBuilder builder, IConfigurationSection section )
        {
            if( section == null ) throw new ArgumentNullException( nameof( section ) );
            return DoUseMonitoring( builder, null, section );
        }

        /// <summary>
        /// Uses <see cref="CK.Monitoring"/> during the web host building and initializes an instance of the <see cref="GrandOutput"/>
        /// that must not be null nor be the <see cref="GrandOutput.Default"/> and bounds the configuration to a configuration section.
        /// This automatically registers a <see cref="IActivityMonitor"/> as a scoped service in the services.
        /// </summary>
        /// <param name="builder">This Web host builder</param>
        /// <param name="grandOutput">The target <see cref="GrandOutput"/>.</param>
        /// <param name="section">The configuration section.</param>
        /// <returns>The builder.</returns>
        public static IWebHostBuilder UseMonitoring( this IWebHostBuilder builder, GrandOutput grandOutput, IConfigurationSection section )
        {
            if( grandOutput == null ) throw new ArgumentNullException( nameof( grandOutput ) );
            if( grandOutput == GrandOutput.Default ) throw new ArgumentException( "The GrandOutput must not be the default one.", nameof( grandOutput ) );
            if( section == null ) throw new ArgumentNullException( nameof( section ) );
            return DoUseMonitoring( builder, grandOutput, section );
        }

        /// <summary>
        /// Automatically provides a <see cref="ScopedHttpContext"/> service that enables scoped services
        /// to use the HttpContext.
        /// </summary>
        /// <remarks>
        /// This is much more efficient than the HttpContextAccessor. HttpContextAccessor remains the only
        /// way to have a singleton service depends on the HttpContext and must NEVER be used. Singleton
        /// services that MAY need the HttpContext must be designed with explicit HttpContext method parameter 
        /// injection.
        /// A contrario, Scoped services CAN easily depend on the HttpContext thanks to this ScopedHttpContext.
        /// </remarks>
        /// <param name="builder">This Web host builder.</param>
        /// <returns>The builder.</returns>
        public static IWebHostBuilder UseScopedHttpContext( this IWebHostBuilder builder ) => ScopedHttpContext.Install( builder );

        class PostInstantiationFilter : IStartupFilter
        {
            readonly GrandOutputConfigurationInitializer _initalizer;

            public PostInstantiationFilter( GrandOutputConfigurationInitializer initalizer )
            {
                _initalizer = initalizer;
            }

            public Action<IApplicationBuilder> Configure( Action<IApplicationBuilder> next )
            {
                return builder =>
                {
                    var lifeTime = builder.ApplicationServices.GetRequiredService<IApplicationLifetime>();
                    _initalizer.PostInitialze( lifeTime );
                    next( builder );
                };
            }
        }

        static IWebHostBuilder DoUseMonitoring( IWebHostBuilder builder, GrandOutput grandOutput, string configurationPath )
        {
            // Three steps initialization:
            // First creates the initializer instance.
            var initializer = new GrandOutputConfigurationInitializer( grandOutput );
            builder.ConfigureLogging( ( ctx, loggingBuilder ) =>
            {
                var section = ctx.Configuration.GetSection( configurationPath );
                // Second, give it the environment and its section.
                initializer.Initialize( ctx.HostingEnvironment, loggingBuilder, section );
            } );
            // Now, registers the PostInstantiationFilter as a transient object.
            // This startup filter will inject the Application service IApplicationLifetime.
            return AddPostInstantiationStartupFilterAndRegisterMonitor( builder, initializer );
        }

        /// <summary>
        /// Initialize from IConfigurationSection instead of configurationPath.
        /// </summary>
        static IWebHostBuilder DoUseMonitoring( IWebHostBuilder builder, GrandOutput grandOutput, IConfigurationSection configuration )
        {
            var initializer = new GrandOutputConfigurationInitializer( grandOutput );
            builder.ConfigureLogging( ( ctx, loggingBuilder ) =>
            {
                initializer.Initialize( ctx.HostingEnvironment, loggingBuilder, configuration );
            } );
            return AddPostInstantiationStartupFilterAndRegisterMonitor( builder, initializer );
        }

        static IWebHostBuilder AddPostInstantiationStartupFilterAndRegisterMonitor( IWebHostBuilder builder, GrandOutputConfigurationInitializer initializer )
        {
            return builder.ConfigureServices( services =>
            {
                services.AddTransient<IStartupFilter>( _ => new PostInstantiationFilter( initializer ) );
                services.TryAddScoped<IActivityMonitor>( sp => new ActivityMonitor() );
            } );
        }

    }
}
