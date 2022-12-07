﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;
using Terraria.GameContent.UI.Elements;

namespace Macrocosm.Content.Rocket.UI
{
	public class UIWorldInfoElement : UIPanel
	{
		public Texture2D Icon;
		public string DisplayText = "";
		public string DisplayTextUnits = "";
		public string HoverText = "";

		public Color DisplayValueColor = Color.White;

		private UIText UIDisplayText;

		public UIWorldInfoElement(Texture2D icon, string text, string hoverText = "", string units = "",  Color valueColor = default)
		{
			Icon = icon;
			DisplayText = text;
			DisplayTextUnits = units;
			HoverText = hoverText;

			if(valueColor != default)
				DisplayValueColor = valueColor;

			Initialize();
		}
		
		public override void OnInitialize()
		{
			Width.Set(0f, 0.98f);
			Height.Set(38f, 0f);

			BackgroundColor = new Color(43, 56, 101);
			BorderColor = BackgroundColor * 2f;

			UIDisplayText = new(DisplayText, 0.9f, false);
			UIDisplayText.VAlign = 0.5f;
			UIDisplayText.Left.Set(40, 0f);

			Append(UIDisplayText);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			UIDisplayText.TextColor = DisplayValueColor; 

			if (IsMouseHovering)
				Main.instance.MouseText(HoverText);

			if (UIDisplayText is not null)
				UIDisplayText.SetText(DisplayText + " " + DisplayTextUnits);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			base.DrawSelf(spriteBatch);

			CalculatedStyle dimensions = GetDimensions();
			spriteBatch.Draw(Icon, dimensions.Position() + new Vector2(dimensions.Width * 0.062f, dimensions.Height * 0.185f), Color.White);
		}
	}
}
