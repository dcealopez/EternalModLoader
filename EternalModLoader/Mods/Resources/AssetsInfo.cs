using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace EternalModLoader.Mods.Resources
{
    /// <summary>
    /// AssetsInfo JSON object class
    /// </summary>
    public class AssetsInfo
    {
        /// <summary>
        /// Layers list
        /// </summary>
        public IList<AssetsInfoLayer> Layers;

        /// <summary>
        /// Maps list
        /// </summary>
        public IList<AssetsInfoMap> Maps;

        /// <summary>
        /// Resource files to load/remove in a map
        /// </summary>
        public IList<AssetsInfoResource> Resources;

        /// <summary>
        /// Assets info list
        /// </summary>
        public IList<AssetsInfoAsset> Assets;

        /// <summary>
        /// Deserializes an AssetsInfo object from a JSON string
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>the deserialized AssetsInfo object</returns>
        public static AssetsInfo FromJson(string json)
        {
            AssetsInfo assetsInfo = new AssetsInfo();
            assetsInfo.Layers = default(List<AssetsInfoLayer>);
            assetsInfo.Maps = default(List<AssetsInfoMap>);
            assetsInfo.Resources = default(List<AssetsInfoResource>);
            assetsInfo.Assets = default(List<AssetsInfoAsset>);

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

                        switch (jsonReader.Value)
                        {
                            case "layers":
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

                                        if (assetsInfo.Layers == null)
                                        {
                                            assetsInfo.Layers = new List<AssetsInfoLayer>();
                                        }

                                        if ((string)jsonReader.Value == "name")
                                        {
                                            jsonReader.Read();

                                            var assetsInfoLayer = new AssetsInfoLayer();
                                            assetsInfoLayer.Name = (string)jsonReader.Value;
                                            assetsInfo.Layers.Add(assetsInfoLayer);
                                        }
                                    }
                                }
                                break;
                            case "maps":
                                {
                                    // Read maps array
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

                                        if (assetsInfo.Maps == null)
                                        {
                                            assetsInfo.Maps = new List<AssetsInfoMap>();
                                        }

                                        if ((string)jsonReader.Value == "name")
                                        {
                                            jsonReader.Read();

                                            var assetsInfoMap = new AssetsInfoMap();
                                            assetsInfoMap.Name = (string)jsonReader.Value;
                                            assetsInfo.Maps.Add(assetsInfoMap);
                                        }
                                    }
                                }
                                break;
                            case "resources":
                                {
                                    // Read resources array
                                    jsonReader.Read();
                                    AssetsInfoResource assetsInfoResource = null;

                                    while (jsonReader.Read())
                                    {
                                        if (jsonReader.TokenType == JsonToken.StartObject)
                                        {
                                            assetsInfoResource = new AssetsInfoResource();
                                            continue;
                                        }

                                        if (jsonReader.TokenType == JsonToken.EndObject)
                                        {
                                            assetsInfo.Resources.Add(assetsInfoResource);
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

                                        if (assetsInfo.Resources == null)
                                        {
                                            assetsInfo.Resources = new List<AssetsInfoResource>();
                                        }

                                        switch (jsonReader.Value)
                                        {
                                            case "name":
                                                jsonReader.Read();
                                                assetsInfoResource.Name = (string)jsonReader.Value;
                                                break;
                                            case "remove":
                                                jsonReader.Read();
                                                assetsInfoResource.Remove = (bool)jsonReader.Value;
                                                break;
                                            case "placeFirst":
                                                jsonReader.Read();
                                                assetsInfoResource.PlaceFirst = (bool)jsonReader.Value;
                                                break;
                                            case "placeBefore":
                                                jsonReader.Read();
                                                assetsInfoResource.PlaceBefore = (bool)jsonReader.Value;
                                                break;
                                            case "placeByName":
                                                jsonReader.Read();
                                                assetsInfoResource.PlaceByName = (string)jsonReader.Value;
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                }
                                break;
                            case "assets":
                                {
                                    // Read assets array
                                    jsonReader.Read();
                                    AssetsInfoAsset assetsInfoAsset = null;

                                    while (jsonReader.Read())
                                    {
                                        if (jsonReader.TokenType == JsonToken.StartObject)
                                        {
                                            assetsInfoAsset = new AssetsInfoAsset();
                                            continue;
                                        }

                                        if (jsonReader.TokenType == JsonToken.EndObject)
                                        {
                                            assetsInfo.Assets.Add(assetsInfoAsset);
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

                                        if (assetsInfo.Assets == null)
                                        {
                                            assetsInfo.Assets = new List<AssetsInfoAsset>();
                                        }

                                        switch (jsonReader.Value)
                                        {
                                            case "name":
                                                jsonReader.Read();
                                                assetsInfoAsset.Name = (string)jsonReader.Value;
                                                break;
                                            case "resourceType":
                                                jsonReader.Read();
                                                assetsInfoAsset.ResourceType = (string)jsonReader.Value;
                                                break;
                                            case "mapResourceType":
                                                jsonReader.Read();
                                                assetsInfoAsset.MapResourceType = (string)jsonReader.Value;
                                                break;
                                            case "version":
                                                jsonReader.Read();
                                                assetsInfoAsset.Version = (byte)((Int64)jsonReader.Value);
                                                break;
                                            case "streamDbHash":
                                                jsonReader.Read();
                                                assetsInfoAsset.StreamDbHash = (ulong)((Int64)jsonReader.Value);
                                                break;
                                            case "remove":
                                                jsonReader.Read();
                                                assetsInfoAsset.Remove = (bool)jsonReader.Value;
                                                break;
                                            case "placeBefore":
                                                jsonReader.Read();
                                                assetsInfoAsset.PlaceBefore = (bool)jsonReader.Value;
                                                break;
                                            case "placeByName":
                                                jsonReader.Read();
                                                assetsInfoAsset.PlaceByName = (string)jsonReader.Value;
                                                break;
                                            case "placeByType":
                                                jsonReader.Read();
                                                assetsInfoAsset.PlaceByType = (string)jsonReader.Value;
                                                break;
                                            case "specialByte1":
                                                jsonReader.Read();
                                                assetsInfoAsset.SpecialByte1 = (byte)((Int64)jsonReader.Value);
                                                break;
                                            case "specialByte2":
                                                jsonReader.Read();
                                                assetsInfoAsset.SpecialByte2 = (byte)((Int64)jsonReader.Value);
                                                break;
                                            case "specialByte3":
                                                jsonReader.Read();
                                                assetsInfoAsset.SpecialByte3 = (byte)((Int64)jsonReader.Value);
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            return assetsInfo;
        }
    }

    /// <summary>
    /// Layers object
    /// </summary>
    public class AssetsInfoLayer
    {
        /// <summary>
        /// Layer name
        /// </summary>
        public string Name;
    }

    /// <summary>
    /// Maps object
    /// </summary>
    public class AssetsInfoMap
    {
        /// <summary>
        /// Map name
        /// </summary>
        public string Name;
    }

    /// <summary>
    /// Resource file class
    /// </summary>
    public class AssetsInfoResource
    {
        /// <summary>
        /// File name of the resource file to load/remove on this map
        /// </summary>
        public string Name;

        /// <summary>
        /// Indicates whether or not the specified resource file should be removed
        /// so that it doesn't get loaded on this map
        /// </summary>
        public bool Remove;

        /// <summary>
        /// Indicates whether or not the specified resource file should be placed
        /// as the first resource in the map (i.e: highest priority)
        /// </summary>
        public bool PlaceFirst;

        /// <summary>
        /// Indicates whether or not the resource should be placed
        /// before or after the resource with PlaceByName name
        /// </summary>
        public bool PlaceBefore;

        /// <summary>
        /// Place by (before/after) name
        /// </summary>
        public string PlaceByName;
    }

    /// <summary>
    /// Assets object
    /// </summary>
    public class AssetsInfoAsset
    {
        /// <summary>
        /// The hash for the resource in StreamDb for .resources
        /// </summary>
        public ulong StreamDbHash;

        /// <summary>
        /// Resource type for .resources
        /// </summary>
        public string ResourceType;

        /// <summary>
        /// Version
        /// </summary>
        public byte Version;

        /// <summary>
        /// Asset name for .mapresources
        /// </summary>
        public string Name;

        /// <summary>
        /// Asset type for .mapresources
        /// </summary>
        public string MapResourceType;

        /// <summary>
        /// Indicates whether or not the asset should be removed
        /// from the container's map resources
        /// </summary>
        public bool Remove;

        /// <summary>
        /// Indicates whether or not the asset should be placed
        /// before or after the asset with PlaceByName name and PlaceByType type
        /// </summary>
        public bool PlaceBefore;

        /// <summary>
        /// Place by (before/after) name
        /// </summary>
        public string PlaceByName;

        /// <summary>
        /// (Optional) Place by (before/after) type
        /// Used in conjuction with PlaceByName, since multiple assets
        /// can have the same name in map resources
        /// </summary>
        public string PlaceByType;

        /// <summary>
        /// Special byte 1
        /// </summary>
        public byte SpecialByte1;

        /// <summary>
        /// Special byte 2
        /// </summary>
        public byte SpecialByte2;

        /// <summary>
        /// Special byte 3
        /// </summary>
        public byte SpecialByte3;
    }
}
