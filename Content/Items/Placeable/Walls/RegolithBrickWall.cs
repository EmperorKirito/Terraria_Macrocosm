using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Macrocosm.Content.Items.Placeable.Blocks;

namespace Macrocosm.Content.Items.Placeable.Walls
{
    public class RegolithBrickWall : ModItem
    {
        public override void SetStaticDefaults()
        {

        }

        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 12;
            Item.maxStack = Item.CommonMaxStack;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 7;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createWall = ModContent.WallType<Tiles.Walls.RegolithBrickWall>();
        }

        public override void AddRecipes()
        {

            Recipe recipe = Recipe.Create(Type, 4);
            recipe.AddIngredient<RegolithBrick>();
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }
    }
}