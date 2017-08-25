using CK.Core;
using CK.Monitoring;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Diagnostics;
using CK.AspNet;
using CK.Monitoring.Handlers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting
{

    /// <summary>
    /// Adds extension methods on <see cref="IWebHostBuilder"/>.
    /// </summary>
    public static class CKAspNetWebHostBuilderExtensions
    {
        /// <summary>
        /// Uses <see cref="CK.Monitoring"/> during the web host building and creates the default <see cref="GrandOutput"/>, with default options.
        /// </summary>
        /// <param name="builder">Web host builder</param>
        /// <returns></returns>
        public static IWebHostBuilder UseMonitoring( this IWebHostBuilder builder ) => UseMonitoring<GrandOutputOptions>( builder, new GrandOutputOptions() );

        /// <summary>
        /// Uses <see cref="CK.Monitoring"/> during the web host building and creates the default <see cref="GrandOutput"/>, with <see cref="GrandOutputOptions"/> options.
        /// </summary>
        /// <param name="builder">Web host builder</param>
        /// <returns></returns>
        public static IWebHostBuilder UseMonitoring( this IWebHostBuilder builder, GrandOutputOptions options ) => UseMonitoring<GrandOutputOptions>( builder, options );

        /// <summary>
        /// Uses <see cref="CK.Monitoring"/> during the web host building and creates the default <see cref="GrandOutput"/>,
        /// with a custom implementation of <see cref="GrandOutputOptions"/> options.
        /// </summary>
        /// <typeparam name="TConfig">The custom <GrandOutputOptions /> option type.</typeparam>
        /// <param name="builder">Web host builder</param>
        /// <param name="options">Custom option type instance</param>
        /// <returns></returns>
        public static IWebHostBuilder UseMonitoring<TConfig>( this IWebHostBuilder builder, TConfig options ) where TConfig : GrandOutputOptions
        {
            builder.ConfigureLogging( ( ctx, l ) =>
            {
                ConfigureLogging( ctx, l, options ?? new GrandOutputOptions() );
            } );
            return builder;
        }

        /// <summary>
        /// Uses <see cref="CK.Monitoring"/> during the web host building and creates the default <see cref="GrandOutput"/>,
        /// and bounds the configuration from the given configuration section.
        /// </summary>
        /// <param name="builder">Web host builder</param>
        /// <param name="configuration">The root configuration object.</param>
        /// <param name="configurationPath">The path of the configuration </param>
        /// <returns></returns>
        public static IWebHostBuilder UseMonitoring<TConfig>( this IWebHostBuilder builder, IConfiguration configuration, string configurationPath ) where TConfig : GrandOutputOptions
        {
            builder.UseConfiguration( configuration );
            builder.ConfigureLogging( ( ctx, l ) =>
            {
                TConfig options = GetOptionsFromConfigurationAndWatchChanges<TConfig>( configuration, configurationPath );
                ConfigureLogging( ctx, l, options );
            } );
            builder.ConfigureServices( ( ctx, services ) =>
            {
                services.AddSingleton( GrandOutput.Default );
            } );
            return builder;
        }

        private static TConfig GetOptionsFromConfigurationAndWatchChanges<TConfig>( IConfiguration configuration, string configurationPath ) where TConfig : GrandOutputOptions
        {
            var applyConfiguration = new ApplyConfiguration<TConfig>( configuration, configurationPath );
            applyConfiguration.RegisterChangeCallback();
            return applyConfiguration.GetConfiguration();
        }

        private static void ConfigureLogging<TConfig>( WebHostBuilderContext ctx, ILoggingBuilder l, TConfig options ) where TConfig : GrandOutputOptions
        {
            var rootLogPath = Path.Combine( ctx.HostingEnvironment.ContentRootPath, options.RootLogPath );
            SystemActivityMonitor.RootLogPath = rootLogPath;

            var grandOutputConfig = options.CreateGrandOutputConfiguration();
            var grandOutput = GrandOutput.EnsureActiveDefault( grandOutputConfig );
            l.AddGrandOutput( grandOutput );
#if NET461
            if( options.HandleDiagnosticsEvents )
            {
                DiagnosticListener.AllListeners.Subscribe( new LogObserver<DiagnosticListener>( listener =>
                {
                    listener.Subscribe( new LogObserver<KeyValuePair<string, object>>( diagnosticEvent =>
                    {

                    } ) );
                } ) );
            }
#endif
        }

#if NET461
        class LogObserver<T> : IObserver<T>
        {
            public LogObserver( Action<T> callback ) { _callback = callback; }
            public void OnCompleted() { }
            public void OnError( Exception error ) { }
            public void OnNext( T value ) { _callback( value ); }

            private Action<T> _callback;
        }
#endif
    }
}
