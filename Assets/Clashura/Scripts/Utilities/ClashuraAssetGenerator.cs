#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Clashura.Core;
using Clashura.Characters.Data;
using Clashura.Characters.Managers;

namespace Clashura.Utilities
{
    /// <summary>
    /// Editor utility for generating 2D character artwork, 2D sprites, 2D prefabs,
    /// ability data, and central ScriptableObject databases for all 18 characters.
    /// Strictly 2D game architecture.
    /// </summary>
    public static class ClashuraAssetGenerator
    {
        private struct CharDef
        {
            public string id;
            public string name;
            public string title;
            public CharacterRole role;
            public Color color;
            public int hp;
            public int atk;
            public int def;
            public int spd;
            public string bio;
            public (string id, string name, string desc, AbilityType type, float cd)[] abilities;
        }

        private static readonly CharDef[] Characters = new CharDef[]
        {
            new CharDef {
                id = "clashura_char_01", name = "Aethelgard", title = "The Shield of Dawn",
                role = CharacterRole.Vanguard, color = new Color(0.95f, 0.75f, 0.2f),
                hp = 1600, atk = 95, def = 190, spd = 35,
                bio = "A venerable knight encased in radiant sun-forged armor, unwavering in defence.",
                abilities = new [] {
                    ("aeth_ab_01", "Dawn's Bastion", "Erects a golden aegis absorbing massive damage.", AbilityType.Skill, 8f),
                    ("aeth_ab_02", "Aegis Strike", "Shield bash that stuns all foes directly in front.", AbilityType.Primary, 3f),
                    ("aeth_ab_03", "Solar Radiance", "Passively restores armor while maintaining position.", AbilityType.Passive, 0f),
                    ("aeth_ab_04", "Wrath of the Sun", "Unleashes an incandescent shockwave across the field.", AbilityType.Ultimate, 25f)
                }
            },
            new CharDef {
                id = "clashura_char_02", name = "Valkira", title = "The Crimson Blade",
                role = CharacterRole.Striker, color = new Color(0.85f, 0.15f, 0.2f),
                hp = 1100, atk = 210, def = 90, spd = 70,
                bio = "A fierce warrior whose dual blades carve paths of destruction in combat.",
                abilities = new [] {
                    ("valk_ab_01", "Blood Cleave", "Wide horizontal slash inflicting deep bleed wounds.", AbilityType.Primary, 2.5f),
                    ("valk_ab_02", "Crimson Dash", "Flits past opponent defenses with piercing speed.", AbilityType.Skill, 6f),
                    ("valk_ab_03", "Berserk Fury", "Grants escalating attack power as health diminishes.", AbilityType.Passive, 0f),
                    ("valk_ab_04", "Bloodstorm Execution", "A devastating aerial whirlwind shredding all nearby targets.", AbilityType.Ultimate, 22f)
                }
            },
            new CharDef {
                id = "clashura_char_03", name = "Zephyrion", title = "Wind Walker",
                role = CharacterRole.Duelist, color = new Color(0.2f, 0.8f, 0.95f),
                hp = 950, atk = 180, def = 75, spd = 95,
                bio = "A master fencer navigating tempests with unrivaled finesse and agility.",
                abilities = new [] {
                    ("zeph_ab_01", "Gale Thrust", "High-precision rapier strike breaking enemy stance.", AbilityType.Primary, 2f),
                    ("zeph_ab_02", "Windstep", "Instant wind dash positioning instantly behind the enemy.", AbilityType.Skill, 5f),
                    ("zeph_ab_03", "Featherfoot", "Grants increased evasion while maintaining continuous movement.", AbilityType.Passive, 0f),
                    ("zeph_ab_04", "Cyclone Blitz", "Traps the opponent inside a high-speed whirlwind flurry.", AbilityType.Ultimate, 20f)
                }
            },
            new CharDef {
                id = "clashura_char_04", name = "Nyxara", title = "Shadow Weaver",
                role = CharacterRole.Specialist, color = new Color(0.55f, 0.18f, 0.82f),
                hp = 1000, atk = 160, def = 85, spd = 60,
                bio = "An enigmatic sorceress manipulating shadows and spacetime distortion.",
                abilities = new [] {
                    ("nyx_ab_01", "Umbral Orb", "Hurls an orb of concentrated dark energy that homes in.", AbilityType.Primary, 3f),
                    ("nyx_ab_02", "Void Tether", "Roots enemies with creeping shadow tendrils.", AbilityType.Skill, 7f),
                    ("nyx_ab_03", "Eclipse Shroud", "Grants brief invisibility and evasion when entering combat.", AbilityType.Passive, 0f),
                    ("nyx_ab_04", "Black Hole Cataclysm", "Spawns a gravitational vortex collapsing enemy positions.", AbilityType.Ultimate, 28f)
                }
            },
            new CharDef {
                id = "clashura_char_05", name = "Brakmor", title = "The Iron Juggernaut",
                role = CharacterRole.Vanguard, color = new Color(0.72f, 0.38f, 0.16f),
                hp = 1800, atk = 120, def = 210, spd = 30,
                bio = "A colossal behemoth clad in thick iron plates, practically an immovable mountain.",
                abilities = new [] {
                    ("brak_ab_01", "Heavy Slam", "Smashes the ground with catastrophic force.", AbilityType.Primary, 3.5f),
                    ("brak_ab_02", "Molten Plating", "Ignites armor spikes dealing burn damage to attackers.", AbilityType.Skill, 9f),
                    ("brak_ab_03", "Unstoppable Force", "Immune to stuns, knockbacks, and movement impairment.", AbilityType.Passive, 0f),
                    ("brak_ab_04", "Seismic Catastrophe", "Shatters surrounding terrain creating impassable barriers.", AbilityType.Ultimate, 30f)
                }
            },
            new CharDef {
                id = "clashura_char_06", name = "Lyra Thorne", title = "Verdant Ranger",
                role = CharacterRole.Striker, color = new Color(0.18f, 0.72f, 0.32f),
                hp = 1050, atk = 195, def = 80, spd = 75,
                bio = "An elite marksman from the emerald forest with uncanny long-range precision.",
                abilities = new [] {
                    ("lyra_ab_01", "Piercing Arrow", "High-velocity shot that pierces through multiple enemies.", AbilityType.Primary, 2.2f),
                    ("lyra_ab_02", "Briar Trap", "Conceals a thorn snare that cripples pursuing targets.", AbilityType.Skill, 8f),
                    ("lyra_ab_03", "Hunter's Focus", "Critical strike chance scales with distance from target.", AbilityType.Passive, 0f),
                    ("lyra_ab_04", "Rain of Thorns", "Blankets a broad area with an onslaught of enchanted arrows.", AbilityType.Ultimate, 24f)
                }
            },
            new CharDef {
                id = "clashura_char_07", name = "Kaelen Voss", title = "Arcane Spellblade",
                role = CharacterRole.Duelist, color = new Color(0.2f, 0.42f, 0.88f),
                hp = 1000, atk = 175, def = 90, spd = 80,
                bio = "Infuses ancient rune magic into close-quarters dual sword combat.",
                abilities = new [] {
                    ("kael_ab_01", "Mana Blade", "Infuses weapons with pure magic delivering rapid slashes.", AbilityType.Primary, 2f),
                    ("kael_ab_02", "Phase Shift", "Briefly phases out of physical reality avoiding all harm.", AbilityType.Skill, 6f),
                    ("kael_ab_03", "Rune Barrier", "Generates an energy barrier each time an ability lands.", AbilityType.Passive, 0f),
                    ("kael_ab_04", "Resonance Overload", "Unleashes spinning arcane blade projections around himself.", AbilityType.Ultimate, 22f)
                }
            },
            new CharDef {
                id = "clashura_char_08", name = "Seraphina", title = "The Dawn Priestess",
                role = CharacterRole.Support, color = new Color(0.96f, 0.92f, 0.78f),
                hp = 1000, atk = 90, def = 85, spd = 60,
                bio = "Channeling luminous divinity, she restores vitality and shields the weary.",
                abilities = new [] {
                    ("sera_ab_01", "Sacred Beam", "Directs healing energy to allies or burns hostile forces.", AbilityType.Primary, 3f),
                    ("sera_ab_02", "Divine Aegis", "Bestows protective light halos across all friendly heroes.", AbilityType.Skill, 8f),
                    ("sera_ab_03", "Seraphic Grace", "Increases team movement speed and passive health regen.", AbilityType.Passive, 0f),
                    ("sera_ab_04", "Heavenly Revival", "Cleanses all negative debuffs and provides massive burst healing.", AbilityType.Ultimate, 32f)
                }
            },
            new CharDef {
                id = "clashura_char_09", name = "Morvath", title = "Plague Alchemist",
                role = CharacterRole.Specialist, color = new Color(0.38f, 0.82f, 0.22f),
                hp = 1150, atk = 150, def = 100, spd = 50,
                bio = "A rogue scientist deploying noxious chemicals and insidious decay.",
                abilities = new [] {
                    ("morv_ab_01", "Corrosive Flask", "Hurls corrosive chemicals corroding target defenses.", AbilityType.Primary, 3f),
                    ("morv_ab_02", "Toxic Miasma", "Releases an expanding poisonous fog cloud.", AbilityType.Skill, 7.5f),
                    ("morv_ab_03", "Viral Leech", "Converts poison damage dealt to enemies into self-healing.", AbilityType.Passive, 0f),
                    ("morv_ab_04", "Epidemic Outbreak", "Detonates all active afflictions into contagious explosions.", AbilityType.Ultimate, 26f)
                }
            },
            new CharDef {
                id = "clashura_char_10", name = "Ignis Rex", title = "Infernal Warlord",
                role = CharacterRole.Striker, color = new Color(0.96f, 0.36f, 0.12f),
                hp = 1300, atk = 190, def = 110, spd = 55,
                bio = "Wields magma-infused greatswords with ferocious barbarian discipline.",
                abilities = new [] {
                    ("ignis_ab_01", "Flame Cleave", "Cleaves with a flaming edge scorching everything ahead.", AbilityType.Primary, 2.5f),
                    ("ignis_ab_02", "Combustion Roar", "Intimidating roar that blasts opponents backward in flame.", AbilityType.Skill, 8f),
                    ("ignis_ab_03", "Fire Within", "Basic attacks apply burning damage over 3 seconds.", AbilityType.Passive, 0f),
                    ("ignis_ab_04", "Hellfire Meteor", "Calls down an apocalyptic molten meteor onto the battlefield.", AbilityType.Ultimate, 25f)
                }
            },
            new CharDef {
                id = "clashura_char_11", name = "Selene Mist", title = "Moonlight Oracle",
                role = CharacterRole.Support, color = new Color(0.62f, 0.76f, 0.94f),
                hp = 980, atk = 105, def = 80, spd = 65,
                bio = "A serene mystic attuned to celestial tides and ethereal moonlight.",
                abilities = new [] {
                    ("sele_ab_01", "Lunar Ray", "Channels a focused moonlight beam that illuminates and slows.", AbilityType.Primary, 2.5f),
                    ("sele_ab_02", "Mist Shroud", "Cloaks the allied team inside a protective veil.", AbilityType.Skill, 7f),
                    ("sele_ab_03", "Crescent Aura", "Reduces cooldowns for all nearby companions.", AbilityType.Passive, 0f),
                    ("sele_ab_04", "Eclipse Tranquility", "Puts enemies in range to sleep and grants immunity to allies.", AbilityType.Ultimate, 30f)
                }
            },
            new CharDef {
                id = "clashura_char_12", name = "Gorgar", title = "Stone Sovereign",
                role = CharacterRole.Vanguard, color = new Color(0.52f, 0.46f, 0.42f),
                hp = 1750, atk = 110, def = 200, spd = 35,
                bio = "Carved from living bedrock, impervious to blunt impacts and sharp steel.",
                abilities = new [] {
                    ("gorg_ab_01", "Boulder Throw", "Lobs a gigantic boulder knocking targets flat.", AbilityType.Primary, 3.5f),
                    ("gorg_ab_02", "Granite Skin", "Hardens skin into solid stone temporarily doubling defense.", AbilityType.Skill, 10f),
                    ("gorg_ab_03", "Bedrock Core", "Greatly reduces incoming physical damage.", AbilityType.Passive, 0f),
                    ("gorg_ab_04", "Avalanche Rampage", "Transforms into a rolling boulder crushing enemy formations.", AbilityType.Ultimate, 28f)
                }
            },
            new CharDef {
                id = "clashura_char_13", name = "Astrid Storm", title = "Thunder Striker",
                role = CharacterRole.Striker, color = new Color(0.95f, 0.9f, 0.16f),
                hp = 1100, atk = 200, def = 85, spd = 85,
                bio = "A lightning conductor warrior darting across the field like an arc of thunder.",
                abilities = new [] {
                    ("astr_ab_01", "Lightning Arc", "Discharges lightning bolts that jump between nearby foes.", AbilityType.Primary, 2f),
                    ("astr_ab_02", "Thunder Surge", "High-voltage dash that leaves an electrified trail.", AbilityType.Skill, 6f),
                    ("astr_ab_03", "Static Buildup", "Every 4th attack delivers an instant critical lightning strike.", AbilityType.Passive, 0f),
                    ("astr_ab_04", "Cataclysmic Storm", "Summons a torrential thunderstorm striking lightning everywhere.", AbilityType.Ultimate, 24f)
                }
            },
            new CharDef {
                id = "clashura_char_14", name = "Darius Hex", title = "Blood Siphon",
                role = CharacterRole.Specialist, color = new Color(0.48f, 0.12f, 0.52f),
                hp = 1200, atk = 165, def = 95, spd = 55,
                bio = "A dark noble drawing life force from enemies to sustain unholy longevity.",
                abilities = new [] {
                    ("dari_ab_01", "Life Siphon", "Channels a dark beam draining target health directly.", AbilityType.Primary, 2.5f),
                    ("dari_ab_02", "Hex of Fragility", "Places a blood curse weakening target defense significantly.", AbilityType.Skill, 8f),
                    ("dari_ab_03", "Sanguine Hunger", "Gains 25% movement speed when pursuing wounded foes.", AbilityType.Passive, 0f),
                    ("dari_ab_04", "Crimson Harvest", "Drains life simultaneously from all enemies in proximity.", AbilityType.Ultimate, 26f)
                }
            },
            new CharDef {
                id = "clashura_char_15", name = "Ronin Jin", title = "The Wandering Edge",
                role = CharacterRole.Duelist, color = new Color(0.88f, 0.28f, 0.38f),
                hp = 1050, atk = 205, def = 80, spd = 90,
                bio = "A masterless swordsman possessing lightning-fast draw techniques.",
                abilities = new [] {
                    ("jin_ab_01", "Iaido Cut", "Instant draw strike with heightened critical chance.", AbilityType.Primary, 2f),
                    ("jin_ab_02", "Parry & Counter", "Deflects incoming frontal attacks with a riposte strike.", AbilityType.Skill, 5f),
                    ("jin_ab_03", "Flowing Blade", "Landing attacks increases attack speed incrementally.", AbilityType.Passive, 0f),
                    ("jin_ab_04", "Cherry Blossom Tempest", "Blinks between targets delivering twelve lethal strikes.", AbilityType.Ultimate, 22f)
                }
            },
            new CharDef {
                id = "clashura_char_16", name = "Tahlia Frost", title = "Glacial Sentinel",
                role = CharacterRole.Sentinel, color = new Color(0.22f, 0.84f, 0.86f),
                hp = 1400, atk = 130, def = 150, spd = 50,
                bio = "Shapes indestructible ice to control the battle line and suppress rushes.",
                abilities = new [] {
                    ("tahl_ab_01", "Frost Lance", "Throws an icicle slowing target attack and move speed.", AbilityType.Primary, 2.5f),
                    ("tahl_ab_02", "Glacial Barrier", "Raises a thick ice wall blocking enemy movement and shots.", AbilityType.Skill, 9f),
                    ("tahl_ab_03", "Chilled Domain", "Enemies in close proximity suffer progressive frostbite.", AbilityType.Passive, 0f),
                    ("tahl_ab_04", "Deep Freeze", "Submerges the area in subzero temperatures freezing foes.", AbilityType.Ultimate, 26f)
                }
            },
            new CharDef {
                id = "clashura_char_17", name = "Orion Sol", title = "The Cosmic Guardian",
                role = CharacterRole.Sentinel, color = new Color(0.86f, 0.66f, 0.24f),
                hp = 1450, atk = 125, def = 160, spd = 45,
                bio = "Harnesses gravitational fields and stellar energy to anchor defense lines.",
                abilities = new [] {
                    ("orion_ab_01", "Stellar Pulse", "Pulses concentrated cosmic radiation in front.", AbilityType.Primary, 3f),
                    ("orion_ab_02", "Gravitational Well", "Deploys an anchor pulling enemy projectiles into the void.", AbilityType.Skill, 8f),
                    ("orion_ab_03", "Solar Armor", "Damaging Orion causes blinding flashes reducing enemy accuracy.", AbilityType.Passive, 0f),
                    ("orion_ab_04", "Supernova Shock", "Channels stellar collapse unleashing a massive stunning explosion.", AbilityType.Ultimate, 28f)
                }
            },
            new CharDef {
                id = "clashura_char_18", name = "Vesper", title = "Phantom of the Void",
                role = CharacterRole.Duelist, color = new Color(0.12f, 0.22f, 0.52f),
                hp = 980, atk = 190, def = 85, spd = 92,
                bio = "An assassin cloaked in midnight ether who strikes from blind spots.",
                abilities = new [] {
                    ("vesp_ab_01", "Void Stiletto", "Rapid dual dagger punctures ignoring base armor.", AbilityType.Primary, 2f),
                    ("vesp_ab_02", "Shadow Blink", "Teleports instantly behind the target with bonus damage.", AbilityType.Skill, 5f),
                    ("vesp_ab_03", "Veil of Shadows", "Becomes completely invisible for 2.5 seconds on each kill.", AbilityType.Passive, 0f),
                    ("vesp_ab_04", "Phantom Mirage", "Creates 3 shadow clones that converge and assassinate the target.", AbilityType.Ultimate, 20f)
                }
            }
        };

