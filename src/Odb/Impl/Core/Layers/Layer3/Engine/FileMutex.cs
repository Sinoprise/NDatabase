using System.Collections.Generic;
using NDatabase.Tool.Wrappers;
using NDatabase.Tool.Wrappers.Map;

namespace NDatabase.Odb.Impl.Core.Layers.Layer3.Engine
{
    /// <summary>
    ///   A mutex to logically lock ODB database file
    /// </summary>
    /// <author>osmadja</author>
    public class FileMutex
    {
        private static readonly FileMutex Instance = new FileMutex();

        private readonly IDictionary<string, string> _openFiles;

        private FileMutex()
        {
            _openFiles = new OdbHashMap<string, string>();
        }

        public static FileMutex GetInstance()
        {
            lock (typeof (FileMutex))
            {
                return Instance;
            }
        }

        public virtual void ReleaseFile(string fileName)
        {
            lock (_openFiles)
            {
                _openFiles.Remove(fileName);
            }
        }

        public virtual void LockFile(string fileName)
        {
            lock (_openFiles)
            {
                _openFiles.Add(fileName, fileName);
            }
        }

        private bool CanOpenFile(string fileName)
        {
            lock (_openFiles)
            {
                string value;
                _openFiles.TryGetValue(fileName, out value);
                var canOpen = value == null;
                if (canOpen)
                    LockFile(fileName);

                return canOpen;
            }
        }

        public virtual bool OpenFile(string fileName)
        {
            var canOpenfile = CanOpenFile(fileName);

            if (canOpenfile)
                return true;

            if (OdbConfiguration.RetryIfFileIsLocked())
            {
                var nbRetry = 0;
                while (!CanOpenFile(fileName) && nbRetry < OdbConfiguration.GetNumberOfRetryToOpenFile())
                {
                    try
                    {
                        OdbThreadUtil.Sleep(OdbConfiguration.GetRetryTimeout());
                    }
                    catch
                    {
                        //TODO: check it
                    }
                    // nothing to do
                    nbRetry++;
                }
                if (nbRetry < OdbConfiguration.GetNumberOfRetryToOpenFile())
                    return true;
            }
            return false;
        }
    }
}