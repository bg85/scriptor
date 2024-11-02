using log4net;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace ScriptorABC.Services
{
    public interface IJanitor 
    {
        Task CleanOlderFiles(string location);
    }
    public class Janitor(ILog logger) : IJanitor
    {
        private readonly ILog _logger = logger;

        public async Task CleanOlderFiles(string location)
        {
            try
            {
                var storageFolder = await StorageFolder.GetFolderFromPathAsync(location);
                var files = await storageFolder.GetFilesAsync();

                foreach (var file in files)
                {
                    if (DateTime.Now.Subtract(file.DateCreated.Date).Days > 2)
                    {
                        await file.DeleteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception deleting old files.", ex);
            }
        }
    }
}
