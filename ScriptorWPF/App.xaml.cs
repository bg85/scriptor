using log4net;
using Microsoft.Extensions.DependencyInjection;
using ScriptorWPF.Services;
using System.Windows;

namespace ScriptorWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ILog _logger;

        public static IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            this.InitializeComponent();
            var services = new ServiceCollection();
            DependencyInjectionModule.RegisterServices(services);
            ServiceProvider = services.BuildServiceProvider();
            _logger = LogManager.GetLogger(this.GetType());
            services.AddSingleton(_logger);
            _logger.Info("Starting App");
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.Error("Unhandled exception occurred.", (Exception)e.ExceptionObject); 
            LogManager.Shutdown(); // Flush and shutdown log
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _logger.Info("App window launched.");
        }
    }
}