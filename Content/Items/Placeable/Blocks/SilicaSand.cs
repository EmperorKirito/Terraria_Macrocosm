﻿using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Items.Placeable.Blocks
{
	public class SilicaSand : ModItem
	{
		public override void SetStaticDefaults() 
		{
			Item.ResearchUnlockCount = 200;
			ItemID.Sets.ExtractinatorMode[Type] = Type;
		}

		public override void SetDefaults()
		{
			Item.width = 12;
			Item.height = 12;
			Item.maxStack = 9999;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.createTile = ModContent.TileType<Tiles.Blocks.SilicaSand>();
			//Item.ammo = AmmoID.Sand; Using this Sand in the Sandgun would require PickAmmo code and changes to SilicaSandProjectile or a new ModProjectile.
		}

		public override void AddRecipes()
		{

		}
	}
}