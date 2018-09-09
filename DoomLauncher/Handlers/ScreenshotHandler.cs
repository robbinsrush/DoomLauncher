﻿using DoomLauncher.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoomLauncher
{
    class ScreenshotHandler
    {
        public ScreenshotHandler(IDataSourceAdapter adapter, LauncherPath screenshotDirectory)
        {
            DataSourceAdapter = adapter;
            ScreenshotDirectory = screenshotDirectory;
        }

        public IEnumerable<IFileData> HandleNewScreenshots(ISourcePort sourcePort, IGameFile gameFile, string[] files)
        {
            List<IFileData> ret = new List<IFileData>();

            if (gameFile != null && gameFile.GameFileID.HasValue)
            {
                foreach (string file in files)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        string fileName = Guid.NewGuid().ToString() + fi.Extension;
                        fi.CopyTo(Path.Combine(ScreenshotDirectory.GetFullPath(), fileName));
                        FileData fileData = new FileData
                        {
                            FileName = fileName,
                            GameFileID = gameFile.GameFileID.Value,
                            SourcePortID = sourcePort.SourcePortID,
                            FileTypeID = FileType.Screenshot
                        };

                        DataSourceAdapter.InsertFile(fileData);
                        ret.Add(fileData);
                    }
                    catch
                    {
                        //failed, nothing to do
                    }
                }
            }

            return ret;
        }

        public LauncherPath ScreenshotDirectory { get; set; }
        public IDataSourceAdapter DataSourceAdapter { get; set; }
    }
}
