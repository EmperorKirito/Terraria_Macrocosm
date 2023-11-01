﻿using Macrocosm.Common.Config;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;

namespace Macrocosm.Content.UI
{
    public readonly partial struct UITheme : ILoadable
	{
		private static Dictionary<string, UITheme> themeStorage;

		public static void Reload() => LoadThemes();
		private static void LoadThemes()
		{
			themeStorage = new Dictionary<string, UITheme>();

			// UITheme defaults to Macrocosm's style
			Add(new UITheme("Macrocosm"));

			Add(new UITheme("Terraria")
			{
				PanelStyle = new(new(73, 94, 171), Color.Black),
				TabStyle = new(Color.Transparent, Color.Transparent),
				WindowStyle = new(new Color(33, 43, 79) * 0.8f, Color.Black),
				InfoElementStyle = new(new(57, 74, 136), Color.Black),
				SeparatorColor = new Color(89, 116, 213),
				ButtonStyle = new(new(57, 74, 136), Color.Black),
				ButtonHighlightStyle = new(new(69, 90, 166), Color.Gold),
				InventorySlotStyle = new(Terraria.Main.inventoryBack, default)
			});
		}

		public void Load(Mod mod)
		{
			LoadThemes();
		}

		public void Unload()
		{
			themeStorage = null;
		}

		public static void Add(UITheme theme)
			=> themeStorage.Add(theme.Name, theme);

		public static UITheme Current => Get(MacrocosmConfig.Instance.SelectedUITheme);

		public static UITheme Get(string name) 
		{
			if (themeStorage is null)
				LoadThemes();

			return themeStorage.TryGetValue(name, out var theme) ? theme : default;
		}

		/*
		public static string[] AvailableThemes
		{
			get
			{
				if (themeStorage is null)
					LoadThemes();

				return themeStorage.Keys.ToArray();
			}
		}
		*/
	}
}

