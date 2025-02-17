﻿using System;
using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.ServerMods.WorldEdit;

namespace Vintagestory.ServerMods
{
    public static class RegisterUtil
    {
        public static void RegisterTool(ModSystem mod)
        {
            ((WorldEdit.WorldEdit)mod).RegisterTool("TreeGen", typeof(TreeGenTool));
        }
    }

    internal class TreeGenTool : ToolBase
    {
        Random rand = new Random();
        TreeGeneratorsUtil treeGenerators;

        
        public float MinTreeSize
        {
            get { return workspace.FloatValues["std.treeToolMinTreeSize"]; }
            set { workspace.FloatValues["std.treeToolMinTreeSize"] = value; }
        }

        public float MaxTreeSize
        {
            get { return workspace.FloatValues["std.treeToolMaxTreeSize"]; }
            set { workspace.FloatValues["std.treeToolMaxTreeSize"] = value; }
        }

        public string TreeVariant
        {
            get { return workspace.StringValues["std.treeToolTreeVariant"]; }
            set { workspace.StringValues["std.treeToolTreeVariant"] = value; }
        }

        public TreeGenTool(WorldEditWorkspace workspace, IBlockAccessorRevertable blockAccess) : base(workspace, blockAccess)
        {
            if (!workspace.FloatValues.ContainsKey("std.treeToolMinTreeSize")) MinTreeSize = 0.7f;
            if (!workspace.FloatValues.ContainsKey("std.treeToolMaxTreeSize")) MaxTreeSize = 1.3f;
            if (!workspace.StringValues.ContainsKey("std.treeToolTreeVariant")) TreeVariant = null;

            
        }

        public override Vec3i Size
        {
            get { return new Vec3i(0, 0, 0); }
        }


        public override bool OnWorldEditCommand(WorldEdit.WorldEdit worldEdit, CmdArgs args)
        {
            if (treeGenerators == null)
            {
                treeGenerators = new TreeGeneratorsUtil(worldEdit.sapi);
            }

            string cmd = args.PopWord();
            switch (cmd)
            {
                case "tsizemin":
                    {
                        float size = 0.7f;
                        if (args.Length > 0) float.TryParse(args[0], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out size);
                        MinTreeSize = size;

                        worldEdit.Good("Tree Min Size=" + size + " set.");

                        return true;
                    }

                case "tsizemax":
                    {
                        float size = 0.7f;
                        if (args.Length > 0) float.TryParse(args[0], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out size);
                        MaxTreeSize = size;

                        worldEdit.Good("Tree Max Size=" + size + " set.");

                        return true;
                    }

                case "tsize":
                    {
                        float min = 0.7f;
                        if (args.Length > 0) float.TryParse(args[0], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out min);
                        MinTreeSize = min;

                        float max = 1.3f;
                        if (args.Length > 1) float.TryParse(args[1], NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out max);
                        MaxTreeSize = max;

                        worldEdit.Good("Tree Min Size=" + min + ", max size =" + MaxTreeSize + " set.");

                        return true;
                    }

                case "trnd":
                    return true;

                case "tv":
                    int index = 0;

                    string variant = args.PopWord();

                    bool numeric = int.TryParse(variant, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out index);

                    treeGenerators.ReloadTreeGenerators();

                    if (numeric)
                    {
                        var val = treeGenerators.GetGenerator(index);
                        if (val.Key == null)
                        {
                            worldEdit.Bad("No such tree variant found.");
                            return true;
                        }

                        TreeVariant = val.Key.ToShortString();
                        worldEdit.Good("Tree variant " + val.Key + " set.");
                        
                    } else
                    {
                        if (variant != null && treeGenerators.GetGenerator(new AssetLocation(variant)) != null)
                        {
                            TreeVariant = variant;
                            worldEdit.Good("Tree variant " + variant + " set.");
                        } else
                        {
                            worldEdit.Bad("No such tree variant found.");
                        }
                    }

                    return true;

            }

            return false;
        }

        public override void OnBuild(WorldEdit.WorldEdit worldEdit, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack)
        {
            if (treeGenerators == null)
            {
                treeGenerators = new TreeGeneratorsUtil(worldEdit.sapi);
            }

            if (TreeVariant == null)
            {
                worldEdit.Bad("Please select a tree variant first.");
                return;
            }

            worldEdit.sapi.World.BlockAccessor.SetBlock(oldBlockId, blockSel.Position);
            blockSel.Position.Add(blockSel.Face.Opposite); // - prevented trees from growing o.O   - seems to work again and with this disabled trees float in the air 0.O

            ba.ReadFromStagedByDefault = true;

            treeGenerators.ReloadTreeGenerators();
            treeGenerators.RunGenerator(new AssetLocation(TreeVariant), ba, blockSel.Position, MinTreeSize + (float)rand.NextDouble() * (MaxTreeSize - MinTreeSize));

            ba.SetHistoryStateBlock(blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, oldBlockId, ba.GetStagedBlockId(blockSel.Position));
            ba.Commit();
        }

    }
}
