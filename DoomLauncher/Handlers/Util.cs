﻿using DoomLauncher.DataSources;
using DoomLauncher.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WadReader;

namespace DoomLauncher
{
    public static class Util
    {
        public static IEnumerable<object> TableToStructure(DataTable dt, Type type)
        {
            List<object> ret = new List<object>();
            object convertedObj;
            PropertyInfo[] properties = type.GetProperties().Where(x => x.GetSetMethod() != null && x.GetGetMethod() != null).ToArray();

            foreach (DataRow dr in dt.Rows)
            {
                object obj = Activator.CreateInstance(type);

                foreach (PropertyInfo pi in properties)
                {
                    Type pType = pi.PropertyType;

                    if (pType.IsGenericType && pType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        pType = pType.GetGenericArguments()[0];

                    if (dt.Columns.Contains(pi.Name) && ChangeType(dr[pi.Name].ToString(), pType, out convertedObj))
                        pi.SetValue(obj, convertedObj, null);
                }

                ret.Add(obj);
            }

            return ret;
        }

        public static bool ChangeType(string obj, Type t, out object convertedObj)
        {
            convertedObj = null;
            if (obj == null) return false;

            if (obj.GetType() == typeof(string) && t == typeof(string))
            {
                convertedObj = obj;
                return true;
            }
            else if (obj.GetType() == typeof(string) && t == typeof(bool) &&
                (obj == "0" || obj == "1"))
            {
                if (obj == "0")
                    convertedObj = false;
                else
                    convertedObj = true;
                return true;
            }
            else if (t.BaseType == typeof(Enum))
            {
                convertedObj = Convert.ToInt32(obj);
                return true;
            }

            MethodInfo method = t.GetMethod("TryParse", new[] { typeof(string), Type.GetType(string.Format("{0}&", t.FullName)) });

            if (method != null)
            {
                object[] args = new object[] { obj, convertedObj };

                if ((bool)method.Invoke(null, args))
                {
                    convertedObj = args[1];
                    return true;
                }
            }

            return false;
        }

        public static string GetMapStringFromWad(string file)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                FileStream fs = File.OpenRead(file);
                WadFileReader wadReader = new WadFileReader(fs);

                if (wadReader.WadType != WadType.Unknown)
                {
                    var mapLumps = WadFileReader.GetMapMarkerLumps(wadReader.ReadLumps()).OrderBy(x => x.Name).ToArray();
                    fs.Close();

                    Array.ForEach(mapLumps, x => sb.Append(x.Name + ", "));
                }
                else
                {
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                DisplayUnexpectedException(null, ex);
            }

            return sb.ToString();
        }

        public static void DisplayUnexpectedException(Form form, Exception ex)
        {
#if DEBUG
            throw ex;
#else
            if (form.InvokeRequired)
                form.Invoke(new Action<Form, Exception>(DisplayUnexpectedException), form, ex);
            else
                DisplayException(form, ex);
#endif
        }

        private static void DisplayException(Form form, Exception ex)
        {
            if (form != null && form.InvokeRequired)
            {
                form.Invoke(new Action<Form, Exception>(DisplayException), new object[] { form, ex });
            }
            else
            {
                TextBoxForm txt = new TextBoxForm
                {
                    Text = "Unexpected Error",
                    HeaderText = "An unexpected error occurred. Please submit the error report by clicking the link below. The report has been copied to your clipboard." + Environment.NewLine,
                    DisplayText = ex.ToString()
                };
                txt.SetLink("Click here to submit", GitHubRepository);
                Clipboard.SetText(txt.DisplayText);

                if (form == null)
                {
                    txt.ShowDialog();
                }
                else
                {
                    txt.StartPosition = FormStartPosition.CenterParent;
                    txt.ShowDialog(form);
                }
            }
        }

        public static string GitHubRepository => "https://github.com/hobomaster22/DoomLauncher";

        public static string DoomworldThread => "http://www.doomworld.com/vb/doom-general/69346-doom-launcher-doom-frontend-database/";

        public static string Realm667Thread => "http://realm667.com/index.php/en/kunena/doom-launcher";

        public static void SetDefaultSearchFields(SearchControl ctrlSearch)
        {
            string[] filters = new string[]
            {
                "Title",
                "Author",
                "Filename",
                "Description",
            };

            ctrlSearch.SetSearchFilters(filters);
            ctrlSearch.SetSearchFilter(filters[0], true);
            ctrlSearch.SetSearchFilter(filters[1], true);
            ctrlSearch.SetSearchFilter(filters[2], true);
        }

        public static GameFileSearchField[] SearchFieldsFromSearchCtrl(SearchControl ctrlSearch)
        {
            string[] items = ctrlSearch.GetSelectedSearchFilters();
            List<GameFileSearchField> ret = new List<GameFileSearchField>();
            GameFileFieldType type;

            foreach (string item in items)
            {
                if (Enum.TryParse(item, out type))
                {
                    ret.Add(new GameFileSearchField(type, GameFileSearchOp.Like, ctrlSearch.SearchText));
                }
            }

            return ret.ToArray();
        }

        public static List<ISourcePort> GetSourcePortsData(IDataSourceAdapter adapter)
        {
            List<ISourcePort> sourcePorts = adapter.GetSourcePorts().ToList();
            SourcePort noPort = new SourcePort
            {
                Name = "N/A",
                SourcePortID = -1
            };
            sourcePorts.Insert(0, noPort);
            return sourcePorts;
        }

        public static string[] GetSkills()
        {
            return new string[] { "1", "2", "3", "4", "5" };
        }

