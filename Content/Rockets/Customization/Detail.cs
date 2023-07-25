﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria.ModLoader;
using ReLogic.Content;
using System;
using Macrocosm.Common.Utils;

namespace Macrocosm.Content.Rockets.Customization
{
    public class Detail
    {
        public string Name { get; set; }
        public string ModuleName { get; set; }

		public bool Unlocked { get; set; }	
		public bool UnlockedByDefault { get; private set; }	

        public string TexturePath => GetType().Namespace.Replace('.', '/') + "/Details/" + ModuleName + "/" + Name;
		public Texture2D Texture
		{
			get
			{
				if (ModContent.RequestIfExists(TexturePath, out Asset<Texture2D> paintMask))
					return paintMask.Value;
				else
					return Macrocosm.EmptyTex;
			}
		}

		//public Texture2D IconTexture { get; set; }
		//public int ItemType{ get; set; }

		public Detail(string moduleName, string patternName, bool unlockedByDefault)
        {
			ModuleName = moduleName;
            Name = patternName;

			UnlockedByDefault = unlockedByDefault;
			Unlocked = unlockedByDefault;
		}
	}
}