using Macrocosm.Common.Utils;
using Macrocosm.Content.Items.Dev;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SubworldLibrary;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Macrocosm.Common.Netcode;
using System.Collections.Generic;
using Macrocosm.Content.Rockets.Modules;
using Terraria.Localization;
using Macrocosm.Common.Subworlds;
using System.Linq;
using Terraria.UI.Chat;
using Terraria.GameContent;
using Macrocosm.Content.Rockets.Construction;

namespace Macrocosm.Content.Rockets
{
    public partial class Rocket 
	{
		/// <summary> The rocket's identifier </summary>
		public int WhoAmI = -1;

		/// <summary> Whether the rocket is currently active </summary>
		[NetSync] public bool Active;

		/// <summary> The rocket's top-left coodrinates in the world </summary>
		[NetSync] public Vector2 Position;

		/// <summary> The rocket's velocity </summary>
		[NetSync] public Vector2 Velocity;

		/// <summary> The rocket's sequence timer </summary>
		[NetSync] public int FlightTime;

		/// <summary> Whether the rocket has been launched </summary>
		[NetSync] public bool InFlight;

		/// <summary> Whether the rocket is landing </summary>
		[NetSync] public bool Landing;

		/// <summary> The initial vertical position </summary>
		[NetSync] public float StartPositionY;

		/// <summary> The target landing position </summary>
		[NetSync] public Vector2 TargetLandingPosition;

		/// <summary> The world Y coordinate for entering the target subworld </summary>
		public const float WorldExitPositionY = 20 * 16f;

		/// <summary> The amount of fuel currently stored in the rocket, as an absolute value </summary>
		[NetSync] public float Fuel;

		/// <summary> The rocket's fuel capacity as an absoulute value </summary>
		[NetSync] public float FuelCapacity = 1000f;

		/// <summary> The rocket's current world, "Earth" if active and not in a subworld. Other mod's subworlds have the mod name prepended </summary>
		[NetSync] public string CurrentSubworld;

		/// <summary> Whether the rocket is active in the current subworld and should be updated and visible </summary>
		public bool ActiveInCurrentSubworld => Active && CurrentSubworld == MacrocosmSubworld.CurrentSubworld;

		/// <summary> The rocket's bounds width </summary>
		public static int Width = 276;

		/// <summary> The rocket's bounds height </summary>
		public static int Height = 594;

		/// <summary> The size of the rocket's bounds </summary>
		public static Vector2 Size => new(Width, Height);

		/// <summary> The rectangle occupied by the Rocket in the world </summary>
		public Rectangle Bounds => new((int)Position.X, (int)Position.Y, Width, Height);

		/// <summary> The rocket's center in world coordinates </summary>
		public Vector2 Center
		{
			get => Position + Size/2f;
			set => Position = value - Size/2f;
		}

		public bool Stationary => Velocity.LengthSquared() < 0.1f;

		/// <summary> The layer this rocket is drawn in </summary>
		public RocketDrawLayer DrawLayer = RocketDrawLayer.BeforeNPCs;

		/// <summary> The Rocket's name, set by the user, defaults to a localized "Rocket" name </summary>
		public string DisplayName
			=> EngineModule.Nameplate.HasNoSupportedChars() ? Language.GetTextValue("Mods.Macrocosm.Common.Rocket") : EngineModule.Nameplate.Text;


		/// <summary> List of the module names, in the customization access order </summary>
		public List<string> ModuleNames => Modules.Keys.ToList();

		/// <summary> List of all the rocket's modules, in their order found in ModuleNames </summary>
		public Dictionary<string, RocketModule> Modules = new()
		{
			{ "CommandPod", new CommandPod() },
			{ "ServiceModule", new ServiceModule() },
			{ "ReactorModule", new ReactorModule() },
			{ "EngineModule", new EngineModule() },
			{ "BoosterLeft", new BoosterLeft() },
			{ "BoosterRight", new BoosterRight() }
		};

		/// <summary> The Rocket's command pod </summary>
		public CommandPod CommandPod => (CommandPod)Modules["CommandPod"];

		/// <summary> The Rocket's service module </summary>
		public ServiceModule ServiceModule => (ServiceModule)Modules["ServiceModule"];

		/// <summary> The rocket's reactor module </summary>
		public ReactorModule ReactorModule => (ReactorModule)Modules["ReactorModule"];

		/// <summary> The Rocket's engine module </summary>
		public EngineModule EngineModule => (EngineModule)Modules["EngineModule"];

