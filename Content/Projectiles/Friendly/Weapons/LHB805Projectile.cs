using Macrocosm.Common.Utility;
using Macrocosm.Content.Gores;
using Macrocosm.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Macrocosm.Content.Projectiles.Friendly.Weapons
{
	public class LHB805Projectile : ModProjectile
	{

		private const int windupFrames = 4; // number of windup animaton frames
		private const int shootFrames = 6;  // number of shooting animaton frames

		private const int startTicksPerFrame = 8; // tpf at the start of the animation
		private const int maxTicksPerFrame = 3;   // tpf cap after fullFireRateTime

		private const int windupTime = 45;        // ticks till start of shooting 
		private const int fullFireRateTime = 80;  // ticks to reach full fire rate 

		private const int fireRateStart = 6;      // fireRateFreqStart the ticks period between fires at the start of shooting
		private const int fireRateCap = 1;        // fireRateFreqMaxthe ticks period between fires after fullFireRateTime

		public ref float AI_Windup => ref Projectile.ai[0];

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Minigun");
			Main.projFrames[Type] = 10;
			ProjectileID.Sets.NeedsUUID[Type] = true;

		}

		public override void SetDefaults()
		{
			Projectile.CloneDefaults(ProjectileID.LastPrism);
			Projectile.friendly = false;
			Projectile.width = 1;
			Projectile.height = 1;
		}

		private Player Player => Main.player[Projectile.owner];
		private bool StillInUse => Player.channel && Player.HasAmmo(Player.inventory[Player.selectedItem]) && !Player.noItems && !Player.CCed;
		private bool CanShoot => AI_Windup >= windupTime;

		public override bool PreDraw(ref Color lightColor)
		{
			Projectile.DrawAnimated(lightColor, Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None, new Vector2(5, 10));
			return false;
		}

		public override void PostDraw(Color lightColor)
		{
			Texture2D glowmask = ModContent.Request<Texture2D>("Macrocosm/Content/Projectiles/Friendly/Weapons/LHB805ProjectileGlow").Value;
			Projectile.DrawAnimatedGlowmask(glowmask, Color.White, Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None, new Vector2(5, 10));
		}

		public override void AI()
		{
			PlayerVisuals();
			Aim();
			Animate();
			Shoot();
			PlaySounds();

			AI_Windup++;

			if (!StillInUse)
				Projectile.Kill();
		}

		private void PlayerVisuals()
		{
			Projectile.Center = Player.Center;
			Projectile.rotation = Projectile.velocity.ToRotation();

			Projectile.spriteDirection = Projectile.direction;

			Player.ChangeDir(Projectile.direction);
			Player.heldProj = Projectile.whoAmI;
			Player.itemTime = 2;
			Player.itemAnimation = 2;

			Player.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
		}

		private void Aim()
		{
			// Get the player's current aiming direction as a normalized vector.
			Vector2 aim = Vector2.Normalize(Main.MouseWorld - Projectile.Center);
			if (aim.HasNaNs())
				aim = Vector2.UnitY;

			// Change a portion of the gun's current velocity so that it points to the mouse. This gives smooth movement over time.
			aim = Vector2.Normalize(Vector2.Lerp(Vector2.Normalize(Projectile.velocity), aim, 1));
			aim *= Player.HeldItem.shootSpeed;

			if (aim != Projectile.velocity)
				Projectile.netUpdate = true;

			Projectile.velocity = aim;
		}

		private void Animate()
		{
			Projectile.frameCounter++;

			int windupTicksPerFrame = (int)MathHelper.Clamp(MathHelper.Lerp(startTicksPerFrame, maxTicksPerFrame, AI_Windup / fullFireRateTime), maxTicksPerFrame, startTicksPerFrame);

			if (!CanShoot)
			{
				if (Projectile.frameCounter >= windupTicksPerFrame)
				{
					Projectile.frameCounter = 0;
					Projectile.frame++;
					if (Projectile.frame >= Main.projFrames[Type] - shootFrames)
						Projectile.frame = 0;
				}
			}
			else
			{
				Player.GetModPlayer<MacrocosmPlayer>().ScreenShakeIntensity += 0.2f;

				if (Projectile.frameCounter >= windupTicksPerFrame)
				{
					Projectile.frameCounter = 0;
					Projectile.frame++;
					if (Projectile.frame >= Main.projFrames[Type])
						Projectile.frame = windupFrames;
				}
			}
		}

		private void Shoot()
		{
			if (CanShoot)
			{
				int damage = Player.GetWeaponDamage(Player.inventory[Player.selectedItem]); //makes the damage your weapon damage + the ammunition used.
				int projToShoot = ProjectileID.Bullet;
				float knockback = Player.inventory[Player.selectedItem].knockBack;

				if (StillInUse)
					Player.PickAmmo(Player.inventory[Player.selectedItem], out projToShoot, out float speed, out damage, out knockback, out var usedAmmoItemId); //uses ammunition from inventory

				Vector2 rotPoint = MathUtils.RotatingPoint(Projectile.Center, new Vector2(40, 8 * Projectile.spriteDirection), Projectile.rotation);

				// gradually increase fire rate
				int fireFreq = (int)MathHelper.Clamp(MathHelper.Lerp(fireRateStart, fireRateCap, (AI_Windup - windupTime) / (fullFireRateTime - windupTime)), fireRateCap, fireRateStart);// Main.rand.NextBool()

				if (AI_Windup % fireFreq == 0)
					Projectile.NewProjectile(Projectile.InheritSource(Projectile), rotPoint, Vector2.Normalize(Projectile.velocity).RotatedByRandom(MathHelper.ToRadians(14)) * 10f, projToShoot, damage, knockback, Projectile.owner, default, Projectile.GetByUUID(Projectile.owner, Projectile.whoAmI));

				#region Spawn bullet shells as gore

				if (!Main.dedServ && AI_Windup % (fireFreq * 1.5) == 0)
				{
					Vector2 position = Projectile.Center - new Vector2(-20, 0) * Projectile.spriteDirection;
					Vector2 velocity = new Vector2(3f * Projectile.spriteDirection, 3f);
					Gore.NewGore(Projectile.GetSource_FromThis(), position, velocity, ModContent.GoreType<MinigunShell>());
				}

				#endregion
			}

		}

		private SlotId playingSoundId;
		private void PlaySounds()
		{
			if (AI_Windup == 0f)
			{
				playingSoundId = SoundEngine.PlaySound(CustomSounds.MinigunWindup with
				{
					Volume = 0.3f,
					SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
				});
			}
			else if (AI_Windup == windupTime)
			{
				playingSoundId = SoundEngine.PlaySound(CustomSounds.MinigunFire with
				{
					Volume = 0.3f,
					IsLooped = true,
					SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
				});
			}
		}

		public override void Kill(int timeLeft)
		{
			if (SoundEngine.TryGetActiveSound(playingSoundId, out ActiveSound playingSound))
				playingSound.Stop();
		}
	}
}
