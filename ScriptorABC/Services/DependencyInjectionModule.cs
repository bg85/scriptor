using log4net.Config;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System;

namespace ScriptorABC.Services
{
    public class DependencyInjectionModule
    {
        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<IResourceManager, ResourceManager>();
            services.AddSingleton<IVoiceRecorder, VoiceRecorder>();
            services.AddSingleton<ITranslator, Translator>();
            services.AddSingleton<IAnimator, Animator>();

            string credentialsPath = Path.Combine(AppContext.BaseDirectory, "Assets", "scriptor-client.json");
            string configPath = Path.Combine(AppContext.BaseDirectory, "log4net.xml");
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
            XmlConfigurator.ConfigureAndWatch(LogManager.GetRepository(GetType().Assembly), new FileInfo(configPath));

            ILog logger = LogManager.GetLogger(typeof(Program));
            services.AddSingleton<ILog>(logger);
            logger.Info("Starting Scriptor");
        }

        // Used this function to troubleshoot issue writting to Google Cloud Logs
        //private readonly CallSettings _retryAWhile = CallSettings.FromRetry(
        //   RetrySettings.FromExponentialBackoff(
        //       maxAttempts: 15,
        //       initialBackoff: TimeSpan.FromSeconds(3),
        //       maxBackoff: TimeSpan.FromSeconds(12),
        //       backoffMultiplier: 2.0,
        //       retryFilter: RetrySettings.FilterForStatusCodes(StatusCode.Internal, StatusCode.DeadlineExceeded)));

        //private void WriteLogEntry(string logId)
        //{
        //    var client = LoggingServiceV2Client.Create();
        //    LogName logName = new LogName("scriptor-436001", logId);
        //    var jsonPayload = new Struct()
        //    {
        //        Fields =
        //        {
        //            { "name", Value.ForString("King Arthur") },
        //            { "quest", Value.ForString("Find the Holy Grail") },
        //            { "favorite_color", Value.ForString("Blue") }
        //        }
        //    };
        //    LogEntry logEntry = new LogEntry
        //    {
        //        LogNameAsLogName = logName,
        //        Severity = LogSeverity.Info,
        //        JsonPayload = jsonPayload
        //    };
        //    MonitoredResource resource = new MonitoredResource { Type = "global" };
        //    IDictionary<string, string> entryLabels = new Dictionary<string, string>
        //    {
        //        { "size", "large" },
        //        { "color", "blue" }
        //    };
        //    client.WriteLogEntries(logName, resource, entryLabels,
        //        new[] { logEntry }, _retryAWhile);
        //    Console.WriteLine($"Created log entry in log-id: {logId}.");
        //}
    }
}
