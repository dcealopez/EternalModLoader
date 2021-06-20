﻿using Newtonsoft.Json;
using System;
using System.IO;

namespace EternalModLoader.Mods
{
    /// <summary>
    /// Mod info class
    /// </summary>
    public class Mod
    {
        /// <summary>
        /// Mod load priority
        /// </summary>
        public int LoadPriority;

        /// <summary>
        /// Required mod loader version to run the mod
        /// </summary>
        public int RequiredVersion;

        /// <summary>
        /// Deserializes a Mod object from a JSON string
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>the deserialized Mod object</returns>
        public static Mod FromJson(string json)
        {
            Mod mod = new Mod();

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

                        if ((string)jsonReader.Value == "loadPriority")
                        {
                            jsonReader.Read();
                            mod.LoadPriority = (int)((Int64)jsonReader.Value);
                        }
                        else if ((string)jsonReader.Value == "requiredVersion")
                        {
                            jsonReader.Read();
                            mod.RequiredVersion = (int)((Int64)jsonReader.Value);
                        }
                    }
                }
            }

            return mod;
        }
    }
}
