using log4net;
using ScriptorABC.Models;
using System.IO;
using System.Reflection;

namespace ScriptorABC.Services
{
    public interface IResourceManager
    {
        Result<string> GetResourceContent(string resourceName);
    }

    public class ResourceManager(ILog logger) : IResourceManager
    {
        private readonly ILog _logger = logger;

        public Result<string> GetResourceContent(string resourceName)
        {
            _logger.Info("Requesting resource.");

            var result = new Result<string>();
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            result.Value = reader.ReadToEnd();
                            result.Success = true;
                        }
                    }
                    else
                    {
                        result.Message = $"Resource not found: {resourceName}.";
                        result.Success = false;
                        _logger.Error(result.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Unable to get the file content for translation.", ex);
            }

            return result;
        }
    }
}
