using EternalModLoader.Mods;
using EternalModLoader.Mods.Resources;
using EternalModLoader.Mods.Sounds;
using EternalModLoader.Mods.StreamDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EternalModLoader
{
    /// <summary>
    /// Hard-coded mods for online multiplayer safety
    /// </summary>
    public static class OnlineSafety
    {
        /// <summary>
        /// Whether or not the multiplayer disabler mod is initialized and ready to be injected
        /// </summary>
        private static bool s_isInitialized = false;

        /// <summary>
        /// Parent mod for the online safety hard-coded mods
        /// </summary>
        private static Mod s_parentMod = new Mod()
        {
            LoadPriority = int.MinValue,
            RequiredVersion = EternalModLoader.Version
        };

		/// <summary>
		/// ResourceModFile objects that contain the hard-coded mod that disables multiplayer
		/// </summary>
		public static List<ResourceModFile> MultiplayerDisablerMod = new List<ResourceModFile>()
        {
            // Battlemode
            new ResourceModFile(s_parentMod, "swf/hud/menus/battle_arena/play_online_screen.swf", "gameresources_patch2", false)
            {
                FileData = new MemoryStream(OnlineSafetySWF.s_SWFData, 0, OnlineSafetySWF.s_SWFData.Length, false, true)
            }
        };

        /// <summary>
        /// Localization for the multiplayer disabler mod ("Public Match" menu label)
        /// </summary>
        public static Dictionary<string, string> MultiplayerDisablerModLocalization = new Dictionary<string, string>()
        {
            { "french",                 "^8No mods in public matches" },
			{ "italian",                "^8No mods in public matches" },
			{ "german",                 "^8No mods in public matches" },
			{ "spanish",                "^8PARTIDA ONLINE (DESHABILITADA)" },
            { "russian",                "^8No mods in public matches" },
			{ "polish",                 "^8No mods in public matches" },
			{ "japanese",               "^8No mods in public matches" },
			{ "latin_spanish",          "^8PARTIDA ONLINE (DESHABILITADA)" },
			{ "portuguese",             "^8PARTIDA ONLINE (DESACTIVADA)" },
			{ "traditional_chinese",    "^8No mods in public matches" },
			{ "simplified_chinese",     "^8No mods in public matches" },
			{ "korean",                 "^8No mods in public matches" },
			{ "english",                "^8No mods in public matches" }
        };

		/// <summary>
		/// Localization for the multiplayer disabler mod ("Private Match" descriptive text)
		/// </summary>
		public static Dictionary<string, string> MultiplayerDisablerModLocalizationDescription = new Dictionary<string, string>()
		{
			{ "french",                 "Jouez à des matchs BATTLEMODE avec les joueurs de votre groupe.\n^3Public matchmaking is unavailable when using mods that might affect gameplay.\nOnly private matches are allowed." },
			{ "italian",                "Gioca in partite di BATTLEMODE con i giocatori del tuo gruppo\n^3Public matchmaking is unavailable when using mods that might affect gameplay\nOnly private matches are allowed" },
			{ "german",                 "Spiele BATTLE-MODUS-Matches mit den Spielern in deiner Gruppe.\n^3Public matchmaking is unavailable when using mods that might affect gameplay.\nOnly private matches are allowed." },
			{ "spanish",                "Juega BATTLEMODE con jugadores de tu mismo grupo.\n^3Las partidas publicas estan desactivadas al usar mods que alteren el GAMEPLAY.\nSolo partidas privadas estan permitidas." },
			{ "russian",                "Участвуйте в матчах в режиме BATTLEMODE с игроками вашего отряда\n^3Public matchmaking is unavailable when using mods that might affect gameplay\nOnly private matches are allowed" },
			{ "polish",                 "Graj w trybie BATTLEMODE z graczami z twojej grupy.\n^3Public matchmaking is unavailable when using mods that might affect gameplay.\nOnly private matches are allowed." },
			{ "japanese",               "パーティー内のプレイヤーとバトルモードのマッチをプレイする\n^3Public matchmaking is unavailable when using mods that might affect gameplay\nOnly private matches are allowed" },
			{ "latin_spanish",          "Juega BATTLEMODE con jugadores de tu mismo grupo.\n^3Las partidas publicas estan desactivadas al usar mods que alteren el GAMEPLAY.\nSolo partidas privadas estan permitidas." },
			{ "portuguese",             "Jogue partidas do BATTLEMODE com os jogadores do seu grupo.\n^3Partidas públicas estão desativadas quando se usam mods que podem afetar GAMEPLAY. Apenas partidas privadas são autorizadas." },
			{ "traditional_chinese",    "與你隊伍中的玩家玩戰鬥模式對戰\n^3Public matchmaking is unavailable when using mods that might affect gameplay\nOnly private matches are allowed" },
			{ "simplified_chinese",     "与自己队伍的玩家玩战斗模式比赛\n^3Public matchmaking is unavailable when using mods that might affect gameplay\nOnly private matches are allowed" },
			{ "korean",                 "파티에 속한 플레이어들과 전투 모드 플레이\n^3Public matchmaking is unavailable when using mods that might affect gameplay\nOnly private matches are allowed" },
			{ "english",                "Play BATTLEMODE matches with the players in your party\n^3Public matchmaking is unavailable when using mods that might affect gameplay.\nOnly private matches are allowed." }
		};

		/// <summary>
		/// Online-safe mod name keywords
		/// </summary>
		public static string[] OnlineSafeModNameKeywords = new string[]
        {
            "/eternalmod/",
            ".tga",
            ".png",
            ".swf",
            ".bimage",
            "/advancedscreenviewshake/",
            "/audiolog/",
            "/audiologstory/",
            "/automap/",
            "/automapplayerprofile/",
            "/automapproperties/",
            "/automapsoundprofile/",
            "/env/",
            "/font/",
            "/fontfx/",
            "/fx/",
            "/gameitem/",
            "/globalfonttable/",
            "/gorebehavior/",
            "/gorecontainer/",
            "/gorewounds/",
            "/handsbobcycle/",
            "/highlightlos/",
            "/highlights/",
            "/hitconfirmationsoundsinfo/",
            "/hud/",
            "/hudelement/",
            "/lightrig/",
            "/lodgroup/",
            "/material2/",
            "/md6def/",
            "/modelasset/",
            "/particle/",
            "/particlestage/",
            "/renderlayerdefinition/",
            "/renderparm/",
            "/renderparmmeta/",
            "/renderprogflag/",
            "/ribbon2/",
            "/rumble/",
            "/soundevent/",
            "/soundpack/",
            "/soundrtpc/",
            "/soundstate/",
            "/soundswitch/",
            "/speaker/",
            "/staticimage/",
            "/swfresources/",
            "/uianchor/",
            "/uicolor/",
            "/weaponreticle/",
            "/weaponreticleswfinfo/",
            "/entitydef/light/",
            "/entitydef/fx",
            "/impacteffect/",
            "/uiweapon/",
            "/globalinitialwarehouse/",
            "/globalshell/",
            "/warehouseitem/",
            "/warehouseofflinecontainer/",
            "/tooltip/",
            "/livetile/",
            "/tutorialevent/",
            "/maps/game/dlc/",
            "/maps/game/dlc2/",
            "/maps/game/hub/",
            "/maps/game/shell/",
            "/maps/game/sp/",
            "/maps/game/tutorials/",
            "/decls/campaign/"
        };

        /// <summary>
        /// Online-unsafe resource name keywords
        /// </summary>
        public static string[] UnsafeResourceNameKeywords = new string[]
        {
            "gameresources",
            "pvp",
            "shell",
            "warehouse"
        };

        /// <summary>
        /// Initializes the multiplayer disabler mod
        /// </summary>
        public static void InitMultiplayerDisablerMod()
        {
            if (s_isInitialized)
            {
                return;
            }

            // Build the resource mod files for the localization - "Public Match" menu label
            foreach (var localization in MultiplayerDisablerModLocalization)
            {
                var jsonBytes = Encoding.UTF8.GetBytes($"{{\"strings\":[{{\"name\":\"#eternalmod_no_online_mods\",\"text\":\"{localization.Value}\"}}]}}");

                MultiplayerDisablerMod.Add(new ResourceModFile(s_parentMod, $"EternalMod/strings/{localization.Key}.json", "gameresources_patch1", false)
                {
                    IsBlangJson = true,
                    FileData = new MemoryStream(jsonBytes, 0, jsonBytes.Length, false, true)
                });
            }

			// Build the resource mod files for the localization - "Private Match" menu description
			foreach (var localization in MultiplayerDisablerModLocalizationDescription)
			{
				var jsonBytes = Encoding.UTF8.GetBytes($"{{\"strings\":[{{\"name\":\"#str_decl_pvp_private_lobby_desc_GHOST71267\",\"text\":\"{localization.Value}\"}}]}}");

				MultiplayerDisablerMod.Add(new ResourceModFile(s_parentMod, $"EternalMod/strings/{localization.Key}.json", "gameresources_patch1", false)
				{
					IsBlangJson = true,
					FileData = new MemoryStream(jsonBytes, 0, jsonBytes.Length, false, true)
				});
			}
			
			s_isInitialized = true;
        }

        /// <summary>
        /// Determines whether or not the mod with the given name and resource name is safe for online play
        /// </summary>
        /// <param name="mod">mod</param>
        /// <returns>whether or not the mod with the given name and resource name is safe for online play</returns>
        public static bool IsModSafeForOnline(Mod mod)
        {
            bool isSafe = true;
            bool isModifyingUnsafeResource = false;
            List<ResourceModFile> assetsInfoJsons = new List<ResourceModFile>();

            foreach (var modFile in mod.Files)
            {
                if (modFile is SoundModFile || modFile is StreamDBModFile)
                {
                    continue;
                }

                var resourceModFile = modFile as ResourceModFile;

                // Check assets info files last
                if (resourceModFile.IsAssetsInfoJson)
                {
                    assetsInfoJsons.Add(resourceModFile);
                    continue;
                }

                if (UnsafeResourceNameKeywords.Any(keyword => resourceModFile.ResourceName.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    isModifyingUnsafeResource = true;
                }

                // Files with .lwo extension are unsafe - also catches $ variants such as .lwo$uvlayout_lightmap=1
                if (Path.GetExtension(resourceModFile.Name).Contains(".lwo"))
                {
                    isSafe = false;
                }

                // Allow modification of anything outside of "generated/decls/"
                if (!string.IsNullOrEmpty(resourceModFile.Name)
                    && !resourceModFile.Name.StartsWith("generated/decls/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (isSafe)
                {
                    isSafe = OnlineSafeModNameKeywords.Any(keyword => resourceModFile.Name.ToLower().Contains(keyword));
                }
            }

            if (isSafe)
            {
                return true;
            }

            if (!isSafe && isModifyingUnsafeResource)
            {
                return false;
            }

            // Don't allow adding unsafe mods in safe resource files into unsafe resources files
            // Otherwise, don't mark the mod as unsafe, it should be fine for single-player if
            // the mod is not modifying a critical resource
            foreach (var assetsInfo in assetsInfoJsons)
            {
                if (assetsInfo.AssetsInfo != null)
                {
                    if (assetsInfo.AssetsInfo.Resources != null
                        && !string.IsNullOrEmpty(assetsInfo.ResourceName)
                        && UnsafeResourceNameKeywords.Any(keyword => assetsInfo.ResourceName.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