        [MenuItem("Clashura/Generate 2D Characters and Assets")]
        public static void GenerateAll2D()
        {
            Debug.Log("[ClashuraAssetGenerator] Starting 2D asset generation...");

            Ensure2DFolders();

            var portraitDict = GeneratePortraits();
            var artworkDict = GenerateSelectionArtworks();
            var spriteDict = GenerateGameplaySprites();
            var iconDict = GenerateAbilityIcons();
            var abilityDict = GenerateAbilityData(iconDict);
            var prefabDict = Generate2DCharacterPrefabs(spriteDict);

            var characterList = GenerateCharacterData(portraitDict, artworkDict, spriteDict, prefabDict, abilityDict);
            GenerateDatabase(characterList);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ClashuraAssetGenerator] Successfully generated all 18 2D characters, artworks, sprites, and prefabs!");
        }

        private static void Ensure2DFolders()
        {
            string[] folders = {
                "Assets/Clashura/Art/Characters/Portraits",
                "Assets/Clashura/Art/Characters/SelectionArtwork",
                "Assets/Clashura/Art/Characters/Sprites",
                "Assets/Clashura/Art/Characters/SpriteSheets",
                "Assets/Clashura/Art/Characters/Icons",
                "Assets/Clashura/Prefabs/Characters",
                "Assets/Clashura/Data/Characters/ScriptableObjects",
                "Assets/Clashura/Data/Characters/AbilityData"
            };

            foreach (var f in folders)
            {
                if (!AssetDatabase.IsValidFolder(f))
                {
                    Directory.CreateDirectory(Path.Combine(Application.dataPath, f.Substring("Assets/".Length)));
                }
            }
            AssetDatabase.Refresh();
        }

