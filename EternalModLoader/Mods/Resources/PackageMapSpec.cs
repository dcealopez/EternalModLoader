using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace EternalModLoader.Mods.Resources
{
    /// <summary>
    /// Package map spec info class
    /// </summary>
    public class PackageMapSpecInfo
    {
        /// <summary>
        /// Package map spec cobject
        /// </summary>
        public PackageMapSpec PackageMapSpec = null;

        /// <summary>
        /// Path to the package map spec JSON file
        /// </summary>
        public string PackageMapSpecPath = string.Empty;

        /// <summary>
        /// Is the JSON file invalid?
        /// </summary>
        public bool InvalidPackageMapSpec = false;

        /// <summary>
        /// Was the package map spec modified?
        /// </summary>
        public bool WasPackageMapSpecModified = false;
    }

    /// <summary>
    /// Package map spec class
    /// </summary>
    public class PackageMapSpec
    {
        /// <summary>
        /// File list
        /// </summary>
        public IList<PackageMapSpecFile> Files;

        /// <summary>
        /// Map-file references
        /// </summary>
        public IList<PackageMapSpecMapFileRef> MapFileRefs;

        /// <summary>
        /// Map list
        /// </summary>
        public IList<PackageMapSpecMap> Maps;

        /// <summary>
        /// Deserializes a PackageMapSpec object from a JSON string
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>the deserialized PackageMapSpec object</returns>
        public static PackageMapSpec FromJson(string json)
        {
            PackageMapSpec packageMapSpec = new PackageMapSpec();
            packageMapSpec.Files = default(List<PackageMapSpecFile>);
            packageMapSpec.MapFileRefs = default(List<PackageMapSpecMapFileRef>);
            packageMapSpec.Maps = default(List<PackageMapSpecMap>);

            using (var stringReader = new StringReader(json))
            {
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    while (jsonReader.Read())
                    {
                        if (jsonReader.TokenType != JsonToken.PropertyName)
                        {
                            continue;
                        }

                        if ((string)jsonReader.Value == "files")
                        {
                            jsonReader.Read();

                            while (jsonReader.Read())
                            {
                                if (jsonReader.TokenType == JsonToken.EndObject)
                                {
                                    continue;
                                }

                                if (jsonReader.TokenType == JsonToken.EndArray)
                                {
                                    break;
                                }

                                if (jsonReader.TokenType != JsonToken.PropertyName)
                                {
                                    continue;
                                }

                                if (packageMapSpec.Files == null)
                                {
                                    packageMapSpec.Files = new List<PackageMapSpecFile>();
                                }

                                if ((string)jsonReader.Value == "name")
                                {
                                    jsonReader.Read();

                                    var packageMapSpecFile = new PackageMapSpecFile();
                                    packageMapSpecFile.Name = (string)jsonReader.Value;

                                    packageMapSpec.Files.Add(packageMapSpecFile);
                                }
                            }
                        }
                        else if ((string)jsonReader.Value == "mapFileRefs")
                        {
                            jsonReader.Read();
                            PackageMapSpecMapFileRef packageMapSpecMapFileRef = null;

                            while (jsonReader.Read())
                            {
                                if (jsonReader.TokenType == JsonToken.StartObject)
                                {
                                    packageMapSpecMapFileRef = new PackageMapSpecMapFileRef();
                                    continue;
                                }

                                if (jsonReader.TokenType == JsonToken.EndObject)
                                {
                                    packageMapSpec.MapFileRefs.Add(packageMapSpecMapFileRef);
                                    continue;
                                }

                                if (jsonReader.TokenType == JsonToken.EndArray)
                                {
                                    break;
                                }

                                if (jsonReader.TokenType != JsonToken.PropertyName)
                                {
                                    continue;
                                }

                                if (packageMapSpec.MapFileRefs == null)
                                {
                                    packageMapSpec.MapFileRefs = new List<PackageMapSpecMapFileRef>();
                                }

                                if ((string)jsonReader.Value == "file")
                                {
                                    jsonReader.Read();
                                    packageMapSpecMapFileRef.File = (int)((Int64)jsonReader.Value);
                                }
                                else if ((string)jsonReader.Value == "map")
                                {
                                    jsonReader.Read();
                                    packageMapSpecMapFileRef.Map = (int)((Int64)jsonReader.Value);
                                }
                            }
                        }
                        else if ((string)jsonReader.Value == "maps")
                        {
                            jsonReader.Read();

                            while (jsonReader.Read())
                            {
                                if (jsonReader.TokenType == JsonToken.EndObject)
                                {
                                    continue;
                                }

                                if (jsonReader.TokenType == JsonToken.EndArray)
                                {
                                    break;
                                }

                                if (jsonReader.TokenType != JsonToken.PropertyName)
                                {
                                    continue;
                                }

                                if (packageMapSpec.Maps == null)
                                {
                                    packageMapSpec.Maps = new List<PackageMapSpecMap>();
                                }

                                if ((string)jsonReader.Value == "name")
                                {
                                    jsonReader.Read();

                                    var packageMapSpecMap = new PackageMapSpecMap();
                                    packageMapSpecMap.Name = (string)jsonReader.Value;
                                    packageMapSpec.Maps.Add(packageMapSpecMap);
                                }
                            }
                        }
                    }
                }
            }

            return packageMapSpec;
        }

        /// <summary>
        /// Serializes a PackageMapSpec object into a JSON string
        /// and writes it to the specified file path
        /// </summary>
        /// <param name="path">file path to write the JSON to</param>
        public void WriteToAsJson(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, EternalModLoader.BufferSize))
            {
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.Write("{\"files\":[");

                    for (int i = 0; i < Files.Count; i++)
                    {
                        streamWriter.Write("{\"name\":\"");
                        streamWriter.Write(Files[i].Name);
                        streamWriter.Write("\"}");

                        if (i != Files.Count - 1)
                        {
                            streamWriter.Write(",");
                        }
                    }

                    streamWriter.Write("],\"mapFileRefs\":[");

                    for (int i = 0; i < MapFileRefs.Count; i++)
                    {
                        streamWriter.Write("{\"file\":");
                        streamWriter.Write(MapFileRefs[i].File);
                        streamWriter.Write(",\"map\":");
                        streamWriter.Write(MapFileRefs[i].Map);
                        streamWriter.Write("}");

                        if (i != MapFileRefs.Count - 1)
                        {
                            streamWriter.Write(",");
                        }
                    }

                    streamWriter.Write("],\"maps\":[");

                    for (int i = 0; i < Maps.Count; i++)
                    {
                        streamWriter.Write("{\"name\":\"");
                        streamWriter.Write(Maps[i].Name);
                        streamWriter.Write("\"}");

                        if (i != Maps.Count - 1)
                        {
                            streamWriter.Write(",");
                        }
                    }

                    streamWriter.Write("]}");
                }
            }
        }
    }

    /// <summary>
    /// Package map spec file class
    /// </summary>
    public class PackageMapSpecFile
    {
        /// <summary>
        /// File name
        /// </summary>
        public string Name;
    }

    /// <summary>
    /// Package map spec map-file reference class
    /// </summary>
    public class PackageMapSpecMapFileRef
    {
        /// <summary>
        /// File index
        /// </summary>
        public int File;

        /// <summary>
        /// Map index
        /// </summary>
        public int Map;
    }

    /// <summary>
    /// Package map spec map class
    /// </summary>
    public class PackageMapSpecMap
    {
        /// <summary>
        /// Map name
        /// </summary>
        public string Name;
    }
}
