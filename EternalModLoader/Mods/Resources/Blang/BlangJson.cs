using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace EternalModLoader.Mods.Resources.Blang
{
    /// <summary>
    /// BlangJson class
    /// </summary>
    public class BlangJson
    {
        /// <summary>
        /// List of blang strings
        /// </summary>
        public IList<BlangJsonString> Strings;

        /// <summary>
        /// Deserializes a BlangJson object from a JSON string
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>the deserialized BlangJson object</returns>
        public static BlangJson FromJson(string json)
        {
            BlangJson blangJson = new BlangJson();
            blangJson.Strings = default(List<BlangJsonString>);

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

                        if ((string)jsonReader.Value == "strings")
                        {
                            jsonReader.Read();
                            BlangJsonString blangJsonString = null;

                            while (jsonReader.Read())
                            {
                                if (jsonReader.TokenType == JsonToken.StartObject)
                                {
                                    blangJsonString = new BlangJsonString();
                                    continue;
                                }

                                if (jsonReader.TokenType == JsonToken.EndObject)
                                {
                                    blangJson.Strings.Add(blangJsonString);
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

                                if (blangJson.Strings == null)
                                {
                                    blangJson.Strings = new List<BlangJsonString>();
                                }

                                if ((string)jsonReader.Value == "name")
                                {
                                    jsonReader.Read();
                                    blangJsonString.Name = (string)jsonReader.Value;
                                }
                                else if ((string)jsonReader.Value == "text")
                                {
                                    jsonReader.Read();
                                    blangJsonString.Text = (string)jsonReader.Value;
                                }
                            }
                        }
                    }
                }
            }

            return blangJson;
        }
    }

    /// <summary>
    /// BlangJsonString class
    /// </summary>
    public class BlangJsonString
    {
        /// <summary>
        /// String identifier
        /// </summary>
        public string Name;

        /// <summary>
        /// String text
        /// </summary>
        public string Text;
    }
}