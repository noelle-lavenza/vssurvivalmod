﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods
{

    public class ChildDepositGenerator : DiscDepositGenerator
    {
        /// <summary>
        /// Radius in blocks, capped 64 blocks
        /// </summary>
        [JsonProperty]
        public NatFloat RandomTries;

        public ChildDepositGenerator(ICoreServerAPI api, DepositVariant variant, LCGRandom depositRand, NormalizedSimplexNoise noiseGen) : base(api, variant, depositRand, noiseGen)
        {
        }

        public override void Init()
        {

        }

        public void Init(Block inblock, string key, string value)
        {
            placeBlockByInBlockId[inblock.BlockId] = PlaceBlock.Resolve(variant.fromFile, Api, inblock, key, value);
        }

        public override void GenDeposit(IBlockAccessor blockAccessor, IServerChunk[] chunks, int originChunkX, int originChunkZ, BlockPos pos, ref Dictionary<BlockPos, DepositVariant> subDepositsToPlace)
        {
            IMapChunk heremapchunk = chunks[0].MapChunk;

            int depositGradeIndex = PlaceBlock.AllowedVariants != null ? DepositRand.NextInt(PlaceBlock.AllowedVariants.Length) : 0;

            int radius = Math.Min(64, (int)Radius.nextFloat(1, DepositRand));
            if (radius <= 0) return;
            radius++;

            bool shouldGenSurfaceDeposit = DepositRand.NextFloat() > 0.35f && SurfaceBlock != null;
            float tries = RandomTries.nextFloat(1, DepositRand);
            
            for (int i = 0; i < tries; i++)
            {
                targetPos.Set(
                    pos.X + DepositRand.NextInt(radius) - radius / 2,
                    pos.Y + DepositRand.NextInt(radius) - radius / 2,
                    pos.Z + DepositRand.NextInt(radius) - radius / 2
                );

                int lx = targetPos.X - pos.X / chunksize;
                int lz = targetPos.Z - pos.Z / chunksize;

                if (targetPos.Y <= 1 || targetPos.Y >= worldheight || lx < 0 || lz < 0 || lx >= chunksize || lz >= chunksize) continue;
                
                long index3d = ((targetPos.Y % chunksize) * chunksize + lz) * chunksize + lx;
                ushort blockId = chunks[targetPos.Y / chunksize].Blocks[index3d];
                
                ResolvedDepositBlock resolvedPlaceBlock = null;

                if (placeBlockByInBlockId.TryGetValue(blockId, out resolvedPlaceBlock))
                {
                    Block placeblock = resolvedPlaceBlock.Blocks[depositGradeIndex];

                    if (variant.WithBlockCallback)
                    {
                        placeblock.TryPlaceBlockForWorldGen(blockAccessor, targetPos, BlockFacing.UP);
                    }
                    else
                    {
                        chunks[targetPos.Y / chunksize].Blocks[index3d] = placeblock.BlockId;
                    }                    

                    if (shouldGenSurfaceDeposit)
                    {
                        int surfaceY = heremapchunk.RainHeightMap[lz * chunksize + lx];
                        int depth = surfaceY - targetPos.Y;
                        float chance = SurfaceBlockChance * Math.Max(0, 1 - depth / 8f);
                        if (surfaceY < worldheight && DepositRand.NextFloat() < chance)
                        {
                            index3d = (((surfaceY + 1) % chunksize) * chunksize + lz) * chunksize + lx;

                            Block belowBlock = Api.World.Blocks[chunks[surfaceY / chunksize].Blocks[((surfaceY % chunksize) * chunksize + lz) * chunksize + lx]];

                            if (belowBlock.SideSolid[BlockFacing.UP.Index] && chunks[(surfaceY + 1) / chunksize].Blocks[index3d] == 0)
                            {
                                chunks[(surfaceY + 1) / chunksize].Blocks[index3d] = surfaceBlockByInBlockId[blockId].Blocks[depositGradeIndex].BlockId;
                            }
                        }
                    }
                }
            }
        }
        

        protected override void beforeGenDeposit(IMapChunk heremapchunk, BlockPos pos)
        {
            // not called
        }

        protected override void loadYPosAndThickness(IMapChunk heremapchunk, int lx, int lz, BlockPos targetPos, double distanceToEdge)
        {
            // not called
        }
    }

}