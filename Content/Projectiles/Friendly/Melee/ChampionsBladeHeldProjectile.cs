﻿using Macrocosm.Content.Items.Weapons.Melee;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Projectiles.Friendly.Melee
{
    internal class ChampionsBladeHeldProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 0;
            Projectile.height = 0;

            Projectile.aiStyle = -1;

            Projectile.DamageType = DamageClass.Melee;

            Projectile.penetrate = -1;

            Projectile.friendly = true;
            Projectile.hostile = false;

            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;

            Projectile.extraUpdates = 2;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 999;
        }

        private Player Player => Main.player[Projectile.owner];
        private ref float SwingDirection => ref Projectile.ai[0];
        private ref float Arc => ref Projectile.ai[1];

        // Client side only.
        private ChampionsBlade blade;

        private int shots;
        private int hitStacks;

        // So that the weapon doesn't "blink" during continuous use.
        private bool despawn;

        public override void OnSpawn(IEntitySource source)
        {
            blade = (source as EntitySource_ItemUse_WithAmmo).Item.ModItem as ChampionsBlade;

            hitStacks = blade.HitStacks;
            SwingDirection = blade.SwingDirection;
            Arc = Main.rand.NextFloat(MathHelper.PiOver2, MathHelper.TwoPi * 0.85f);

            Projectile.netUpdate = true;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override void AI()
        {
            if (!despawn)
            {
                Projectile.timeLeft = 2;
            }

            Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
            Player.heldProj = Projectile.whoAmI;
            Projectile.Center = Player.RotatedRelativePoint(Player.MountedCenter) + new Vector2(Player.direction * -3, -1);

            if (Player.noItems || Player.CCed || Player.ItemAnimationEndingOrEnded || Player.HeldItem.type != ModContent.ItemType<ChampionsBlade>())
            {
                despawn = true;
                if (blade is not null)
                {
                    blade.HitStacks = hitStacks;
                }

                return;
            }

            if (Main.myPlayer == Player.whoAmI)
            {
                Projectile.velocity = Projectile.Center.DirectionTo(Main.MouseWorld);
                Projectile.netUpdate = true;
            }

            var progress = 1f - (float)Player.itemAnimation / Player.itemAnimationMax;
            var x = progress - 1f;
            Projectile.rotation = Projectile.velocity.ToRotation()
                + (0.5f * Arc - Arc * (MathF.Sin((x * x * x - 0.5f) * MathHelper.Pi) + 1f) / 2f) * SwingDirection
                + Main.rand.NextFloat(0.1f);


            if (shots < 5 && Main.netMode != NetmodeID.MultiplayerClient && progress > 0.3f && progress < 0.5f && Main.rand.NextBool(2))
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromAI(),
                    Projectile.Center,
                    Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.PiOver4) * 12f,
                    ModContent.ProjectileType<ChampionsBladeBoltProjectile>(),
                    40,
                    2f
                );
                shots++;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hitStacks = Math.Min(hitStacks + 1, ChampionsBlade.MaxStacks);
            blade.ResetTimer = 0;
            Projectile.netUpdate = true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            var length = hitStacks == ChampionsBlade.MaxStacks ? 120 : 80;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + Projectile.rotation.ToRotationVector2() * length);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var texture = hitStacks == ChampionsBlade.MaxStacks ? TextureAssets.Projectile[Type].Value
                : ModContent.Request<Texture2D>("Macrocosm/Content/Items/Weapons/Melee/ChampionsBlade", AssetRequestMode.ImmediateLoad).Value;
            var rotation = Projectile.rotation + (Player.direction == 1 ? MathHelper.PiOver4 : MathHelper.Pi * 0.75f);
            var origin = new Vector2(Player.direction == 1 ? 10 : 67, 67);

            Main.spriteBatch.Draw(
                texture,
                Projectile.Center + Projectile.rotation.ToRotationVector2() * 16f - Main.screenPosition,
                null,
                lightColor,
                rotation,
                origin,
                Projectile.scale,
                Player.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
                0
            );

            return false;
        }
    }
}
