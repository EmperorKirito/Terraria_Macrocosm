using Macrocosm.Content.Items.Blocks.Terrain;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Items.Walls
{
    public class RegolithWall : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 400;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableWall(ModContent.WallType<Tiles.Walls.RegolithWall>());
            Item.width = 24;
            Item.height = 24;
        }

        public override void AddRecipes()
        {
            CreateRecipe(4)
            .AddIngredient<Regolith>()
            .AddTile(TileID.WorkBenches)
            .DisableDecraft()
            .AddCustomShimmerResult(ModContent.ItemType<RegolithWallUnsafe>())
            .Register();
        }
    }

    public class RegolithWallUnsafe : RegolithWall
    {
        public override string Texture => base.Texture.Replace("Unsafe", "");

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            ItemID.Sets.DrawUnsafeIndicator[Type] = true;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.createWall = ModContent.WallType<Tiles.Walls.RegolithWallUnsafe>();
        }
    }
}