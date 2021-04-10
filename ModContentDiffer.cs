using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ModContentDiffer
{
	/* public abstract partial class Terraria.ModLoader.Mod
	 * 
		internal readonly IDictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
		internal readonly IDictionary<string, SoundEffect> sounds = new Dictionary<string, SoundEffect>();
		internal readonly IDictionary<string, Music> musics = new Dictionary<string, Music>();
		internal readonly IDictionary<string, DynamicSpriteFont> fonts = new Dictionary<string, DynamicSpriteFont>();
		internal readonly IDictionary<string, Effect> effects = new Dictionary<string, Effect>();
		internal readonly IList<ModRecipe> recipes = new List<ModRecipe>();
		internal readonly IDictionary<string, ModItem> items = new Dictionary<string, ModItem>();

		internal readonly IDictionary<string, GlobalItem> globalItems = new Dictionary<string, GlobalItem>();
		internal readonly IDictionary<Tuple<string, EquipType>, EquipTexture> equipTextures = new Dictionary<Tuple<string, EquipType>, EquipTexture>();
		internal readonly IDictionary<string, ModPrefix> prefixes = new Dictionary<string, ModPrefix>();
		internal readonly IDictionary<string, ModDust> dusts = new Dictionary<string, ModDust>();

		internal readonly IDictionary<string, ModTile> tiles = new Dictionary<string, ModTile>();
		internal readonly IDictionary<string, GlobalTile> globalTiles = new Dictionary<string, GlobalTile>();
		internal readonly IDictionary<string, ModTileEntity> tileEntities = new Dictionary<string, ModTileEntity>();
		internal readonly IDictionary<string, ModWall> walls = new Dictionary<string, ModWall>();
		internal readonly IDictionary<string, GlobalWall> globalWalls = new Dictionary<string, GlobalWall>();
		internal readonly IDictionary<string, ModProjectile> projectiles = new Dictionary<string, ModProjectile>();
		internal readonly IDictionary<string, GlobalProjectile> globalProjectiles = new Dictionary<string, GlobalProjectile>();
		internal readonly IDictionary<string, ModNPC> npcs = new Dictionary<string, ModNPC>();
		internal readonly IDictionary<string, GlobalNPC> globalNPCs = new Dictionary<string, GlobalNPC>();
		internal readonly IDictionary<string, ModPlayer> players = new Dictionary<string, ModPlayer>();
		internal readonly IDictionary<string, ModMountData> mountDatas = new Dictionary<string, ModMountData>();
		internal readonly IDictionary<string, ModBuff> buffs = new Dictionary<string, ModBuff>();
		internal readonly IDictionary<string, GlobalBuff> globalBuffs = new Dictionary<string, GlobalBuff>();
		internal readonly IDictionary<string, ModWorld> worlds = new Dictionary<string, ModWorld>();
		internal readonly IDictionary<string, ModUgBgStyle> ugBgStyles = new Dictionary<string, ModUgBgStyle>();
		internal readonly IDictionary<string, ModSurfaceBgStyle> surfaceBgStyles = new Dictionary<string, ModSurfaceBgStyle>();
		internal readonly IDictionary<string, GlobalBgStyle> globalBgStyles = new Dictionary<string, GlobalBgStyle>();
		internal readonly IDictionary<string, ModWaterStyle> waterStyles = new Dictionary<string, ModWaterStyle>();
		internal readonly IDictionary<string, ModWaterfallStyle> waterfallStyles = new Dictionary<string, ModWaterfallStyle>();
		internal readonly IDictionary<string, GlobalRecipe> globalRecipes = new Dictionary<string, GlobalRecipe>();
		internal readonly IDictionary<string, ModTranslation> translations = new Dictionary<string, ModTranslation>();
	 */

	public class ModContentDiffer : Mod
	{
		internal const BindingFlags bf_instance = BindingFlags.Instance | BindingFlags.NonPublic;
		private const string diffText = "_diff_";
		private const string extension = ".json";
		private const string removedText = "REMOVED: ";
		private const string addedText = "  ADDED: ";
		private const string saveFolder = "ModContentDiffer";

		internal static readonly string[] contentCategories = new string[]
		{
			"items",
			"dusts",
			"prefixes",
			"tiles",
			"walls",
			"projectiles",
			"npcs",
			"mountDatas",
			"buffs",
		};

		internal static readonly string[] blacklistedNames = new string[]
		{
			"ModLoader",
			"ModContentDiffer"
		};

		/// <summary>
		/// Reflection trickery to get underlying names of each content dictionary
		/// </summary>
		public ICollection<string> GetNamesOfContent(Mod mod, string contentCategory)
		{
			try
			{
				FieldInfo field = typeof(Mod).GetField(contentCategory, bf_instance);

				object rawDict = field.GetValue(mod);

				var dictType = rawDict.GetType();

				if (dictType.IsGenericType)
				{
					var baseType = dictType.GetGenericTypeDefinition();

					Type[] types = baseType.GetInterfaces();
					Type value = typeof(IDictionary);
					if (types.Contains(value))
					{
						object keys = dictType.GetProperty("Keys").GetValue(rawDict, null);

						return (ICollection<string>)keys;
					}
				}
			}
			catch
			{
				Logger.Warn($"Failed to collect content of {mod.Name}");
			}

			return new List<string>();
		}

		public Data LoadFile(Mod mod, out Version version)
		{
			version = null;

			try
			{
				string basePath = string.Concat(new object[]
				{
					Main.SavePath,
					Path.DirectorySeparatorChar,
					saveFolder,
					Path.DirectorySeparatorChar,
					mod.Name
				});

				Directory.CreateDirectory(basePath);

				string[] fileNames = Directory.GetFiles(basePath);

				//Take non-diff files, strip extension, convert to version, order by newest first
				List<Version> versions = fileNames.Where(s => !s.Contains(diffText)).Select(s => new Version(Path.GetFileNameWithoutExtension(s))).OrderBy(v => v).ToList();
				versions.Reverse();

				//Loads largest non-current version that exists
				Version current = mod.Version;

				Version previous = versions.FirstOrDefault(v => v < current);

				if (previous != null)
				{
					version = previous;
					string path = string.Concat(new object[]
					{
						basePath,
						Path.DirectorySeparatorChar,
						previous,
						extension
					});

					if (File.Exists(path))
					{
						using (StreamReader r = new StreamReader(path))
						{
							string json = r.ReadToEnd();
							Data data = JsonConvert.DeserializeObject<Data>(json);
							return data;
						}
					}
					else
					{
						Logger.Warn($"File {path} not found.");
					}
				}
				else
				{
					Logger.Warn($"No version of {mod.Name} before {current} found.");
				}
			}
			catch
			{
				Logger.Error("Error loading a file");
			}

			return null;
		}

		public void SaveFile(Mod mod, Data data, Version previous = null)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				return;
			}

			try
			{
				string modFolderName = mod.Name;

				string basePath = string.Concat(new object[]
				{
					Main.SavePath,
					Path.DirectorySeparatorChar,
					saveFolder,
					Path.DirectorySeparatorChar,
					modFolderName,
				});

				Directory.CreateDirectory(basePath);

				string fileName = $"{mod.Version}";

				if (previous != null)
				{
					fileName = previous + diffText + fileName;
				}

				string path = string.Concat(new object[]
				{
					basePath,
					Path.DirectorySeparatorChar,
					fileName,
					extension
				});

				string json = JsonConvert.SerializeObject(data, Formatting.Indented);
				File.WriteAllText(path, json);

				Logger.Info($"Saved {modFolderName}{Path.DirectorySeparatorChar}{fileName}{extension}");
			}
			catch
			{
				Logger.Error($"Error saving a file");
			}
		}

		public override void PostSetupContent()
		{
			//Take all loaded mods, respecting blacklist
			var mods = ModLoader.Mods.ToList().Where(m => !blacklistedNames.Contains(m.Name));

			foreach (Mod mod in mods)
			{
				//Collect current mods' content
				Data data = new Data();
				foreach (string contentCategory in contentCategories)
				{
					var names = GetNamesOfContent(mod, contentCategory);
					foreach (string name in names)
					{
						data.Add(contentCategory, name);
					}
				}

				SaveFile(mod, data);

				Data oldData = LoadFile(mod, out Version previous);

				try
				{
					//Compare and merge diff
					if (oldData != null && oldData.content.Count > 0)
					{
						Data removed = new Data();
						Data added = new Data();

						Dictionary<string, List<string>> currentContent = data.content;
						Dictionary<string, List<string>> previousContent = oldData.content;

						foreach (var pair in currentContent)
						{
							string contentCategory = pair.Key;

							foreach (string name in currentContent[contentCategory])
							{
								//Check if a current name does not exist in previous: added
								if (!previousContent.ContainsKey(contentCategory) || !previousContent[contentCategory].Contains(name))
								{
									added.Add(contentCategory, addedText + name);
								}
							}
						}

						foreach (var pair in previousContent)
						{
							string contentCategory = pair.Key;

							foreach (string name in previousContent[contentCategory])
							{
								//Check if a previous name does not exist in current: removed
								if (!currentContent.ContainsKey(contentCategory) || !currentContent[contentCategory].Contains(name))
								{
									removed.Add(contentCategory, removedText + name);
								}
							}
						}

						removed.MergeWith(added);
						//the former now contains both removed and added content

						SaveFile(mod, removed, previous);
					}
				}
				catch
				{
					Logger.Warn($"Failed to generate a diff file for {mod.Name} {previous} and {mod.Version}");
				}
			}
		}
	}
}