        private static Dictionary<string, Sprite> GeneratePortraits()
        {
            var dict = new Dictionary<string, Sprite>();

            foreach (var ch in Characters)
            {
                string path = $"Assets/Clashura/Art/Characters/Portraits/Portrait_{ch.name}.png";
                Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);

                for (int y = 0; y < 128; y++)
                {
                    for (int x = 0; x < 128; x++)
                    {
                        if (x < 5 || x > 122 || y < 5 || y > 122)
                        {
                            tex.SetPixel(x, y, new Color(0.1f, 0.12f, 0.16f, 1f));
                        }
                        else
                        {
                            float grad = (float)y / 128f * 0.4f + 0.6f;
                            tex.SetPixel(x, y, new Color(ch.color.r * grad, ch.color.g * grad, ch.color.b * grad, 1f));
                        }
                    }
                }
                tex.Apply();

                SaveAndImportSprite(path, tex);
                dict[ch.id] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return dict;
        }

        private static Dictionary<string, Sprite> GenerateSelectionArtworks()
        {
            var dict = new Dictionary<string, Sprite>();

            foreach (var ch in Characters)
            {
                string path = $"Assets/Clashura/Art/Characters/SelectionArtwork/Artwork_{ch.name}.png";
                // High-res portrait artwork (256x384)
                Texture2D tex = new Texture2D(256, 384, TextureFormat.RGBA32, false);

                for (int y = 0; y < 384; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        // Outer decorative vignette/frame
                        if (x < 6 || x > 249 || y < 6 || y > 377)
                        {
                            tex.SetPixel(x, y, new Color(0.12f, 0.15f, 0.2f, 1f));
                        }
                        else
                        {
                            float normY = (float)y / 384f;
                            float normX = (float)x / 256f;

                            // Dynamic stylized gradient background
                            Color c = Color.Lerp(ch.color * 0.7f, ch.color * 1.15f, normY);

                            // Central hero silhouette accent
                            float distFromCenter = Mathf.Abs(normX - 0.5f);
                            if (distFromCenter < 0.28f && normY > 0.18f && normY < 0.85f)
                            {
                                c = Color.Lerp(c, Color.white, 0.25f);
                            }

                            tex.SetPixel(x, y, c);
                        }
                    }
                }
                tex.Apply();

                SaveAndImportSprite(path, tex);
                dict[ch.id] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return dict;
        }

