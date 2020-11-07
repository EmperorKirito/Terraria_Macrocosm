﻿using Macrocosm.Subworlds;
using Macrocosm.Tiles;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Macrocosm
{
    class MacrocosmWorld : ModWorld
    {
        public static int moonBiome = 0;
        // public static int marsBiome = 0;
        public override void TileCountsAvailable(int[] tileCounts)
        {
            moonBiome = tileCounts[ModContent.TileType<Regolith>()];
            // marsBiome = tileCounts[ModContent.TileType<NameOfTile>()];
        }
        public override void ResetNearbyTileEffects()
        {
            moonBiome = 0;
            // marsBiome = 0;
        }
        public override void PreUpdate()
        {
            if (Subworld.IsActive<Moon>())
            {
                Main.time += 0.125; // 
                // Main.time += 10; // One tenth the duration of a normal day/night
                // Main.time += 5; // Half the duration of a normal day/night
                // Main.time += 0.5; // Double the duration of a normal day/night
            }
        }
        public override TagCompound Save()
        {
            if (Main.gameMenu)
            {
                Main.sunTexture = ModContent.GetTexture("Terraria/Sun");
            }
            return null;
        }
    }
}
