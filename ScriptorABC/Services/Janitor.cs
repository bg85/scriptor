using log4net;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace ScriptorABC.Services
{
    public interface IJanitor 
    {
        Task CleanOlderFiles();
    }
    public class Janitor : IJanitor
    {
        private readonly ILog _logger;
        public Janitor(ILog logger) { 
            _logger = logger;
        }

        public async Task CleanOlderFiles()
        {
            try
            {
                var storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Recordings", CreationCollisionOption.OpenIfExists);
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