        public static string GetTimePlayedString(int minutes)
        {
            string ret = "Time Played: ";
            if (minutes < 60)
            {
                ret += string.Format("{0} minute{1}", minutes,
                    minutes == 1 ? string.Empty : "s");
            }
            else
            {
                double hours = Math.Round(minutes / 60.0, 2);
                ret += string.Format("{0} hour{1}", hours.ToString("N", CultureInfo.InvariantCulture),
                    hours == 1 ? string.Empty : "s");
            }

            return ret;
        }

        public static List<IGameFile> GetAdditionalFiles(IDataSourceAdapter adapter, IGameFile gameFile)
        {
            if (gameFile != null && !string.IsNullOrEmpty(gameFile.SettingsFiles))
                return GetAdditionalFiles(adapter, gameFile, gameFile.SettingsFiles);

            return new List<IGameFile>();
        }

        public static List<IGameFile> GetIWadAdditionalFiles(IDataSourceAdapter adapter, IGameFile gameFile)
        {
            if (gameFile != null && !string.IsNullOrEmpty(gameFile.SettingsFilesIWAD))
                return GetAdditionalFiles(adapter, gameFile, gameFile.SettingsFilesIWAD);

            return new List<IGameFile>();
        }

        public static List<IGameFile> GetSourcePortAdditionalFiles(IDataSourceAdapter adapter, IGameFile gameFile)
        {
            if (gameFile != null && !string.IsNullOrEmpty(gameFile.SettingsFilesSourcePort))
                return GetAdditionalFiles(adapter, gameFile, gameFile.SettingsFilesSourcePort);

            return new List<IGameFile>();
        }

        public static List<IGameFile> GetAdditionalFiles(IDataSourceAdapter adapter, ISourcePort sourcePort)
        {
            return GetAdditionalFiles(adapter, null, sourcePort.SettingsFiles);
        }

        private static List<IGameFile> GetAdditionalFiles(IDataSourceAdapter adapter, IGameFile gameFile, string property)
        {
            string[] fileNames = property.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            List<IGameFile> gameFiles = new List<IGameFile>();
            Array.ForEach(fileNames, x => gameFiles.Add(adapter.GetGameFile(x)));
            return gameFiles.Where(x => x != null).ToList();
        }

        [Conditional("DEBUG")]
        public static void ThrowDebugException(string msg)
        {
            throw new Exception(msg);
        }

        public static IEnumerable<ZipArchiveEntry> GetEntriesByExtension(ZipArchive za, string[] extensions)
        {
            List<ZipArchiveEntry> entries = new List<ZipArchiveEntry>();

            foreach (var ext in extensions)
            {
                 entries.AddRange(za.Entries
                     .Where(x => x.Name.Contains('.') && Path.GetExtension(x.Name).Equals(ext, StringComparison.OrdinalIgnoreCase)));
            }

            return entries;
        }

        public static string[] GetPkExtenstions()
        {
            return new string[] { ".pk3", ".pk7" };
        }

        public static string GetPkExtensionsCsv()
        {
            return ".pk3,.pk7";
        }

        public static GameFileFieldType[] DefaultGameFileUpdateFields
        {
            get
            {
                return new GameFileFieldType[]
                {
                    GameFileFieldType.Author,
                    GameFileFieldType.Title,
                    GameFileFieldType.Description,
                    GameFileFieldType.Downloaded,
                    GameFileFieldType.LastPlayed,
                    GameFileFieldType.ReleaseDate,
                    GameFileFieldType.Comments,
                    GameFileFieldType.Rating
                };
            }
        }

        public static GameFileFieldType[] GetSyncGameFileUpdateFields()
        {
            return DefaultGameFileUpdateFields.Union(new GameFileFieldType[] { GameFileFieldType.Map, GameFileFieldType.MapCount }).ToArray();
        }

        //Takes a file 'MAP01.wad' and makes it 'MAP01_GUID.wad'.
        //Checks if file with prefix MAP01 exists with same file length and returns that file (same file).
        //Otherwise a new file is extracted and returned.
        public static string ExtractTempFile(string tempDirectory, ZipArchiveEntry zae)
        {
            string ext = Path.GetExtension(zae.Name);
            string file = zae.Name.Replace(ext, string.Empty) + "_";
            string[] searchFiles = Directory.GetFiles(tempDirectory, file + "*");

            string matchingFile = searchFiles.FirstOrDefault(x => new FileInfo(x).Length == zae.Length);

            if (matchingFile == null)
            {
                string extractFile = Path.Combine(tempDirectory, string.Concat(file, Guid.NewGuid().ToString(), ext));
                zae.ExtractToFile(extractFile);
                return extractFile;
            }

            return matchingFile;
        }

        public static List<IIWadData> GetIWadsDataSource(IDataSourceAdapter adapter)
        {
            List<IIWadData> iwads = adapter.GetIWads().ToList();
            iwads.ForEach(x => x.FileName = RemoveExtension(x.FileName));
            return iwads;
        }

        public static string RemoveExtension(string fileName)
        {
            return fileName.Replace(Path.GetExtension(fileName), string.Empty);
        }

        public static string CleanDescription(string description)
        {
            string[] items = description.Split(new char[] { '\n' });
            StringBuilder sb = new StringBuilder();

            foreach (string item in items)
            {
                string text = Regex.Replace(item, @"\s+", " ");
                if (text.StartsWith(" "))
                    text = text.Substring(1);
                sb.Append(text);
                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }
    }
}
