using log4net;
using System;
using System.IO;
using System.Reflection;

namespace ScriptorABC.Services
{
    public interface IResourceManager
    {
        string GetResourceContent(string resourceName);
    }

    public class ResourceManager : IResourceManager
    {
        private ILog _logger;

        public ResourceManager(ILog logger)
        {
            _logger = logger;
        }

        public string GetResourceContent(string resourceName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var content = reader.ReadToEnd();
                            return content;
                        }
                    }
                    else
                    {
                        _logger.Error($"Resource not found: {resourceName}.");
                        return "Resource not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Unable to get the file content for translation.", ex);
                throw;
            }
        }
    }
}
