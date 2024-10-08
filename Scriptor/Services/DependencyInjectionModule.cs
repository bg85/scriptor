using log4net.Config;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System;

namespace Scriptor.Services
{
    public class DependencyInjectionModule
    {
        public void RegisterServices(IServiceCollection services)
        {
            // Register your dependencies here
            services.AddSingleton<IResourceManager, ResourceManager>();
            services.AddSingleton<IVoiceRecorder, VoiceRecorder>();
            services.AddSingleton<ITranslator, Translator>();

            // Initialize log4net for cloud logging
            string credentialsPath = Path.Combine(AppContext.BaseDirectory, "Assets", "scriptor-client.json");
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
            XmlConfigurator.ConfigureAndWatch(LogManager.GetRepository(GetType().Assembly), new FileInfo("log4net.xml"));
            ILog logger = LogManager.GetLogger(typeof(Program));
            services.AddSingleton<ILog>(logger);
        }
    }
}
