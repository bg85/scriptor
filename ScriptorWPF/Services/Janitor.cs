﻿using System.IO;
using log4net;


namespace ScriptorWPF.Services
{
    public interface IJanitor
    {
        void CleanOlderFiles(string location);
    }
    public class Janitor(ILog logger) : IJanitor
    {
        private readonly ILog _logger = logger;

        public void CleanOlderFiles(string location)
        {
            try
            {
                var directory = new DirectoryInfo(location);
                var files = directory.GetFiles().Where(f => DateTime.Now.Subtract(f.CreationTime).Days > 2);

                foreach (var file in files)
                {
                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Exception deleting old files.", ex);
            }
        }
    }
}
