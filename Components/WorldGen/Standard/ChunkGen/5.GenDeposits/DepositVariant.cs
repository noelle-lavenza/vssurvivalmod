﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using System.Linq;

namespace Vintagestory.ServerMods
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DepositVariant : WorldPropertyVariant
    {
        public string fromFile;

        [JsonProperty]
        public new string Code;

        /// <summary>
        /// Amount of deposits per serverchunk-column 
        /// </summary>
        [JsonProperty]
        public float TriesPerChunk;

        [JsonProperty]
        public string Generator;

        [JsonProperty]
        public bool WithOreMap;

        [JsonProperty]
        public string HandbookPageCode;

        [JsonProperty]
        public bool WithBlockCallback;

        [JsonProperty, JsonConverter(typeof(JsonAttributesConverter))]
        public JsonObject Attributes;

        [JsonProperty]
        public ClimateConditions Climate;

        [JsonProperty]
        public DepositVariant[] ChildDeposits;


        public DepositGeneratorBase GeneratorInst;
        public MapLayerBase OreMapLayer;

        int noiseSizeOre;
        int regionSize;
        int chunksize;
        ICoreServerAPI api;

        public void InitWithoutGenerator(ICoreServerAPI api)
        {
            this.api = api;
            regionSize = api.WorldManager.RegionSize;
            chunksize = api.World.BlockAccessor.ChunkSize;
            noiseSizeOre = regionSize / TerraGenConfig.oreMapScale;
        }

        public void Init(ICoreServerAPI api, LCGRandom depositRand, NormalizedSimplexNoise noiseGen)
        {
            this.api = api;
            InitWithoutGenerator(api);

            if (Generator == null)
            {
                api.World.Logger.Error("Error in deposit variant in file {0}: No generator defined! Must define a generator.", fromFile, Generator);
            } else
            {
                GeneratorInst = DepositGeneratorRegistry.CreateGenerator(Generator, Attributes, api, this, depositRand, noiseGen);
                if (GeneratorInst == null)
                {
                    api.World.Logger.Error("Error in deposit variant in file {0}: No generator with code '{1}' found!", fromFile, Generator);
                }
            }
            
            if (Code == null)
            {
                api.World.Logger.Error("Error in deposit variant in file {0}: Deposit has no code! Defaulting to 'unknown'", fromFile);
                Code = "unknown";
            }
        }
        

        public void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ)
        {
            IntMap map;

            if (OreMapLayer != null && !mapRegion.OreMaps.ContainsKey(Code))
            {
                map = new IntMap();
                map.Size = noiseSizeOre + 1;
                map.BottomRightPadding = 1;
                map.Data = OreMapLayer.GenLayer(regionX * noiseSizeOre, regionZ * noiseSizeOre, noiseSizeOre + 1, noiseSizeOre + 1);
                mapRegion.OreMaps[Code] = map;
            }

            if (ChildDeposits != null)
            {
                for (int k = 0; k < ChildDeposits.Length; k++)
                {
                    DepositVariant childVariant = ChildDeposits[k];
                    if (childVariant.OreMapLayer != null && !mapRegion.OreMaps.ContainsKey(childVariant.Code))
                    {
                        map = new IntMap();
                        map.Size = noiseSizeOre + 1;
                        map.BottomRightPadding = 1;
                        map.Data = childVariant.OreMapLayer.GenLayer(regionX * noiseSizeOre, regionZ * noiseSizeOre, noiseSizeOre + 1, noiseSizeOre + 1);
                        mapRegion.OreMaps[childVariant.Code] = map;
                    }
                }
            }
        }

        public float GetOreMapFactor(int chunkx, int chunkz)
        {
            IMapRegion originMapRegion = api.WorldManager.GetMapRegion(chunkx * chunksize / regionSize, chunkz * chunksize / regionSize);
            if (originMapRegion == null) return 0;
            int lx = (chunkx * chunksize + chunksize / 2) % regionSize;
            int lz = (chunkz * chunksize + chunksize / 2) % regionSize;

            IntMap map = null;
            originMapRegion.OreMaps.TryGetValue(Code, out map);
            if (map != null)
            {
                float posXInRegionOre = GameMath.Clamp((float)lx / regionSize * noiseSizeOre, 0, noiseSizeOre - 1);
                float posZInRegionOre = GameMath.Clamp((float)lz / regionSize * noiseSizeOre, 0, noiseSizeOre - 1);

                int oreDist = originMapRegion.OreMaps[Code].GetUnpaddedColorLerped(posXInRegionOre, posZInRegionOre);

                return (oreDist & 0xff) / 255f;
            }

            return 0;
        }

        public float GetAbsAvgQuantity()
        {
            return GeneratorInst.GetAbsAvgQuantity();
        }




        public DepositVariant Clone()
        {
            DepositVariant var = new DepositVariant()
            {
                fromFile = fromFile,
                Code = Code,
                TriesPerChunk = TriesPerChunk,
                Generator = Generator,
                WithOreMap = WithOreMap,
                WithBlockCallback = WithBlockCallback,
                Attributes = Attributes?.Clone(),
                Climate = Climate?.Clone(),
                ChildDeposits = ChildDeposits == null ? null : (DepositVariant[])ChildDeposits.Clone(),
                OreMapLayer = OreMapLayer,
            };

            var.GeneratorInst = DepositGeneratorRegistry.CreateGenerator(Generator, Attributes, api, var, GeneratorInst.DepositRand, GeneratorInst.DistortNoiseGen);

            return var;
        }
    }


    [JsonObject(MemberSerialization.OptIn)]
    public class ClimateConditions
    {
        [JsonProperty]
        public float MinTemp = -50;

        [JsonProperty]
        public float MaxTemp = 50;

        [JsonProperty]
        public float MinRain;

        [JsonProperty]
        public float MaxRain = 1;
        

        [JsonProperty]
        public float MaxY = 1;

        public ClimateConditions Clone()
        {
            return new ClimateConditions()
            {
                MinRain = MinRain,
                MinTemp = MinTemp,
                MaxRain = MaxRain,
                MaxTemp = MaxTemp,
                MaxY = MaxY
            };
        }
    }
}