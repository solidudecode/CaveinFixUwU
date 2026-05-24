using System;
using System.Collections.Generic;
using Vintagestory;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent; 
using Vintagestory.ServerMods;  

namespace CaveinFix
{
    public class CaveinFixModSystem : ModSystem
    {
        private ICoreServerAPI _sapi;

        private Dictionary<int, int> rockToHardenedMap = new Dictionary<int, int>();

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override double ExecuteOrder()
        {
            return 0.95;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            _sapi = api;

            api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.TerrainFeatures, "standard");
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            foreach (Block block in api.World.Blocks)
            {
                if (block.Code == null) continue;
                if (!block.HasBehavior<BlockBehaviorUnstableRock>()) continue;

                string rockType = block.Variant["rock"];
                if (rockType == null) continue;

                Block hardenedBlock = api.World.GetBlock(new AssetLocation("caveinfix", "hardenedrock-" + rockType));
                if (hardenedBlock != null)
                {
                    rockToHardenedMap[block.Id] = hardenedBlock.Id;
                }
            }
        }

        private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
        {
            var chunks = request.Chunks;

            const int chunksize = GlobalConstants.ChunkSize;
            const int chunksizeSquared = chunksize * chunksize;

            int yMax = chunks[0].MapChunk.YMax;
            int cyMax = Math.Min(yMax / chunksize + 1, _sapi.WorldManager.MapSizeY / chunksize);

            for (int cy = 0; cy < cyMax; cy++)
            {
                IChunkBlocks chunkdata = chunks[cy].Data;
                int yStart = cy == 0 ? 1 : 0;
                int yEnd = chunksize - 1;

                for (int baseindex3d = 0; baseindex3d < chunksizeSquared; baseindex3d++)
                {
                    int blockIdBelow;
                    int index3d = baseindex3d + (yStart - 1) * chunksizeSquared;

                    if (yStart == 0)
                    {
                        if (cy > 0)
                        {
                            blockIdBelow = chunks[cy - 1].Data.GetBlockIdUnsafe(index3d + chunksize * chunksizeSquared);
                        }
                        else
                        {
                            blockIdBelow = 0;
                        }
                    }
                    else
                    {
                        blockIdBelow = chunkdata.GetBlockIdUnsafe(index3d);
                    }

                    for (int y = yStart; y <= yEnd; y++)
                    {
                        index3d += chunksizeSquared;
                        int currentBlockId = chunkdata.GetBlockIdUnsafe(index3d);

                        if (blockIdBelow == 0 && currentBlockId != 0)
                        {
                            if (rockToHardenedMap.TryGetValue(currentBlockId, out int hardenedId))
                            {
                                chunkdata.SetBlockUnsafe(index3d, hardenedId);
                                currentBlockId = hardenedId;
                            }
                        }

                        blockIdBelow = currentBlockId;
                    }
                }
            }
        }
    }
}