		/// <summary> The rocket's left booster </summary>
		public BoosterLeft BoosterLeft => (BoosterLeft)Modules["BoosterLeft"];

		/// <summary> The rocket's right booster </summary>
		public BoosterRight BoosterRight => (BoosterRight)Modules["BoosterRight"];

		// Number of ticks of the launch countdown (seconds * 60 ticks/sec)
		private int liftoffTime = 180;
		private float maxFlightSpeed = 25f;

		public float FlightProgress => 1f - ((Center.Y - WorldExitPositionY) / (StartPositionY - WorldExitPositionY));

		// TODO: assign a value to TargetLandingPosition
		public float LandingProgress => Center.Y / TargetLandingPosition.Y;


		/// <summary> Instatiates a rocket. Use <see cref="Create(Vector2)"/> for spawning in world and proper syncing. </summary>
		public Rocket()
		{
		}

		public void OnCreation()
		{
			CurrentSubworld = MacrocosmSubworld.CurrentSubworld;
		}

		public void OnWorldSpawn()
		{
			if (Landing)
			{
				// This is to ensure the location is properly assigned if subworld was just generated
				if (TargetLandingPosition == default)
					TargetLandingPosition = LaunchPadLocations.GetDefaultLocation(CurrentSubworld);

				Center = new(TargetLandingPosition.X, Center.Y);
			}	
		}

		/// <summary> Update the rocket </summary>
		public void Update()
		{
			Velocity = GetCollisionVelocity();
			Position += Velocity;

			// Testing
			Fuel = 1000f;

			SetModuleRelativePositions();
			Movement();

			if (Stationary)
			{
				Interact();
				LookForCommander();
			}

            if (InFlight && Position.Y < WorldExitPositionY)
			{
				InFlight = false;
				Landing = true;
				EnterDestinationSubworld();
			}
		}

		/// <summary> Safely despawn the rocket </summary>
		public void Despawn()
		{
			if(Main.netMode == NetmodeID.SinglePlayer)
			{
				if (CheckPlayerInRocket(Main.myPlayer))
				{
					GetRocketPlayer(Main.myPlayer).InRocket = false;
					GetRocketPlayer(Main.myPlayer).AsCommander = false;
				}
			}
 			else
			{
				for (int i = 0; i < Main.maxPlayers; i++)
				{
					if (CheckPlayerInRocket(i))
					{
						GetRocketPlayer(i).InRocket = false;
						GetRocketPlayer(i).AsCommander = false;
					}
				}
			}

			Active = false;
			CurrentSubworld = "";
			NetSync();
		}

		/// <summary> Draw the rocket </summary>
		public void Draw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			// Positions (also) set here, in the update method only they lag behind 
			SetModuleRelativePositions();

			foreach (RocketModule module in Modules.Values.OrderBy(module => module.DrawPriority))
			{
				module.Draw(spriteBatch, screenPos, drawColor);
			}

			//DrawDebugBounds();
			//DrawDebugModuleHitbox();
			//DisplayWhoAmI();
		}

		/// <summary> Draw the rocket as a dummy </summary>
		public void DrawDummy(SpriteBatch spriteBatch, Vector2 offset, Color drawColor)
		{
			// Passing Rocket world position as "screenPosition" cancels it out  
			Draw(spriteBatch, Position - offset, drawColor);
		}

		// Set the rocket's modules positions in the world
		private void SetModuleRelativePositions()
		{
			CommandPod.Position = Position + new Vector2(Width/2f - CommandPod.Hitbox.Width/2f, 0);
			ServiceModule.Position = CommandPod.Position + new Vector2(-6, CommandPod.Hitbox.Height - 2.1f);
			ReactorModule.Position = ServiceModule.Position + new Vector2(-2, ServiceModule.Hitbox.Height - 2);
			EngineModule.Position = ReactorModule.Position + new Vector2(-18, ReactorModule.Hitbox.Height);
			BoosterLeft.Position = new Vector2(EngineModule.Center.X, EngineModule.Position.Y) - new Vector2(BoosterLeft.Texture.Width/2, 0) + new Vector2(-37, 16);
			BoosterRight.Position = new Vector2(EngineModule.Center.X, EngineModule.Position.Y)  -new Vector2(BoosterLeft.Texture.Width / 2, 0) + new Vector2(37, 16);
		}

		/// <summary> Gets the RocketPlayer bound to the provided player ID </summary>
		/// <param name="playerID"> The player ID </param>
		public RocketPlayer GetRocketPlayer(int playerID) => Main.player[playerID].RocketPlayer();

