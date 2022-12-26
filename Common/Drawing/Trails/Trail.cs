﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Steamworks;
using System;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;

namespace Macrocosm.Common.Drawing.Trails
{
	public abstract class Trail
	{
		public abstract MiscShaderData TrailShader { get; }
		public virtual Projectile Owner { get; set; }

		public virtual float Opacity { get; set; } = 1f;
		public virtual float Saturation { get; set; } = -1f;
	
		public virtual Color? TrailColor { get; set; }
		public virtual float? TrailWidth { get; set; }

		public virtual Color TrailColors(float progressOnStrip) => TrailColor ?? Color.White;
		public virtual float TrailWidths(float progressOnStrip) => TrailWidth ?? 1f;

		//public void SetTexture1(string path) => TrailShader.UseImage0(path);
		//public void SetTexture2(string path) => TrailShader.UseImage1(path);
		//public void SetTexture3(string path) => TrailShader.UseImage2(path);

		public virtual void Update() { }
		private void InternalUpdate()
		{
			Update();

			TrailShader.UseOpacity(Opacity);
			TrailShader.UseSaturation(Saturation);
		}

		public virtual void Draw()
		{ 
			Draw(Owner.oldPos, Owner.oldRot, Owner.Size / 2);
		}

		public virtual void Draw(Vector2[] positions, float[] rotations, Vector2 offset = default)
		{
			VertexStrip vertexStrip = new();

			InternalUpdate();
			TrailShader.Apply();

			vertexStrip.PrepareStripWithProceduralPadding(positions, rotations, TrailColors, TrailWidths, offset - Main.screenPosition, true);
			vertexStrip.DrawTrail();

			Main.pixelShader.CurrentTechnique.Passes[0].Apply();
		}
	}
}