        private static Dictionary<string, Sprite> GenerateGameplaySprites()
        {
            var dict = new Dictionary<string, Sprite>();

            foreach (var ch in Characters)
            {
                string path = $"Assets/Clashura/Art/Characters/Sprites/Sprite_{ch.name}.png";
                Texture2D tex = new Texture2D(64, 96, TextureFormat.RGBA32, false);

                for (int y = 0; y < 96; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        // Clean outline for 2D character sprite
                        if (x < 2 || x > 61 || y < 2 || y > 93)
                        {
                            tex.SetPixel(x, y, Color.clear);
                        }
                        else
                        {
                            tex.SetPixel(x, y, ch.color);
                        }
                    }
                }
                tex.Apply();

                SaveAndImportSprite(path, tex);
                dict[ch.id] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return dict;
        }

        private static Dictionary<string, Sprite> GenerateAbilityIcons()
        {
            var dict = new Dictionary<string, Sprite>();

            foreach (var ch in Characters)
            {
                foreach (var ab in ch.abilities)
                {
                    string path = $"Assets/Clashura/Art/Characters/Icons/Icon_{ab.id}.png";
                    Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                    Color abCol = ch.color * 0.85f;

                    for (int y = 0; y < 64; y++)
                    {
                        for (int x = 0; x < 64; x++)
                        {
                            if (x < 3 || x > 60 || y < 3 || y > 60)
                            {
                                tex.SetPixel(x, y, new Color(0.12f, 0.14f, 0.18f, 1f));
                            }
                            else
                            {
                                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                                float shade = Mathf.Clamp01(1f - dist / 38f);
                                tex.SetPixel(x, y, new Color(abCol.r * shade, abCol.g * shade, abCol.b * shade, 1f));
                            }
                        }
                    }
                    tex.Apply();

                    SaveAndImportSprite(path, tex);
                    dict[ab.id] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }

            return dict;
        }

        private static Dictionary<string, AbilityDataSO> GenerateAbilityData(Dictionary<string, Sprite> icons)
        {
            var dict = new Dictionary<string, AbilityDataSO>();

            foreach (var ch in Characters)
            {
                foreach (var ab in ch.abilities)
                {
                    string path = $"Assets/Clashura/Data/Characters/AbilityData/Ability_{ab.id}.asset";
                    AbilityDataSO so = AssetDatabase.LoadAssetAtPath<AbilityDataSO>(path);
                    if (so == null)
                    {
                        so = ScriptableObject.CreateInstance<AbilityDataSO>();
                        AssetDatabase.CreateAsset(so, path);
                    }

                    icons.TryGetValue(ab.id, out Sprite iconSprite);
                    so.Initialize(ab.id, ab.name, ab.desc, ab.type, ab.cd, iconSprite);
                    EditorUtility.SetDirty(so);
                    dict[ab.id] = so;
                }
            }

            return dict;
        }

        private static Dictionary<string, GameObject> Generate2DCharacterPrefabs(Dictionary<string, Sprite> sprites)
        {
            var dict = new Dictionary<string, GameObject>();

            foreach (var ch in Characters)
            {
                string prefabPath = $"Assets/Clashura/Prefabs/Characters/Prefab_{ch.name}.prefab";

                // Pure 2D GameObject with SpriteRenderer & 2D physics
                GameObject root = new GameObject($"Character2D_{ch.name}");

                var sr = root.AddComponent<SpriteRenderer>();
                sprites.TryGetValue(ch.id, out Sprite charSprite);
                sr.sprite = charSprite;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 0;

                var col = root.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.64f, 0.96f);

                var rb = root.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);

                dict[ch.id] = savedPrefab;
            }

