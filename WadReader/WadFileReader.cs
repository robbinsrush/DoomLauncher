﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WadReader
{
    public class WadFileReader
    {
        private static readonly string[] s_mapData = new string[] { "THINGS", "LINEDEFS", "SIDEDEFS", "VERTEXES", "SEGS", "SSECTORS", "NODES", "SECTORS", "REJECT", "BLOCKMAP", "BEHAVIOR",
            "TEXTMAP", "ZNODES", "ENDMAP", "DIALOGUE", "SCRIPTS", };
        private static readonly string[] s_importantMapData = new string[] { "BLOCKMAP", "VERTEXES", "SECTORS", "SIDEDEFS", "LINEDEFS", "SSECTORS", "NODES", "SEGS" };

        private static readonly string s_mapdataRegex = @"^GL_\S+";

        private readonly Stream m_stream;

        public WadFileReader(Stream stream)
        {
            m_stream = stream;

            WadHeader = new WadHeader(stream);
        }

        public WadType WadType
        {
            get { return WadHeader.WadType; }
        }

        public List<FileLump> ReadLumps()
        {
            return WadHeader.ReadLumps();
        }

        public static List<FileLump> GetMapMarkerLumps(List<FileLump> lumps)
        {
            List<FileLump> ret = new List<FileLump>();
            HashSet<string> mapLumps = new HashSet<string>(s_mapData);
            HashSet<string> important = new HashSet<string>(s_importantMapData);
            int mapLumpCount = 0, mapMarkerIndex = 0;

            for (int i = 0; i < lumps.Count; i++)
            {
                if (IsMapData(mapLumps, lumps[i]) && i > 0)
                {
                    mapMarkerIndex = i - 1;

                    while (i < lumps.Count - 1 && IsMapData(mapLumps, lumps[i]))
                    {
                        if (important.Contains(lumps[i].Name))
                            mapLumpCount++;
                        i++;
                    }

                    if (mapLumpCount > 4)
                        ret.Add(lumps[mapMarkerIndex]);

                    if (i == lumps.Count - 1)
                        break;

                    mapLumpCount = 0;
                    i--;
                }
            }

            return ret;
        }

        private static bool IsMapData(HashSet<string> mapLumps, FileLump lump)
        {
            return mapLumps.Contains(lump.Name) || Regex.IsMatch(lump.Name, s_mapdataRegex);
        }

        private WadHeader WadHeader { get; set; }
    }
}