		/// <summary> Checks whether the provided player ID is on this rocket </summary>
		/// <param name="playerID"> The player ID </param>
		public bool CheckPlayerInRocket(int playerID) => Main.player[playerID].active && GetRocketPlayer(playerID).InRocket && GetRocketPlayer(playerID).RocketID == WhoAmI;

		/// <summary> Checks whether the provided player ID is a commander on this rocket </summary>
		/// <param name="playerID"> The player ID </param>
		public bool CheckPlayerCommander(int playerID) => Main.player[playerID].active && GetRocketPlayer(playerID).AsCommander && GetRocketPlayer(playerID).RocketID == WhoAmI;

		/// <summary> Checks whether this rocket has a commander </summary>
		/// <param name="playerID"> The commander player ID </param>
		public bool TryFindingCommander(out int playerID)
		{
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				if (!Main.player[i].active)
					continue;

				RocketPlayer rocketPlayer = GetRocketPlayer(i);
				if (CheckPlayerInRocket(i) && rocketPlayer.AsCommander)
				{
					playerID = i;
					return true;
				}
			}

			playerID = -1;
			return false;
		}

		/// <summary> Check whether there are currently any players inside this rocket </summary>
		/// <param name="first"> The ID of the first player found in rocket, will be -1 if none found </param>
		public bool AnyEmbarkedPlayers(out int first)
		{
			if (Main.netMode == NetmodeID.SinglePlayer)
			{
				first = Main.myPlayer;
				return CheckPlayerInRocket(first);
			}

			for (int i = 0; i < Main.maxPlayers; i++)
			{
				if (!Main.player[i].active)
					continue;

				if (CheckPlayerInRocket(i))
				{
					first = i;
					return true;
				}
			}
			first = -1;
			return false;
		}

		/// <summary> Whether the local player can interact with the rocket </summary>
		public bool InInteractionRange()
		{
			Point location = Bounds.ClosestPointInRect(Main.LocalPlayer.Center).ToTileCoordinates();
			return Main.LocalPlayer.IsInTileInteractionRange(location.X, location.Y, TileReachCheckSettings.Simple);
		}

		/// <summary> Launches the rocket, with syncing </summary>
		public void Launch()
		{
			InFlight = true;
			StartPositionY = Position.Y;
			NetSync();
		}

		/// <summary> Checks whether the flight path is obstructed by solid blocks </summary>
		// TODO: CHECK THIS AT A LOWER FREQUENCY - maybe once every second, and return a cached result otherwise
		// Will implement it in the Checklist provider rework
		public bool CheckFlightPathObstruction()
		{
			int startY = (int)((Center.Y - Height / 2) / 16);

			for (int offsetX = 0; offsetX < (Width / 16); offsetX++)
			{
				if (Utility.GetFirstTileCeiling((int)(Position.X / 16f) + offsetX, startY, solid: true) > 10)
					return false;
			}

			return true;
		}

		public bool CheckTileCollision()
		{
			foreach (RocketModule module in Modules.Values)
				if (Math.Abs(Collision.TileCollision(module.Position, Velocity, module.Hitbox.Width, module.Hitbox.Height).Y) > 0.1f)
					return true;

			return false;
		}

		// Interaction logic
		private void Interact()
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			if (MouseCanInteract() && InInteractionRange() && !InFlight && !GetRocketPlayer(Main.myPlayer).InRocket)
			{
				if (Main.mouseRight)
				{
					bool noCommanderInRocket = (Main.netMode == NetmodeID.SinglePlayer) || !TryFindingCommander(out _);
					GetRocketPlayer(Main.myPlayer).EmbarkPlayerInRocket(WhoAmI, noCommanderInRocket);
				}
				else
				{
					if(!RocketUISystem.Active)
					{
						Main.LocalPlayer.cursorItemIconEnabled = true;
						Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<RocketPlacer>();
					}
				}
			}
		}

		private Vector2 GetCollisionVelocity()
		{
			Vector2 minCollisionVelocity = new(float.MaxValue, float.MaxValue);

			foreach (RocketModule module in Modules.Values)
			{
				Vector2 collisionVelocity = Collision.TileCollision(module.Position, Velocity, module.Hitbox.Width, module.Hitbox.Height);
				if (collisionVelocity.LengthSquared() < minCollisionVelocity.LengthSquared())
					minCollisionVelocity = collisionVelocity;
 			}
			return minCollisionVelocity;
		}


		private bool MouseCanInteract()
		{
			foreach (RocketModule module in Modules.Values.Where((module) => !(module is BoosterRight) && !(module is BoosterLeft)))
				if (module.Hitbox.Contains(Main.MouseWorld.ToPoint()))
					return true;

			return false;
 		}

		// Updates the commander player in real time, in multiplayer scenarios
		private void LookForCommander()
		{
			if (Main.netMode == NetmodeID.SinglePlayer)
				return;

			if (AnyEmbarkedPlayers(out int id) && !TryFindingCommander(out _))
				GetRocketPlayer(id).AsCommander = true;
		}

		// Handles the rocket's movement during flight
		private void Movement()
		{
			if (InFlight)
 				FlightTime++;
 			else
 				Velocity.Y += 0.1f * MacrocosmSubworld.CurrentGravityMultiplier;
 
			if (Landing)
			{
				// TODO: Add smooth deceleration

				if (Stationary && LandingProgress > 0.9f)
					Landing = false;
			}


			if (FlightTime >= liftoffTime)
			{
				float flightAcceleration = 0.1f;   // mid-flight
				float liftoffAcceleration = 0.05f; // during liftoff
				float startAcceleration = 0.01f;   // initial 

				if (Velocity.Y < maxFlightSpeed)
					if (FlightTime >= liftoffTime + 60)
						Velocity.Y -= flightAcceleration;
					else if (FlightTime >= liftoffTime + 40)
						Velocity.Y -= liftoffAcceleration;
					else
						Velocity.Y -= startAcceleration;

				SetScreenshake();
				//VisualEffects();
			}	
		}

		// Sets the screenshake during flight 
		private void SetScreenshake()
		{
			float intenstity;

			if (FlightTime >= liftoffTime && FlightProgress < 0.05f)
				intenstity = 30f;
			else
				intenstity = 15f * (1f - Utility.QuadraticEaseOut(FlightProgress));

			Main.LocalPlayer.AddScreenshake(intenstity, "RocketFlight");
		}

		// Handle visuals (dusts, particles)
		private void VisualEffects()
		{
			int dustCnt = FlightTime > liftoffTime + 40 ? 10 : 4;
 			for (int i = 0; i < dustCnt; i++)
			{
				Dust dust = Dust.NewDustDirect(Position + new Vector2(-10, Height - 15 - 50 * FlightProgress), (int)((float)Width + 20), 1, DustID.Flare, Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(20f, 200f) * FlightProgress, Scale: Main.rand.NextFloat(1.5f, 3f));
				dust.noGravity = false;
			}
 			
			for (int g = 0; g < 3; g++)
			{
				if (Main.rand.NextBool(2))
				{
					int type = Main.rand.NextFromList<int>(GoreID.Smoke1, GoreID.Smoke2, GoreID.Smoke3);
					Gore.NewGore(null, Position + new Vector2(Main.rand.Next(-25, Width), Height - 15 - 30 * FlightProgress), new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(0, 8f)), type);
				}
			}
		}

		// Handles the subworld transfer on each client, locally
		private void EnterDestinationSubworld()
		{
			if (Main.netMode == NetmodeID.Server)
				return;

			RocketPlayer commander;
			if (Main.netMode == NetmodeID.SinglePlayer)
 				commander = GetRocketPlayer(Main.myPlayer);
 			else  
			{
				if (TryFindingCommander(out int id))
					commander = GetRocketPlayer(id);
				else if (AnyEmbarkedPlayers(out id))
					commander = GetRocketPlayer(id);
				else
					return;
 			}

			if (CheckPlayerInRocket(Main.myPlayer))
			{
				CurrentSubworld = commander.TargetSubworldID;
				//NetSync();

				// if(commander.TargetLandingPosition != Vector2.Zero) // (or nullable Vector2?)
				//   TargetLandingPosition = commander.TargetLandingPosition;
				// else 
				TargetLandingPosition = LaunchPadLocations.GetDefaultLocation(commander.TargetSubworldID);
 
				if (commander.TargetSubworldID == "Earth")
					SubworldSystem.Exit();
				else if (commander.TargetSubworldID != null && commander.TargetSubworldID != "")
				{
					if (!SubworldSystem.Enter(Macrocosm.Instance.Name + "/" + commander.TargetSubworldID))
					{
						// Stay here if entering the subworld fails, for whatever reason
						CurrentSubworld = MacrocosmSubworld.CurrentSubworld;
						string message = "Error: Failed entering target subworld: " + commander.TargetSubworldID + ", staying on " + MacrocosmSubworld.CurrentSubworld;

						Utility.Chat(message, Color.Red);
						Macrocosm.Instance.Logger.Error(message);
					}
				}
			}
		}
	}
}