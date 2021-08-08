using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace EternalModLoader.Mods
{
    /// <summary>
    /// Mod info class
    /// </summary>
    public class Mod
    {
        /// <summary>
        /// Is this mod safe for online play?
        /// </summary>
        public bool IsSafeForOnline = true;

        /// <summary>
        /// Mod load priority
        /// </summary>
        public int LoadPriority;

        /// <summary>
        /// Required mod loader version to run the mod
        /// </summary>
        public int RequiredVersion;

        /// <summary>
        /// This mod's files
        /// </summary>
        public List<ModFile> Files = new List<ModFile>();

        /// <summary>
        /// Deserializes a Mod object from a JSON string
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>the deserialized Mod object</returns>
        public static void ReadValuesFromJson(Mod mod, string json)
        {
            using (var stringReader = new StringReader(json))
            {
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    while (jsonReader.Read())
                    {
                        if (jsonReader.TokenType == JsonToken.EndObject)
                        {
                            break;
                        }

                        if (jsonReader.TokenType != JsonToken.PropertyName)
                        {
                            continue;
                        }

                        switch (jsonReader.Value)
                        {
                            case "loadPriority":
                                jsonReader.Read();
                                mod.LoadPriority = (int)((Int64)jsonReader.Value);
                                break;
                            case "requiredVersion":
                                jsonReader.Read();
                                mod.RequiredVersion = (int)((Int64)jsonReader.Value);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
    }
}
