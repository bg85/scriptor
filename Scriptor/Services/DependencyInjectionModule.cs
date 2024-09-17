using Microsoft.Extensions.DependencyInjection;

namespace Scriptor.Services
{
    public class DependencyInjectionModule
    {
        public void RegisterServices(IServiceCollection services)
        {
            // Register your dependencies here
            services.AddSingleton<IVoiceRecorder, VoiceRecorder>();
        }
    }
}