            return dict;
        }

        private static List<CharacterDataSO> GenerateCharacterData(
            Dictionary<string, Sprite> portraits,
            Dictionary<string, Sprite> artworks,
            Dictionary<string, Sprite> sprites,
            Dictionary<string, GameObject> prefabs,
            Dictionary<string, AbilityDataSO> abilities)
        {
            var list = new List<CharacterDataSO>();

            foreach (var ch in Characters)
            {
                string path = $"Assets/Clashura/Data/Characters/ScriptableObjects/Character_{ch.name}.asset";
                CharacterDataSO so = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(path);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<CharacterDataSO>();
                    AssetDatabase.CreateAsset(so, path);
                }

                portraits.TryGetValue(ch.id, out Sprite portrait);
                artworks.TryGetValue(ch.id, out Sprite artwork);
                sprites.TryGetValue(ch.id, out Sprite sprite);
                prefabs.TryGetValue(ch.id, out GameObject prefab2D);

                var abilityList = new List<AbilityDataSO>();
                foreach (var ab in ch.abilities)
                {
                    if (abilities.TryGetValue(ab.id, out AbilityDataSO abSO))
                    {
                        abilityList.Add(abSO);
                    }
                }

                CharacterStats stats = new CharacterStats(ch.hp, ch.atk, ch.def, ch.spd);
                so.Initialize2D(ch.id, ch.name, ch.title, ch.role, portrait, artwork, sprite, prefab2D, ch.bio, stats, abilityList);

                EditorUtility.SetDirty(so);
                list.Add(so);
            }

            return list;
        }

        private static void GenerateDatabase(List<CharacterDataSO> characters)
        {
            string dbPath = "Assets/Clashura/Data/Characters/ScriptableObjects/CharacterDatabase.asset";
            CharacterDatabaseSO db = AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<CharacterDatabaseSO>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            db.SetCharacters(characters);
            EditorUtility.SetDirty(db);

            if (db.ValidateDatabase(out var errors))
            {
                Debug.Log($"[ClashuraAssetGenerator] 2D Central Database validated successfully! Total characters: {db.CharacterCount}");
            }
            else
            {
                Debug.LogWarning($"[ClashuraAssetGenerator] Database validation returned errors:\n{string.Join("\n", errors)}");
            }
        }

        private static void SaveAndImportSprite(string assetPath, Texture2D tex)
        {
            byte[] bytes = tex.EncodeToPNG();
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllBytes(fullPath, bytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
