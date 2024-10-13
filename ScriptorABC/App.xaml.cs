using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using ScriptorABC.Services;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ScriptorABC
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            var services = new ServiceCollection();
            new DependencyInjectionModule().RegisterServices(services);
            ServiceProvider = services.BuildServiceProvider();

            Current.UnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            LogManager.Shutdown(); // Flush and shutdown log
        }

        // Handle window closure to flush and shutdown log4net
        private void OnWindowClosed(object sender, Microsoft.UI.Xaml.WindowEventArgs e)
        {
            LogManager.Shutdown();  // Flush logs and shut down log4net
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();

            // Handle the main window's Closed event
            m_window.Closed += OnWindowClosed;

            m_window.Activate();
        }

        private Window m_window;
    }
}
