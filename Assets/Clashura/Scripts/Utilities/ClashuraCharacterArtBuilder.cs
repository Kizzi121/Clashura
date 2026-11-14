#if UNITY_EDITOR
using System;
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
    /// Generates distinct, fighting-game inspired 2D artwork and sprites for all 18 characters,
    /// featuring diverse body and face archetypes:
    /// - Humans (Martial artists, paladins, rangers, duelists, samurai)
    /// - Beast-Humanoids (Wolf, falcon, bear, kitsune fox, rhino, snow leopard)
    /// - Robots & Cyborgs (Mecha androids, war cyborgs, solar automata, cyber ninjas)
    /// - Supernatural / Monsters (Shadow demoness, plague ghoul)
    /// </summary>
    public static class ClashuraCharacterArtBuilder
    {
        public enum BodyArchetype
        {
            Human,
            Beast,
            Robot,
            Demon
        }

        public enum SpeciesType
        {
            // Humans (6)
            HumanKnight,     // Aethelgard: Golden helmet & armored human knight
            HumanRanger,     // Lyra Thorne: Agile archer with hair & quiver
            HumanDuelist,    // Kaelen Voss: Arcane spellblade duelist with flowing coat
            HumanPriestess,  // Seraphina: Angelic priestess with halo & robes
            HumanBrawler,    // Ignis Rex: Spiky-haired flame warrior with broadsword
            HumanSamurai,    // Ronin Jin: Traditional samurai with katana & haori

            // Beasts / Anthropomorphic Animal Fighters (6)
            BeastWolf,       // Valkira: Crimson Wolf beast-fighter with wolf muzzle, ears, fangs, claws
            BeastFalcon,     // Zephyrion: Avian hawk/falcon warrior with raptor beak, feather crest, talons
            BeastBear,       // Brakmor: Heavy mountain bear beast-fighter with bear snout, thick fur, warhammer
            BeastFox,        // Selene Mist: Lunar Kitsune fox with fox ears, elegant snout, multiple tails
            BeastRhino,      // Gorgar: Armored rhino beast-titan with massive horn on snout, rocky hide
            BeastLeopard,    // Tahlia Frost: Glacial snow leopard with cat ears, feline whiskers, spotted pelt

            // Robots / Cyborgs / Androids (4)
            RobotAndroid,    // Astrid Storm: Lightning mecha-android with glowing LED visor & capacitors
            RobotCyborg,     // Darius Hex: Heavy war cyborg with optic laser eye & hydraulic tubes
            RobotAutomaton,  // Orion Sol: Golden celestial automaton with core eye & floating rings
            RobotNinja,      // Vesper: Cyber-shinobi with digital visor & carbon nanite blades

            // Demons / Monsters (2)
            MonsterDemon,    // Nyxara: Void shadow demoness with sweeping horns & abyssal wings
            MonsterGhoul     // Morvath: Toxic plague ghoul with mutated spines & gas canister
        }

        private struct CharArtDef
        {
            public string id;
            public string name;
            public string title;
            public CharacterRole role;
            public BodyArchetype archetype;
            public SpeciesType species;
            public Color primaryColor;
            public Color secondaryColor;
            public Color accentColor;
            public Color fleshOrBaseColor;
            public int hp;
            public int atk;
            public int def;
            public int spd;
            public string bio;
            public (string id, string name, string desc, AbilityType type, float cd)[] abilities;
        }

        private static readonly CharArtDef[] Characters = new CharArtDef[]
        {
            // ================= 1. HUMAN: Aethelgard =================
            new CharArtDef {
                id = "clashura_char_01", name = "Aethelgard",
                title = "The Dawn Paladin [Human Knight]",
                role = CharacterRole.Vanguard,
                archetype = BodyArchetype.Human,
                species = SpeciesType.HumanKnight,
                primaryColor = new Color(0.95f, 0.76f, 0.20f), // Golden Amber
                secondaryColor = new Color(0.20f, 0.25f, 0.35f), // Steel Navy
                accentColor = new Color(1.0f, 0.95f, 0.60f), // Sunlight White
                fleshOrBaseColor = new Color(0.92f, 0.78f, 0.68f), // Human skin
                hp = 1600, atk = 95, def = 190, spd = 35,
                bio = "A venerable human paladin encased in sun-forged armor, standing as an immovable bastion.",
                abilities = new [] {
                    ("aeth_ab_01", "Dawn's Bastion", "Erects a golden aegis absorbing massive damage.", AbilityType.Skill, 8f),
                    ("aeth_ab_02", "Aegis Strike", "Shield bash that stuns all foes directly in front.", AbilityType.Primary, 3f),
                    ("aeth_ab_03", "Solar Radiance", "Passively restores armor while maintaining position.", AbilityType.Passive, 0f),
                    ("aeth_ab_04", "Wrath of the Sun", "Unleashes an incandescent shockwave across the field.", AbilityType.Ultimate, 25f)
                }
            },

            // ================= 2. BEAST: Valkira =================
            new CharArtDef {
                id = "clashura_char_02", name = "Valkira",
                title = "The Crimson Dire-Wolf [Beast-Warrior]",
                role = CharacterRole.Striker,
                archetype = BodyArchetype.Beast,
                species = SpeciesType.BeastWolf,
                primaryColor = new Color(0.85f, 0.12f, 0.22f), // Crimson Red
                secondaryColor = new Color(0.18f, 0.05f, 0.08f), // Deep Blood
                accentColor = new Color(1.0f, 0.35f, 0.45f), // Bright Scarlet
                fleshOrBaseColor = new Color(0.55f, 0.15f, 0.18f), // Wolf Fur
                hp = 1100, atk = 210, def = 90, spd = 70,
                bio = "A fierce lupine beast-fighter with wolf muzzle, razor fangs, and savage claws wielding dual crimson cleavers.",
                abilities = new [] {
                    ("valk_ab_01", "Blood Cleave", "Wide horizontal slash inflicting deep bleed wounds.", AbilityType.Primary, 2.5f),
                    ("valk_ab_02", "Crimson Dash", "Flits past opponent defenses with piercing speed.", AbilityType.Skill, 6f),
                    ("valk_ab_03", "Berserk Fury", "Grants escalating attack power as health diminishes.", AbilityType.Passive, 0f),
                    ("valk_ab_04", "Bloodstorm Execution", "A devastating aerial whirlwind shredding all nearby targets.", AbilityType.Ultimate, 22f)
                }
            },

            // ================= 3. BEAST: Zephyrion =================
            new CharArtDef {
                id = "clashura_char_03", name = "Zephyrion",
                title = "The Sky Gale Raptor [Avian Beast-Warrior]",
                role = CharacterRole.Duelist,
                archetype = BodyArchetype.Beast,
                species = SpeciesType.BeastFalcon,
                primaryColor = new Color(0.15f, 0.80f, 0.95f), // Sky Cyan
                secondaryColor = new Color(0.10f, 0.25f, 0.40f), // Gale Navy
                accentColor = new Color(0.80f, 0.98f, 1.0f), // Cloud Silver
                fleshOrBaseColor = new Color(0.70f, 0.55f, 0.35f), // Raptor Feathers
                hp = 950, atk = 180, def = 75, spd = 95,
                bio = "An agile avian raptor warrior whose razor beak, feathery crest, and wind rapier dominate the skies.",
                abilities = new [] {
                    ("zeph_ab_01", "Gale Thrust", "Pierces defenses with aerodynamic wind-infused stabs.", AbilityType.Primary, 2f),
                    ("zeph_ab_02", "Tailwind Evade", "Dashes backward leaving behind a disorienting cyclone.", AbilityType.Skill, 5f),
                    ("zeph_ab_03", "Air Supremacy", "Higher agility and evasion while airborne or sprinting.", AbilityType.Passive, 0f),
                    ("zeph_ab_04", "Eye of the Typhoon", "Traps opponents in a spiraling vortex dealing continuous cuts.", AbilityType.Ultimate, 20f)
                }
            },

            // ================= 4. DEMON: Nyxara =================
            new CharArtDef {
                id = "clashura_char_04", name = "Nyxara",
                title = "The Void Demoness [Shadow Creature]",
                role = CharacterRole.Specialist,
                archetype = BodyArchetype.Demon,
                species = SpeciesType.MonsterDemon,
                primaryColor = new Color(0.60f, 0.20f, 0.85f), // Void Purple
                secondaryColor = new Color(0.10f, 0.05f, 0.18f), // Abyssal Black
                accentColor = new Color(0.90f, 0.55f, 1.0f), // Neon Magenta
                fleshOrBaseColor = new Color(0.25f, 0.15f, 0.35f), // Dark Violet Carapace
                hp = 900, atk = 195, def = 70, spd = 60,
                bio = "An otherworldly demoness crowned with curved horns, weaving shadow tendrils to ensnare prey.",
                abilities = new [] {
                    ("nyx_ab_01", "Abyssal Grasp", "Shadow hands emerge from the ground clutching enemies.", AbilityType.Primary, 3f),
                    ("nyx_ab_02", "Umbral Rift", "Teleports through a void portal dealing area shadow damage.", AbilityType.Skill, 7f),
                    ("nyx_ab_03", "Soul Harvest", "Restores health whenever a nearby enemy takes heavy damage.", AbilityType.Passive, 0f),
                    ("nyx_ab_04", "Nether Cataclysm", "Collapses a black hole implosion pulling in all combatants.", AbilityType.Ultimate, 24f)
                }
            },

            // ================= 5. BEAST: Brakmor =================
            new CharArtDef {
                id = "clashura_char_05", name = "Brakmor",
                title = "The Ironhide Ursine [Beast-Warrior]",
                role = CharacterRole.Vanguard,
                archetype = BodyArchetype.Beast,
                species = SpeciesType.BeastBear,
                primaryColor = new Color(0.85f, 0.45f, 0.15f), // Magma Brown
                secondaryColor = new Color(0.15f, 0.12f, 0.10f), // Granite Obsidian
                accentColor = new Color(1.0f, 0.70f, 0.20f), // Fire Amber
                fleshOrBaseColor = new Color(0.40f, 0.25f, 0.15f), // Bear Fur
                hp = 1800, atk = 120, def = 175, spd = 30,
                bio = "A colossal bear beast-fighter wielding an earthen warhammer with crushing brute strength.",
                abilities = new [] {
                    ("brak_ab_01", "Earthshaker", "Slams his hammer into the earth fracturing ground.", AbilityType.Primary, 3f),
                    ("brak_ab_02", "Titan's Roar", "Terrifying battle roar disorienting and weakening enemies.", AbilityType.Skill, 8f),
                    ("brak_ab_03", "Stone Plating", "Gains temporary bonus armor whenever taking sustained hits.", AbilityType.Passive, 0f),
                    ("brak_ab_04", "Tectonic Rupture", "Shatters the battlefield sending magma fissures outward.", AbilityType.Ultimate, 26f)
                }
            },

            // ================= 6. HUMAN: Lyra Thorne =================
            new CharArtDef {
                id = "clashura_char_06", name = "Lyra Thorne",
                title = "The Verdant Huntress [Human Ranger]",
                role = CharacterRole.Striker,
                archetype = BodyArchetype.Human,
                species = SpeciesType.HumanRanger,
                primaryColor = new Color(0.20f, 0.75f, 0.35f), // Forest Emerald
                secondaryColor = new Color(0.12f, 0.22f, 0.14f), // Deep Bark
                accentColor = new Color(0.80f, 0.95f, 0.40f), // Sunlight Lime
                fleshOrBaseColor = new Color(0.95f, 0.82f, 0.72f), // Fair skin
                hp = 920, atk = 185, def = 80, spd = 75,
                bio = "A swift human marksman who defends ancient woodlands with pinpoint recurve bow accuracy.",
                abilities = new [] {
                    ("lyra_ab_01", "Piercing Volley", "Fires a rapid volley of arrows piercing enemy lines.", AbilityType.Primary, 2.5f),
                    ("lyra_ab_02", "Entangling Shot", "Fires an arrow that bursts into grasping thorny briars.", AbilityType.Skill, 6.5f),
                    ("lyra_ab_03", "Hunter's Mark", "Attacking the same target inflicts stacked critical damage.", AbilityType.Passive, 0f),
                    ("lyra_ab_04", "Storm of Brambles", "Showers the arena in a hail of enchanted piercing arrows.", AbilityType.Ultimate, 22f)
                }
            },

            // ================= 7. HUMAN: Kaelen Voss =================
            new CharArtDef {
                id = "clashura_char_07", name = "Kaelen Voss",
                title = "The Arcane Spellblade [Human Duelist]",
                role = CharacterRole.Duelist,
                archetype = BodyArchetype.Human,
                species = SpeciesType.HumanDuelist,
                primaryColor = new Color(0.35f, 0.30f, 0.90f), // Arcane Indigo
                secondaryColor = new Color(0.08f, 0.08f, 0.22f), // Mystic Navy
                accentColor = new Color(0.65f, 0.80f, 1.0f), // Astral Silver
                fleshOrBaseColor = new Color(0.90f, 0.76f, 0.68f), // Human skin
                hp = 1000, atk = 175, def = 85, spd = 70,
                bio = "A stylish human duelist who combines martial fencing with shimmering arcane rune blades.",
                abilities = new [] {
                    ("kael_ab_01", "Runic Flurry", "Rapid multi-hit slashes infused with mystical force.", AbilityType.Primary, 2f),
                    ("kael_ab_02", "Phase Shift", "Briefly phases out of reality dodging all harm.", AbilityType.Skill, 5.5f),
                    ("kael_ab_03", "Mana Blade", "Attacks consume excess energy to deal amplified bonus magic.", AbilityType.Passive, 0f),
                    ("kael_ab_04", "Astral Convergence", "Summons a constellation of blades homing on enemies.", AbilityType.Ultimate, 20f)
                }
            },

            // ================= 8. HUMAN: Seraphina =================
            new CharArtDef {
                id = "clashura_char_08", name = "Seraphina",
                title = "The Celestial Light [Human Priestess]",
                role = CharacterRole.Support,
                archetype = BodyArchetype.Human,
                species = SpeciesType.HumanPriestess,
                primaryColor = new Color(0.95f, 0.90f, 0.70f), // Warm Pearl
                secondaryColor = new Color(0.18f, 0.22f, 0.35f), // Twilight Steel
                accentColor = new Color(1.0f, 0.85f, 0.30f), // Sacred Gold
                fleshOrBaseColor = new Color(0.96f, 0.85f, 0.78f), // Radiant skin
                hp = 950, atk = 80, def = 95, spd = 50,
                bio = "A serene human priestess bearing an angelic halo, granting divine shields and healing.",
                abilities = new [] {
                    ("sera_ab_01", "Sacred Ray", "Channels a beam that heals allies while searing foes.", AbilityType.Primary, 2.5f),
                    ("sera_ab_02", "Divine Ward", "Shields the lowest-health ally with a damage-absorbing halo.", AbilityType.Skill, 7f),
                    ("sera_ab_03", "Benediction", "Aura that constantly regenerates team health over time.", AbilityType.Passive, 0f),
                    ("sera_ab_04", "Angelic Aegis", "Invokes celestial blessing granting team damage immunity.", AbilityType.Ultimate, 30f)
                }
            },

            // ================= 9. DEMON: Morvath =================
            new CharArtDef {
                id = "clashura_char_09", name = "Morvath",
                title = "The Toxic Abomination [Mutated Creature]",
                role = CharacterRole.Specialist,
                archetype = BodyArchetype.Demon,
                species = SpeciesType.MonsterGhoul,
                primaryColor = new Color(0.40f, 0.75f, 0.20f), // Toxic Green
                secondaryColor = new Color(0.10f, 0.15f, 0.08f), // Decay Black
                accentColor = new Color(0.70f, 1.0f, 0.30f), // Acid Lime
                fleshOrBaseColor = new Color(0.28f, 0.38f, 0.25f), // Toxic flesh
                hp = 1350, atk = 140, def = 110, spd = 45,
                bio = "A grotesque plague-mutated creature with toxic spines and foul alchemical miasma.",
                abilities = new [] {
                    ("morv_ab_01", "Noxious Fume", "Sprays corrosive cloud lowering enemy defense and sight.", AbilityType.Primary, 3f),
                    ("morv_ab_02", "Plague Flask", "Hurls volatile flask leaving toxic puddle on impact.", AbilityType.Skill, 6f),
                    ("morv_ab_03", "Rotting Aura", "Enemies in close proximity suffer progressive damage.", AbilityType.Passive, 0f),
                    ("morv_ab_04", "Epidemic Rupture", "Detonates plague pustules across all poisoned targets.", AbilityType.Ultimate, 25f)
                }
            },

            // ================= 10. HUMAN: Ignis Rex =================
            new CharArtDef {
                id = "clashura_char_10", name = "Ignis Rex",
                title = "The Flame Gladiator [Human Brawler]",
                role = CharacterRole.Striker,
                archetype = BodyArchetype.Human,
                species = SpeciesType.HumanBrawler,
                primaryColor = new Color(0.95f, 0.35f, 0.10f), // Blaze Orange
                secondaryColor = new Color(0.20f, 0.06f, 0.04f), // Scorched Charcoal
                accentColor = new Color(1.0f, 0.80f, 0.20f), // Solar Flare
                fleshOrBaseColor = new Color(0.88f, 0.72f, 0.60f), // Tan brawler skin
                hp = 1150, atk = 205, def = 85, spd = 65,
                bio = "A fiery human brawler with spiked red hair, wielding a scorching broadsword and flaming fists.",
                abilities = new [] {
                    ("ignis_ab_01", "Flame Cleave", "Broadsword sweep that ignites targets in blaze.", AbilityType.Primary, 2.5f),
                    ("ignis_ab_02", "Meteor Slam", "Leaps into the air slamming down creating shockwave.", AbilityType.Skill, 7f),
                    ("ignis_ab_03", "Ignition", "Normal attacks leave burning embers dealing dot.", AbilityType.Passive, 0f),
                    ("ignis_ab_04", "Infernal Awakening", "Supercharges sword unleashing waves of molten fire.", AbilityType.Ultimate, 22f)
                }
            },

            // ================= 11. BEAST: Selene Mist =================
            new CharArtDef {
                id = "clashura_char_11", name = "Selene Mist",
                title = "The Lunar Kitsune [Fox Beast-Warrior]",
                role = CharacterRole.Support,
                archetype = BodyArchetype.Beast,
                species = SpeciesType.BeastFox,
                primaryColor = new Color(0.35f, 0.75f, 0.95f), // Celestial Blue
                secondaryColor = new Color(0.08f, 0.12f, 0.25f), // Midnight Ocean
                accentColor = new Color(0.85f, 0.95f, 1.0f), // Lunar Glow
                fleshOrBaseColor = new Color(0.82f, 0.88f, 0.95f), // White Fox fur
                hp = 980, atk = 85, def = 90, spd = 60,
                bio = "A mystical fox beast-sorceress with fox ears, muzzle, and ethereal tails channeling lunar magic.",
                abilities = new [] {
                    ("sele_ab_01", "Moonlight Spark", "Discharges orbs of lunar magic soothing allies.", AbilityType.Primary, 2f),
                    ("sele_ab_02", "Mist Veil", "Cloaks the team in moonlight shroud granting stealth.", AbilityType.Skill, 8f),
                    ("sele_ab_03", "Crescent Ward", "Surrounds team with lunar shields deflecting attacks.", AbilityType.Passive, 0f),
                    ("sele_ab_04", "Lunar Eclipse", "Blinds all enemies while replenishing team energy.", AbilityType.Ultimate, 28f)
                }
            },

            // ================= 12. BEAST: Gorgar =================
            new CharArtDef {
                id = "clashura_char_12", name = "Gorgar",
                title = "The Horned Titan [Rhino Beast-Warrior]",
                role = CharacterRole.Vanguard,
                archetype = BodyArchetype.Beast,
                species = SpeciesType.BeastRhino,
                primaryColor = new Color(0.50f, 0.45f, 0.40f), // Granite Grey
                secondaryColor = new Color(0.18f, 0.15f, 0.12f), // Deep Cavern
                accentColor = new Color(0.75f, 0.85f, 0.60f), // Moss Sprout
                fleshOrBaseColor = new Color(0.45f, 0.42f, 0.38f), // Armored rhino hide
                hp = 1900, atk = 110, def = 205, spd = 28,
                bio = "A colossal rhino beast-juggernaut with a reinforced snout horn and boulder-shattering fists.",
                abilities = new [] {
                    ("gorg_ab_01", "Horn Gore", "Charges forward impaling enemies on his reinforced horn.", AbilityType.Primary, 3.5f),
                    ("gorg_ab_02", "Granite Skin", "Hardens skin into solid stone temporarily doubling defense.", AbilityType.Skill, 10f),
                    ("gorg_ab_03", "Bedrock Core", "Greatly reduces incoming physical damage.", AbilityType.Passive, 0f),
                    ("gorg_ab_04", "Avalanche Rampage", "Transforms into a rolling boulder crushing enemy formations.", AbilityType.Ultimate, 28f)
                }
            },

            // ================= 13. ROBOT: Astrid Storm =================
            new CharArtDef {
                id = "clashura_char_13", name = "Astrid Storm",
                title = "The Volt Android [High-Tech Robot]",
                role = CharacterRole.Striker,
                archetype = BodyArchetype.Robot,
                species = SpeciesType.RobotAndroid,
                primaryColor = new Color(0.95f, 0.88f, 0.12f), // Volt Yellow
                secondaryColor = new Color(0.20f, 0.12f, 0.35f), // Storm Violet
                accentColor = new Color(0.10f, 0.95f, 1.0f), // Electric Cyan LED
                fleshOrBaseColor = new Color(0.80f, 0.82f, 0.88f), // Titanium Chrome
                hp = 1100, atk = 200, def = 85, spd = 85,
                bio = "A high-speed combat android featuring an electric LED visor, tesla coils, and hydraulic thrusters.",
                abilities = new [] {
                    ("astr_ab_01", "Lightning Arc", "Discharges lightning bolts that jump between nearby foes.", AbilityType.Primary, 2f),
                    ("astr_ab_02", "Thunder Surge", "High-voltage dash that leaves an electrified trail.", AbilityType.Skill, 6f),
                    ("astr_ab_03", "Static Buildup", "Every 4th attack delivers an instant critical lightning strike.", AbilityType.Passive, 0f),
                    ("astr_ab_04", "Cataclysmic Storm", "Summons a torrential thunderstorm striking lightning everywhere.", AbilityType.Ultimate, 24f)
                }
            },

            // ================= 14. ROBOT: Darius Hex =================
            new CharArtDef {
                id = "clashura_char_14", name = "Darius Hex",
                title = "The Dread War Cyborg [Cybernetic Cyborg]",
                role = CharacterRole.Specialist,
                archetype = BodyArchetype.Robot,
                species = SpeciesType.RobotCyborg,
                primaryColor = new Color(0.55f, 0.12f, 0.50f), // Royal Plum
                secondaryColor = new Color(0.12f, 0.04f, 0.12f), // Crypt Shadow
                accentColor = new Color(1.0f, 0.15f, 0.20f), // Laser Red Optic
                fleshOrBaseColor = new Color(0.45f, 0.45f, 0.50f), // Cybernetic Steel
                hp = 1200, atk = 165, def = 95, spd = 55,
                bio = "A battle-hardened war cyborg with an infrared optic eye, hydraulic chassis, and life-draining conduits.",
                abilities = new [] {
                    ("dari_ab_01", "Life Siphon", "Channels a dark beam draining target health directly.", AbilityType.Primary, 2.5f),
                    ("dari_ab_02", "Hex of Fragility", "Places a blood curse weakening target defense significantly.", AbilityType.Skill, 8f),
                    ("dari_ab_03", "Sanguine Hunger", "Gains 25% movement speed when pursuing wounded foes.", AbilityType.Passive, 0f),
                    ("dari_ab_04", "Crimson Harvest", "Drains life simultaneously from all enemies in proximity.", AbilityType.Ultimate, 26f)
                }
            },

            // ================= 15. HUMAN: Ronin Jin =================
            new CharArtDef {
                id = "clashura_char_15", name = "Ronin Jin",
                title = "The Shadow Blade [Human Samurai]",
                role = CharacterRole.Duelist,
                archetype = BodyArchetype.Human,
                species = SpeciesType.HumanSamurai,
                primaryColor = new Color(0.90f, 0.25f, 0.35f), // Crimson Rose
                secondaryColor = new Color(0.18f, 0.08f, 0.10f), // Lacquer Black
                accentColor = new Color(1.0f, 0.75f, 0.85f), // Sakura Pink
                fleshOrBaseColor = new Color(0.92f, 0.78f, 0.68f), // Human skin
                hp = 1050, atk = 205, def = 80, spd = 90,
                bio = "A disciplined human samurai with topknot and haori, mastering lightning-fast katana draw cuts.",
                abilities = new [] {
                    ("jin_ab_01", "Iaido Cut", "Instant draw strike with heightened critical chance.", AbilityType.Primary, 2f),
                    ("jin_ab_02", "Parry & Counter", "Deflects incoming frontal attacks with a riposte strike.", AbilityType.Skill, 5f),
                    ("jin_ab_03", "Flowing Blade", "Landing attacks increases attack speed incrementally.", AbilityType.Passive, 0f),
                    ("jin_ab_04", "Cherry Blossom Tempest", "Blinks between targets delivering twelve lethal strikes.", AbilityType.Ultimate, 22f)
                }
            },

            // ================= 16. BEAST: Tahlia Frost =================
            new CharArtDef {
                id = "clashura_char_16", name = "Tahlia Frost",
                title = "The Frost Panther [Leopard Beast-Warrior]",
                role = CharacterRole.Sentinel,
                archetype = BodyArchetype.Beast,
                species = SpeciesType.BeastLeopard,
                primaryColor = new Color(0.18f, 0.82f, 0.88f), // Glacial Aqua
                secondaryColor = new Color(0.06f, 0.20f, 0.28f), // Deep Abyss
                accentColor = new Color(0.85f, 0.98f, 1.0f), // Diamond Frost
                fleshOrBaseColor = new Color(0.75f, 0.88f, 0.92f), // Spotted snow fur
                hp = 1400, atk = 130, def = 150, spd = 50,
                bio = "A glacial leopard beast-huntress with rounded cat ears, feline whiskers, and subzero icy claws.",
                abilities = new [] {
                    ("tahl_ab_01", "Frost Lance", "Throws an icicle slowing target attack and move speed.", AbilityType.Primary, 2.5f),
                    ("tahl_ab_02", "Glacial Barrier", "Raises a thick ice wall blocking enemy movement and shots.", AbilityType.Skill, 9f),
                    ("tahl_ab_03", "Chilled Domain", "Enemies in close proximity suffer progressive frostbite.", AbilityType.Passive, 0f),
                    ("tahl_ab_04", "Deep Freeze", "Submerges the area in subzero temperatures freezing foes.", AbilityType.Ultimate, 26f)
                }
            },

            // ================= 17. ROBOT: Orion Sol =================
            new CharArtDef {
                id = "clashura_char_17", name = "Orion Sol",
                title = "The Solar Automaton [Orbital Robot]",
                role = CharacterRole.Sentinel,
                archetype = BodyArchetype.Robot,
                species = SpeciesType.RobotAutomaton,
                primaryColor = new Color(0.88f, 0.62f, 0.20f), // Celestial Gold
                secondaryColor = new Color(0.08f, 0.08f, 0.22f), // Cosmos Deep Blue
                accentColor = new Color(1.0f, 0.90f, 0.40f), // Solar Flare Eye
                fleshOrBaseColor = new Color(0.85f, 0.75f, 0.50f), // Polished Brass/Gold
                hp = 1450, atk = 125, def = 160, spd = 45,
                bio = "A golden spherical automaton with a glowing solar core eye and levitating orbital rings.",
                abilities = new [] {
                    ("orion_ab_01", "Stellar Pulse", "Pulses concentrated cosmic radiation in front.", AbilityType.Primary, 3f),
                    ("orion_ab_02", "Gravitational Well", "Deploys an anchor pulling enemy projectiles into the void.", AbilityType.Skill, 8f),
                    ("orion_ab_03", "Solar Armor", "Damaging Orion causes blinding flashes reducing enemy accuracy.", AbilityType.Passive, 0f),
                    ("orion_ab_04", "Supernova Shock", "Channels stellar collapse unleashing a massive stunning explosion.", AbilityType.Ultimate, 28f)
                }
            },

            // ================= 18. ROBOT: Vesper =================
            new CharArtDef {
                id = "clashura_char_18", name = "Vesper",
                title = "The Cyber Shinobi [Stealth Android]",
                role = CharacterRole.Duelist,
                archetype = BodyArchetype.Robot,
                species = SpeciesType.RobotNinja,
                primaryColor = new Color(0.12f, 0.18f, 0.52f), // Midnight Navy
                secondaryColor = new Color(0.05f, 0.06f, 0.15f), // Void Shadow
                accentColor = new Color(0.20f, 0.85f, 1.0f), // Digital Cyan
                fleshOrBaseColor = new Color(0.25f, 0.28f, 0.35f), // Carbon Fiber
                hp = 980, atk = 190, def = 85, spd = 92,
                bio = "A covert cyber-shinobi android featuring a carbon-fiber ninja visor and dual vibro-daggers.",
                abilities = new [] {
                    ("vesp_ab_01", "Void Stiletto", "Rapid dual dagger punctures ignoring base armor.", AbilityType.Primary, 2f),
                    ("vesp_ab_02", "Shadow Blink", "Teleports instantly behind the target with bonus damage.", AbilityType.Skill, 5f),
                    ("vesp_ab_03", "Veil of Shadows", "Becomes completely invisible for 2.5 seconds on each kill.", AbilityType.Passive, 0f),
                    ("vesp_ab_04", "Phantom Mirage", "Creates 3 shadow clones that converge and assassinate the target.", AbilityType.Ultimate, 20f)
                }
            }
        };

        [MenuItem("Clashura/Generate All Assets")]
        public static void GeneratePerCharacterArt()
        {
            Debug.Log("[ClashuraCharacterArtBuilder] Generating Fighting-Game Archetype Assets (Human, Beast, Robot, Monster)...");

            string rootArtFolder = "Assets/Clashura/Art/Characters";
            EnsureFolder(rootArtFolder);

            string soFolder = "Assets/Clashura/Data/Characters/ScriptableObjects";
            EnsureFolder(soFolder);

            string abilityFolder = "Assets/Clashura/Data/Characters/AbilityData";
            EnsureFolder(abilityFolder);

            string prefabFolder = "Assets/Clashura/Prefabs/Characters";
            EnsureFolder(prefabFolder);

            var createdCharacters = new List<CharacterDataSO>();

            for (int i = 0; i < Characters.Length; i++)
            {
                var ch = Characters[i];
                string charFolder = $"{rootArtFolder}/{ch.name}";
                EnsureFolder(charFolder);

                string artPath = $"{charFolder}/Artwork_{ch.name}.png";
                string portraitPath = $"{charFolder}/Portrait_{ch.name}.png";
                string spritePath = $"{charFolder}/Sprite_{ch.name}.png";

                // 1. Generate 2D Selection Artwork (256x384)
                GenerateUniqueArtwork(artPath, ch);

                // 2. Generate 2D Portrait (128x128)
                GenerateUniquePortrait(portraitPath, ch);

                // 3. Generate 2D Gameplay Sprite (64x96)
                GenerateUniqueGameplaySprite(spritePath, ch);

                // 4. Generate 4 Ability Icons (64x64)
                var abilityList = new List<AbilityDataSO>();
                for (int a = 0; a < ch.abilities.Length; a++)
                {
                    var abDef = ch.abilities[a];
                    string iconPath = $"{charFolder}/Icon_{abDef.id}.png";
                    GenerateUniqueAbilityIcon(iconPath, ch, abDef.type);

                    Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

                    string abSoPath = $"{abilityFolder}/{abDef.id}.asset";
                    AbilityDataSO abSo = AssetDatabase.LoadAssetAtPath<AbilityDataSO>(abSoPath);
                    if (abSo == null)
                    {
                        abSo = ScriptableObject.CreateInstance<AbilityDataSO>();
                        AssetDatabase.CreateAsset(abSo, abSoPath);
                    }
                    abSo.Initialize(abDef.id, abDef.name, abDef.desc, abDef.type, abDef.cd, iconSprite);
                    EditorUtility.SetDirty(abSo);
                    abilityList.Add(abSo);
                }

                Sprite artSprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
                Sprite portraitSprite = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
                Sprite charSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                // 5. Generate 2D Character Prefab
                string prefabPath = $"{prefabFolder}/Character2D_{ch.name}.prefab";
                GameObject prefab = CreateCharacter2DPrefab(prefabPath, ch.name, charSprite);

                // 6. Generate/Update CharacterDataSO
                string soPath = $"{soFolder}/Character_{ch.name}.asset";
                CharacterDataSO charSO = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(soPath);
                if (charSO == null)
                {
                    charSO = ScriptableObject.CreateInstance<CharacterDataSO>();
                    AssetDatabase.CreateAsset(charSO, soPath);
                }

                CharacterStats stats = new CharacterStats(ch.hp, ch.atk, ch.def, ch.spd);
                charSO.Initialize2D(
                    ch.id,
                    ch.name,
                    ch.title,
                    ch.role,
                    portraitSprite,
                    artSprite,
                    charSprite,
                    prefab,
                    ch.bio,
                    stats,
                    abilityList
                );

                EditorUtility.SetDirty(charSO);
                createdCharacters.Add(charSO);
            }

            // 7. Update CharacterDatabaseSO
            string dbPath = $"{soFolder}/CharacterDatabase.asset";
            CharacterDatabaseSO db = AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<CharacterDatabaseSO>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            db.SetCharacters(createdCharacters);
            EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ClashuraCharacterArtBuilder] Archetype generation complete! {createdCharacters.Count} characters configured with Human, Beast, Robot, and Demon assets.");
        }

        private static void EnsureFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string fullPath = Path.Combine(Application.dataPath, folder.Substring("Assets/".Length));
                Directory.CreateDirectory(fullPath);
                AssetDatabase.Refresh();
            }
        }

        #region Pixel Drawing Helpers

        private static void SetPixelSafe(Texture2D tex, int x, int y, Color col)
        {
            if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
            {
                if (col.a < 1f)
                {
                    Color existing = tex.GetPixel(x, y);
                    Color blended = Color.Lerp(existing, col, col.a);
                    blended.a = Mathf.Max(existing.a, col.a);
                    tex.SetPixel(x, y, blended);
                }
                else
                {
                    tex.SetPixel(x, y, col);
                }
            }
        }

        private static void FillRect(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
        {
            int minX = Mathf.Min(x0, x1);
            int maxX = Mathf.Max(x0, x1);
            int minY = Mathf.Min(y0, y1);
            int maxY = Mathf.Max(y0, y1);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    SetPixelSafe(tex, x, y, col);
                }
            }
        }

        private static void FillCircle(Texture2D tex, int cx, int cy, int r, Color col)
        {
            int rSq = r * r;
            for (int y = -r; y <= r; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    if (x * x + y * y <= rSq)
                        SetPixelSafe(tex, cx + x, cy + y, col);
                }
            }
        }

        private static void FillEllipse(Texture2D tex, int cx, int cy, int rx, int ry, Color col)
        {
            if (rx <= 0 || ry <= 0) return;
            float rxSq = rx * rx;
            float rySq = ry * ry;
            for (int y = -ry; y <= ry; y++)
            {
                for (int x = -rx; x <= rx; x++)
                {
                    if ((x * x) / rxSq + (y * y) / rySq <= 1.0f)
                        SetPixelSafe(tex, cx + x, cy + y, col);
                }
            }
        }

        private static void FillTriangle(Texture2D tex, Vector2Int p0, Vector2Int p1, Vector2Int p2, Color col)
        {
            int minX = Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x));
            int maxX = Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x));
            int minY = Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y));
            int maxY = Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y));

            float Area(Vector2Int a, Vector2Int b, Vector2Int c) =>
                Mathf.Abs((a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y)) / 2f);

            float totalArea = Area(p0, p1, p2);
            if (totalArea <= 0.001f) return;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var pt = new Vector2Int(x, y);
                    float a1 = Area(pt, p1, p2);
                    float a2 = Area(p0, pt, p2);
                    float a3 = Area(p0, p1, pt);
                    if (Mathf.Abs(totalArea - (a1 + a2 + a3)) < 0.85f)
                    {
                        SetPixelSafe(tex, x, y, col);
                    }
                }
            }
        }

        private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color col, int thickness = 1)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (thickness <= 1)
                    SetPixelSafe(tex, x0, y0, col);
                else
                    FillCircle(tex, x0, y0, thickness, col);

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        #endregion

        #region Artwork Generation (256 x 384)

        private static void GenerateUniqueArtwork(string path, CharArtDef ch)
        {
            string fullPath = Path.Combine(Application.dataPath, path.Substring("Assets/".Length));
            // Preserve existing high-detail concept art cutouts if already imported
            if (File.Exists(fullPath))
            {
                byte[] existingBytes = File.ReadAllBytes(fullPath);
                Texture2D tempTex = new Texture2D(2, 2);
                if (tempTex.LoadImage(existingBytes) && tempTex.width > 300)
                {
                    return; // Keep high-res concept art
                }
            }

            int w = 256;
            int h = 384;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            // 1. Transparent Background (Background removed, isolated character only)
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }

            // 2. Draw Character in Unique Pose based on Species/Archetype
            int cx = 128;
            DrawCharacterPose(tex, cx, ch, w, h);

            tex.Apply();
            SaveAndImportSprite(path, tex);
        }

        private static void DrawCharacterPose(Texture2D tex, int cx, CharArtDef ch, int w, int h)
        {
            Color outline = new Color(0.06f, 0.08f, 0.12f, 1f);

            switch (ch.species)
            {
                case SpeciesType.HumanRanger: // Lyra Thorne: Archer Full-Draw Pose
                    // Legs in turned stance
                    FillRect(tex, cx - 24, 30, cx - 12, 140, ch.secondaryColor);
                    FillRect(tex, cx + 8, 30, cx + 20, 140, ch.secondaryColor);
                    FillRect(tex, cx - 26, 30, cx - 10, 60, ch.primaryColor); // Boots
                    FillRect(tex, cx + 6, 30, cx + 22, 60, ch.primaryColor);
                    // Tunic / Torso
                    FillEllipse(tex, cx - 4, 185, 24, 48, ch.primaryColor);
                    FillRect(tex, cx - 18, 160, cx + 10, 172, ch.accentColor); // Belt
                    // Quiver on back
                    FillRect(tex, cx + 14, 170, cx + 24, 250, ch.secondaryColor);
                    FillRect(tex, cx + 12, 250, cx + 26, 265, ch.accentColor); // Fletchings
                    // Head & Face
                    FillEllipse(tex, cx - 4, 260, 18, 22, ch.fleshOrBaseColor);
                    FillEllipse(tex, cx - 4, 272, 20, 14, ch.accentColor); // Flowing hair
                    FillCircle(tex, cx + 8, 258, 3, outline); // Eye aiming right
                    // Bow & Arms
                    // Left arm holding bow forward
                    DrawLine(tex, cx - 12, 205, cx - 45, 215, ch.secondaryColor, 5);
                    // Right arm pulling string back to cheek
                    DrawLine(tex, cx + 6, 205, cx + 8, 250, ch.secondaryColor, 5);
                    // Vertical Bow Stave
                    DrawLine(tex, cx - 45, 130, cx - 55, 215, ch.accentColor, 4);
                    DrawLine(tex, cx - 55, 215, cx - 45, 300, ch.accentColor, 4);
                    // Bowstring
                    DrawLine(tex, cx - 45, 130, cx + 8, 250, Color.white, 2);
                    DrawLine(tex, cx + 8, 250, cx - 45, 300, Color.white, 2);
                    // Arrow shaft
                    DrawLine(tex, cx + 8, 250, cx - 65, 225, ch.primaryColor, 3);
                    break;

                case SpeciesType.HumanSamurai: // Ronin Jin: Iaido Draw Pose
                    // Wide low crouch
                    FillRect(tex, cx - 36, 30, cx - 14, 115, ch.secondaryColor);
                    FillRect(tex, cx + 12, 30, cx + 34, 115, ch.secondaryColor);
                    // Hakama robe skirt
                    FillTriangle(tex, new Vector2Int(cx - 30, 155), new Vector2Int(cx + 30, 155), new Vector2Int(cx - 42, 60), ch.secondaryColor);
                    FillTriangle(tex, new Vector2Int(cx - 30, 155), new Vector2Int(cx + 30, 155), new Vector2Int(cx + 42, 60), ch.secondaryColor);
                    // Haori Torso
                    FillEllipse(tex, cx, 175, 28, 45, ch.primaryColor);
                    FillRect(tex, cx - 16, 145, cx + 16, 158, outline); // Obi sash
                    // Head & Samurai Topknot
                    FillEllipse(tex, cx, 245, 18, 22, ch.fleshOrBaseColor);
                    FillEllipse(tex, cx, 256, 19, 14, outline); // Hair
                    FillCircle(tex, cx, 276, 8, outline); // Topknot
                    FillRect(tex, cx - 16, 248, cx + 16, 253, ch.accentColor); // Headband
                    // Arms: Left hand on scabbard, right hand drawing blade
                    DrawLine(tex, cx - 18, 180, cx - 30, 150, ch.primaryColor, 6); // Left arm
                    DrawLine(tex, cx + 18, 180, cx - 10, 160, ch.primaryColor, 6); // Right arm crossing
                    // Katana Scabbard at waist
                    DrawLine(tex, cx + 25, 130, cx - 35, 155, outline, 5);
                    // Gleaming silver katana blade partially unsheathed
                    DrawLine(tex, cx - 35, 155, cx - 75, 230, new Color(0.9f, 0.95f, 1f), 4);
                    DrawLine(tex, cx - 75, 230, cx - 60, 260, ch.accentColor, 2); // Sakura slash arc
                    break;

                case SpeciesType.HumanDuelist: // Kaelen Voss: Arcane Spellblade Flurry
                    // Elegant fencing stance
                    FillRect(tex, cx - 18, 30, cx - 6, 135, ch.secondaryColor);
                    FillRect(tex, cx + 10, 30, cx + 22, 135, ch.secondaryColor);
                    // Runic Coat
                    FillEllipse(tex, cx, 180, 26, 50, ch.primaryColor);
                    FillRect(tex, cx - 22, 110, cx + 22, 150, ch.primaryColor); // Coat tails
                    // Head with Spiky Silver Hair
                    FillEllipse(tex, cx, 255, 18, 22, ch.fleshOrBaseColor);
                    FillEllipse(tex, cx, 268, 22, 16, ch.accentColor); // Silver hair spikes
                    // Left blade pointed forward
                    DrawLine(tex, cx - 16, 190, cx - 48, 205, ch.primaryColor, 6);
                    DrawLine(tex, cx - 48, 205, cx - 95, 220, ch.accentColor, 4); // Glowing blade
                    // Right blade held raised reverse-grip
                    DrawLine(tex, cx + 16, 190, cx + 45, 225, ch.primaryColor, 6);
                    DrawLine(tex, cx + 45, 225, cx + 55, 280, ch.accentColor, 4);
                    // Arcane rune ring around character
                    for (int deg = 0; deg < 360; deg += 30)
                    {
                        float rad = deg * Mathf.Deg2Rad;
                        int rx = cx + (int)(Mathf.Cos(rad) * 65f);
                        int ry = 180 + (int)(Mathf.Sin(rad) * 45f);
                        FillCircle(tex, rx, ry, 3, ch.accentColor);
                    }
                    break;

                case SpeciesType.HumanPriestess: // Seraphina: Celestial Ascension
                    // Floating Robes
                    FillTriangle(tex, new Vector2Int(cx, 210), new Vector2Int(cx - 36, 45), new Vector2Int(cx + 36, 45), ch.primaryColor);
                    FillEllipse(tex, cx, 180, 22, 40, Color.white);
                    // Outstretched blessing arms
                    DrawLine(tex, cx - 14, 185, cx - 55, 205, Color.white, 6);
                    DrawLine(tex, cx + 14, 185, cx + 55, 205, Color.white, 6);
                    // Golden sacred orb in hands
                    FillCircle(tex, cx - 55, 205, 10, ch.accentColor);
                    FillCircle(tex, cx + 55, 205, 10, ch.accentColor);
                    // Serene Face
                    FillEllipse(tex, cx, 245, 17, 21, ch.fleshOrBaseColor);
                    FillEllipse(tex, cx, 255, 22, 16, ch.accentColor); // Blonde hair
                    // Glowing Celestial Halo floating above
                    FillCircle(tex, cx, 290, 20, ch.accentColor);
                    FillCircle(tex, cx, 290, 14, Color.clear);
                    break;

                case SpeciesType.HumanBrawler: // Ignis Rex: Flame Boxer Strike
                    // Dynamic brawler stance
                    FillRect(tex, cx - 26, 30, cx - 8, 130, ch.secondaryColor);
                    FillRect(tex, cx + 10, 30, cx + 28, 130, ch.secondaryColor);
                    // Muscular Chest / Brawler wraps
                    FillEllipse(tex, cx, 185, 30, 48, ch.fleshOrBaseColor);
                    FillRect(tex, cx - 20, 165, cx + 20, 180, outline); // Chest bandage
                    // Spiky Red Flame Hair & Scarred Face
                    FillEllipse(tex, cx, 255, 19, 23, ch.fleshOrBaseColor);
                    FillTriangle(tex, new Vector2Int(cx - 15, 265), new Vector2Int(cx - 25, 295), new Vector2Int(cx, 275), ch.primaryColor);
                    FillTriangle(tex, new Vector2Int(cx, 270), new Vector2Int(cx, 305), new Vector2Int(cx + 15, 270), ch.accentColor);
                    FillTriangle(tex, new Vector2Int(cx + 10, 265), new Vector2Int(cx + 25, 295), new Vector2Int(cx + 20, 270), ch.primaryColor);
                    // Broadsword on back
                    DrawLine(tex, cx - 25, 120, cx + 35, 280, outline, 8);
                    DrawLine(tex, cx - 25, 120, cx + 35, 280, ch.secondaryColor, 5);
                    // Lead Fiery Fist Punching Forward
                    DrawLine(tex, cx - 18, 190, cx - 55, 195, ch.fleshOrBaseColor, 8);
                    FillCircle(tex, cx - 55, 195, 18, ch.primaryColor); // Fire burst
                    FillCircle(tex, cx - 55, 195, 10, ch.accentColor);
                    FillCircle(tex, cx - 55, 195, 5, Color.white);
                    break;

                case SpeciesType.BeastFox: // Selene Mist: Lunar Kitsune Nine-Tails
                    // Nine Fanned White Fox Tails
                    for (int t = -4; t <= 4; t++)
                    {
                        float angle = t * 14f * Mathf.Deg2Rad;
                        int tx = cx + (int)(Mathf.Sin(angle) * 75f);
                        int ty = 120 + (int)(Mathf.Cos(angle) * 65f);
                        FillEllipse(tex, tx, ty, 15, 35, ch.fleshOrBaseColor);
                        FillCircle(tex, tx, ty + 25, 8, ch.primaryColor); // Cyan tips
                    }
                    // Graceful Beast Body & Kimono
                    FillEllipse(tex, cx, 170, 22, 45, ch.primaryColor);
                    // Fox Head & Ears
                    FillEllipse(tex, cx, 245, 18, 22, ch.fleshOrBaseColor);
                    FillTriangle(tex, new Vector2Int(cx - 16, 255), new Vector2Int(cx - 18, 290), new Vector2Int(cx - 4, 260), ch.fleshOrBaseColor);
                    FillTriangle(tex, new Vector2Int(cx + 16, 255), new Vector2Int(cx + 18, 290), new Vector2Int(cx + 4, 260), ch.fleshOrBaseColor);
                    FillEllipse(tex, cx, 238, 9, 6, ch.primaryColor); // Snout
                    // Crescent Moon Staff
                    DrawLine(tex, cx + 35, 30, cx + 35, 290, ch.secondaryColor, 5);
                    FillCircle(tex, cx + 35, 290, 18, ch.accentColor);
                    FillCircle(tex, cx + 35, 290, 13, Color.clear);
                    break;

                case SpeciesType.BeastRhino: // Gorgar: Rhino Titan Charge
                    // Heavy Wide Stance
                    FillRect(tex, cx - 38, 30, cx - 12, 120, ch.secondaryColor);
                    FillRect(tex, cx + 12, 30, cx + 38, 120, ch.secondaryColor);
                    // Colossal Rock Torso
                    FillEllipse(tex, cx, 175, 42, 55, ch.primaryColor);
                    // Boulder Shoulders
                    FillCircle(tex, cx - 44, 210, 22, ch.fleshOrBaseColor);
                    FillCircle(tex, cx + 44, 210, 22, ch.fleshOrBaseColor);
                    // Heavy Rhino Head & Massive Horn
                    FillEllipse(tex, cx, 245, 26, 24, ch.fleshOrBaseColor);
                    FillTriangle(tex, new Vector2Int(cx - 12, 245), new Vector2Int(cx + 12, 245), new Vector2Int(cx, 310), ch.accentColor); // Giant Horn
                    break;

                case SpeciesType.BeastLeopard: // Tahlia Frost: Frost Panther Pounce
                    // Low Pounce Stance
                    FillRect(tex, cx - 30, 30, cx - 12, 110, ch.secondaryColor);
                    FillRect(tex, cx + 12, 30, cx + 30, 110, ch.secondaryColor);
                    // Arching spotted tail
                    DrawLine(tex, cx - 30, 110, cx - 65, 170, ch.fleshOrBaseColor, 7);
                    DrawLine(tex, cx - 65, 170, cx - 55, 230, ch.fleshOrBaseColor, 6);
                    // Muscular Leopard Torso with spots
                    FillEllipse(tex, cx, 165, 26, 45, ch.fleshOrBaseColor);
                    FillCircle(tex, cx - 8, 175, 3, ch.secondaryColor);
                    FillCircle(tex, cx + 10, 160, 3, ch.secondaryColor);
                    // Feline Head & Ears
                    FillEllipse(tex, cx, 240, 22, 22, ch.fleshOrBaseColor);
                    FillCircle(tex, cx - 16, 256, 7, ch.primaryColor); // Rounded cat ears
                    FillCircle(tex, cx + 16, 256, 7, ch.primaryColor);
                    FillCircle(tex, cx - 8, 244, 4, ch.accentColor); // Icy blue cat eyes
                    FillCircle(tex, cx + 8, 244, 4, ch.accentColor);
                    // Both Claws Forward with Ice Spikes
                    DrawLine(tex, cx - 18, 175, cx - 50, 175, ch.fleshOrBaseColor, 6);
                    DrawLine(tex, cx + 18, 175, cx + 50, 175, ch.fleshOrBaseColor, 6);
                    FillTriangle(tex, new Vector2Int(cx - 50, 180), new Vector2Int(cx - 50, 170), new Vector2Int(cx - 75, 175), ch.accentColor); // Ice claw
                    FillTriangle(tex, new Vector2Int(cx + 50, 180), new Vector2Int(cx + 50, 170), new Vector2Int(cx + 75, 175), ch.accentColor);
                    break;

                case SpeciesType.RobotAndroid: // Astrid Storm: Mecha Lightning Charge
                    // Sprinting / Charging stance
                    FillRect(tex, cx - 22, 30, cx - 8, 125, ch.secondaryColor);
                    FillRect(tex, cx + 12, 45, cx + 26, 140, ch.secondaryColor);
                    // Blue Thruster Flames at boots
                    FillTriangle(tex, new Vector2Int(cx - 24, 30), new Vector2Int(cx - 6, 30), new Vector2Int(cx - 15, 10), ch.accentColor);
                    // Titanium Torso Chassis
                    FillRect(tex, cx - 26, 145, cx + 26, 215, ch.fleshOrBaseColor);
                    FillCircle(tex, cx, 180, 12, ch.primaryColor); // Solar power core
                    FillCircle(tex, cx, 180, 7, Color.white);
                    // Android Head with Glowing LED Visor
                    FillRect(tex, cx - 18, 235, cx + 18, 270, ch.fleshOrBaseColor);
                    FillRect(tex, cx - 14, 250, cx + 14, 258, ch.accentColor); // Visor bar
                    // Electric Tesla Coils on Shoulders
                    FillCircle(tex, cx - 34, 210, 14, ch.primaryColor);
                    FillCircle(tex, cx + 34, 210, 14, ch.primaryColor);
                    // Lightning crackling from fists
                    DrawLine(tex, cx - 34, 210, cx - 65, 230, ch.accentColor, 4);
                    DrawLine(tex, cx + 34, 210, cx + 65, 230, ch.accentColor, 4);
                    DrawLine(tex, cx - 65, 230, cx - 85, 210, Color.white, 2);
                    DrawLine(tex, cx + 65, 230, cx + 85, 210, Color.white, 2);
                    break;

                case SpeciesType.RobotCyborg: // Darius Hex: Heavy War Cyborg Arm Cannon
                    // Broad Heavy Cybernetic Stance
                    FillRect(tex, cx - 32, 30, cx - 10, 130, ch.secondaryColor);
                    FillRect(tex, cx + 10, 30, cx + 32, 130, ch.secondaryColor);
                    // Reinforced Cyber Chassis
                    FillRect(tex, cx - 32, 145, cx + 32, 220, ch.secondaryColor);
                    FillRect(tex, cx - 28, 155, cx + 28, 210, ch.primaryColor);
                    // Cyborg Skull & Red Laser Eye
                    FillRect(tex, cx - 18, 240, cx + 18, 275, ch.fleshOrBaseColor);
                    FillCircle(tex, cx - 7, 258, 6, ch.accentColor); // Red laser optic
                    FillCircle(tex, cx - 7, 258, 2, Color.white);
                    DrawLine(tex, cx - 7, 258, cx - 105, 258, ch.accentColor, 2); // Targeting laser beam!
                    // Right Arm: Multi-Barrel Gatling / Energy Cannon
                    FillRect(tex, cx + 28, 170, cx + 75, 215, outline);
                    FillCircle(tex, cx + 75, 185, 5, ch.accentColor);
                    FillCircle(tex, cx + 75, 200, 5, ch.accentColor);
                    FillCircle(tex, cx + 75, 215, 5, ch.accentColor);
                    // Left Arm: Heavy Cybernetic Hydraulic Claw
                    DrawLine(tex, cx - 32, 200, cx - 60, 175, ch.secondaryColor, 8);
                    FillCircle(tex, cx - 60, 175, 12, ch.primaryColor);
                    break;

                case SpeciesType.RobotAutomaton: // Orion Sol: Levitating Solar Rings
                    // Hovering Golden Spherical Body
                    FillCircle(tex, cx, 190, 36, ch.primaryColor);
                    FillCircle(tex, cx, 190, 20, ch.accentColor); // Radiant core
                    FillCircle(tex, cx, 190, 10, Color.white);
                    // Spherical Head
                    FillCircle(tex, cx, 255, 22, ch.primaryColor);
                    FillCircle(tex, cx, 255, 9, ch.accentColor); // Single cyclops eye
                    // Concentric Orbital Rings
                    for (int deg = 0; deg < 360; deg += 10)
                    {
                        float rad = deg * Mathf.Deg2Rad;
                        int rx1 = cx + (int)(Mathf.Cos(rad) * 75f);
                        int ry1 = 190 + (int)(Mathf.Sin(rad) * 25f);
                        FillCircle(tex, rx1, ry1, 3, ch.accentColor);

                        int rx2 = cx + (int)(Mathf.Cos(rad) * 90f);
                        int ry2 = 190 + (int)(Mathf.Sin(rad) * 45f);
                        FillCircle(tex, rx2, ry2, 2, Color.white);
                    }
                    break;

                case SpeciesType.RobotNinja: // Vesper: Cyber Shinobi Cross-Dagger
                    // Low Shadow Landing Crouch
                    FillRect(tex, cx - 28, 30, cx - 10, 110, ch.secondaryColor);
                    FillRect(tex, cx + 10, 30, cx + 28, 110, ch.secondaryColor);
                    // Carbon Fiber Torso & Flowing Neon Scarf
                    FillEllipse(tex, cx, 160, 24, 42, ch.primaryColor);
                    DrawLine(tex, cx + 8, 195, cx + 65, 210, ch.accentColor, 5); // Scarf trail
                    DrawLine(tex, cx + 65, 210, cx + 95, 240, ch.accentColor, 4);
                    // Carbon Ninja Helmet & Slit Visor
                    FillRect(tex, cx - 16, 225, cx + 16, 258, outline);
                    FillRect(tex, cx - 12, 240, cx + 12, 244, ch.accentColor); // Cyan slit
                    // Crossed Vibro-Daggers in Front
                    DrawLine(tex, cx - 16, 175, cx - 45, 155, ch.primaryColor, 6);
                    DrawLine(tex, cx + 16, 175, cx + 45, 155, ch.primaryColor, 6);
                    DrawLine(tex, cx - 45, 155, cx + 25, 220, ch.accentColor, 3); // Dagger 1
                    DrawLine(tex, cx + 45, 155, cx - 25, 220, ch.accentColor, 3); // Dagger 2
                    break;

                case SpeciesType.MonsterDemon: // Nyxara: Void Demoness Hover
                    // Floating Shadow Tendril Lower Body
                    for (int t = -3; t <= 3; t++)
                    {
                        DrawLine(tex, cx + t * 8, 140, cx + t * 14, 30, ch.primaryColor, 4);
                    }
                    // Chitinous Demon Torso
                    FillEllipse(tex, cx, 175, 26, 45, ch.secondaryColor);
                    FillEllipse(tex, cx, 175, 20, 35, ch.primaryColor);
                    // Demonic Head & Massive Sweeping Horns
                    FillEllipse(tex, cx, 245, 18, 22, ch.fleshOrBaseColor);
                    FillTriangle(tex, new Vector2Int(cx - 14, 255), new Vector2Int(cx - 45, 310), new Vector2Int(cx - 4, 265), ch.primaryColor);
                    FillTriangle(tex, new Vector2Int(cx + 14, 255), new Vector2Int(cx + 45, 310), new Vector2Int(cx + 4, 265), ch.primaryColor);
                    // Glowing Void Eyes
                    FillRect(tex, cx - 8, 246, cx - 2, 250, ch.accentColor);
                    FillRect(tex, cx + 2, 246, cx + 8, 250, ch.accentColor);
                    // Void Wing Tendrils extending outward
                    DrawLine(tex, cx - 20, 200, cx - 80, 270, ch.primaryColor, 5);
                    DrawLine(tex, cx - 20, 185, cx - 90, 230, ch.accentColor, 4);
                    DrawLine(tex, cx + 20, 200, cx + 80, 270, ch.primaryColor, 5);
                    DrawLine(tex, cx + 20, 185, cx + 90, 230, ch.accentColor, 4);
                    break;

                case SpeciesType.MonsterGhoul: // Morvath: Toxic Plague Throw
                    // Hunchbacked Creature Stance
                    FillRect(tex, cx - 26, 30, cx - 8, 120, ch.secondaryColor);
                    FillRect(tex, cx + 8, 30, cx + 26, 120, ch.secondaryColor);
                    // Hunched Torso with Spines
                    FillEllipse(tex, cx - 6, 165, 30, 48, ch.fleshOrBaseColor);
                    FillTriangle(tex, new Vector2Int(cx - 18, 180), new Vector2Int(cx - 42, 215), new Vector2Int(cx - 12, 195), ch.primaryColor); // Spines
                    FillTriangle(tex, new Vector2Int(cx - 14, 150), new Vector2Int(cx - 36, 175), new Vector2Int(cx - 8, 165), ch.primaryColor);
                    // Plague Beak Mask / Head
                    FillEllipse(tex, cx + 8, 235, 18, 20, ch.secondaryColor);
                    FillTriangle(tex, new Vector2Int(cx + 12, 238), new Vector2Int(cx + 12, 226), new Vector2Int(cx + 38, 222), outline); // Beak
                    FillCircle(tex, cx + 4, 240, 4, ch.accentColor); // Toxic eye
                    // Raised Arm holding bubbling plague flask
                    DrawLine(tex, cx - 15, 180, cx - 45, 230, ch.fleshOrBaseColor, 6);
                    FillCircle(tex, cx - 45, 230, 14, ch.primaryColor); // Bubbling flask
                    FillCircle(tex, cx - 45, 230, 8, ch.accentColor);
                    break;

                default:
                    // Standard Fighting Guard
                    FillRect(tex, cx - 20, 30, cx - 8, 135, ch.secondaryColor);
                    FillRect(tex, cx + 8, 30, cx + 20, 135, ch.secondaryColor);
                    FillEllipse(tex, cx, 180, 26, 48, ch.primaryColor);
                    FillEllipse(tex, cx, 250, 18, 22, ch.fleshOrBaseColor);
                    break;
            }
        }

        #endregion

        #region Portrait Generation (128 x 128)

        private static void GenerateUniquePortrait(string path, CharArtDef ch)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            int cx = 64;
            int cy = 60;
            Color outline = new Color(0.06f, 0.08f, 0.12f, 1f);

            // 1. Background Frame
            for (int y = 0; y < size; y++)
            {
                float ny = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    // Frame
                    if (x < 4 || x > size - 5 || y < 4 || y > size - 5)
                    {
                        tex.SetPixel(x, y, outline);
                        continue;
                    }
                    if (x < 7 || x > size - 8 || y < 7 || y > size - 8)
                    {
                        tex.SetPixel(x, y, ch.primaryColor);
                        continue;
                    }

                    // Background with radial burst
                    float dx = ((float)x - cx) / 64f;
                    float dy = ((float)y - cy) / 64f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    Color bg = Color.Lerp(ch.secondaryColor, ch.primaryColor * 0.5f, ny);
                    if (r < 0.65f)
                    {
                        float glow = (0.65f - r) / 0.65f;
                        bg = Color.Lerp(bg, ch.accentColor, glow * 0.4f);
                    }
                    tex.SetPixel(x, y, bg);
                }
            }

            // 2. Collar & Shoulders
            FillEllipse(tex, cx, 16, 42, 22, ch.secondaryColor);
            FillEllipse(tex, cx, 20, 28, 16, ch.primaryColor);

            // 3. Face & Head Close-Up
            switch (ch.archetype)
            {
                case BodyArchetype.Human:
                    // Human Face
                    FillEllipse(tex, cx, cy, 26, 32, outline);
                    FillEllipse(tex, cx, cy, 24, 30, ch.fleshOrBaseColor);
                    // Hair / Helmet
                    if (ch.species == SpeciesType.HumanKnight)
                    {
                        // Knight Helmet with Visor
                        FillEllipse(tex, cx, cy + 10, 27, 22, ch.primaryColor);
                        FillRect(tex, cx - 16, cy - 4, cx + 16, cy + 1, outline);
                        FillRect(tex, cx - 14, cy - 3, cx + 14, cy, ch.accentColor);
                    }
                    else if (ch.species == SpeciesType.HumanPriestess)
                    {
                        // Priestess Halo floating overhead
                        FillCircle(tex, cx, cy + 42, 16, ch.accentColor);
                        FillCircle(tex, cx, cy + 42, 12, ch.secondaryColor);
                        // Flowing hair
                        FillEllipse(tex, cx, cy + 12, 27, 20, ch.accentColor);
                        // Human Eyes
                        FillCircle(tex, cx - 10, cy, 4, ch.secondaryColor);
                        FillCircle(tex, cx + 10, cy, 4, ch.secondaryColor);
                    }
                    else if (ch.species == SpeciesType.HumanSamurai)
                    {
                        // Samurai Hair & Forehead Band
                        FillEllipse(tex, cx, cy + 14, 26, 18, ch.secondaryColor);
                        FillCircle(tex, cx, cy + 32, 9, ch.secondaryColor); // Topknot
                        FillRect(tex, cx - 22, cy + 4, cx + 22, cy + 10, ch.primaryColor);
                        // Eyes
                        FillRect(tex, cx - 12, cy - 2, cx - 6, cy + 1, outline);
                        FillRect(tex, cx + 6, cy - 2, cx + 12, cy + 1, outline);
                    }
                    else
                    {
                        // Styled Human Hair
                        FillEllipse(tex, cx, cy + 12, 28, 20, ch.accentColor);
                        FillCircle(tex, cx - 12, cy + 2, 6, ch.accentColor);
                        FillCircle(tex, cx + 12, cy + 2, 6, ch.accentColor);
                        // Eyes
                        FillCircle(tex, cx - 10, cy, 4, outline);
                        FillCircle(tex, cx + 10, cy, 4, outline);
                        FillCircle(tex, cx - 10, cy, 2, ch.primaryColor);
                        FillCircle(tex, cx + 10, cy, 2, ch.primaryColor);
                    }
                    break;

                case BodyArchetype.Beast:
                    // Beast Head
                    FillEllipse(tex, cx, cy, 30, 30, outline);
                    FillEllipse(tex, cx, cy, 28, 28, ch.fleshOrBaseColor);

                    // Beast Ears
                    if (ch.species == SpeciesType.BeastBear)
                    {
                        FillCircle(tex, cx - 24, cy + 22, 10, ch.fleshOrBaseColor);
                        FillCircle(tex, cx + 24, cy + 22, 10, ch.fleshOrBaseColor);
                        FillCircle(tex, cx - 24, cy + 22, 5, ch.secondaryColor);
                        FillCircle(tex, cx + 24, cy + 22, 5, ch.secondaryColor);
                    }
                    else if (ch.species == SpeciesType.BeastFalcon)
                    {
                        // Raptor Crest
                        FillTriangle(tex, new Vector2Int(cx - 20, cy + 10), new Vector2Int(cx - 34, cy + 36), new Vector2Int(cx - 6, cy + 18), ch.accentColor);
                        FillTriangle(tex, new Vector2Int(cx + 20, cy + 10), new Vector2Int(cx + 34, cy + 36), new Vector2Int(cx + 6, cy + 18), ch.accentColor);
                    }
                    else
                    {
                        // Pointed Wolf / Fox / Leopard Ears
                        FillTriangle(tex, new Vector2Int(cx - 24, cy + 10), new Vector2Int(cx - 22, cy + 42), new Vector2Int(cx - 8, cy + 18), ch.primaryColor);
                        FillTriangle(tex, new Vector2Int(cx + 24, cy + 10), new Vector2Int(cx + 22, cy + 42), new Vector2Int(cx + 8, cy + 18), ch.primaryColor);
                        FillTriangle(tex, new Vector2Int(cx - 21, cy + 14), new Vector2Int(cx - 19, cy + 36), new Vector2Int(cx - 10, cy + 19), ch.secondaryColor);
                        FillTriangle(tex, new Vector2Int(cx + 21, cy + 14), new Vector2Int(cx + 19, cy + 36), new Vector2Int(cx + 10, cy + 19), ch.secondaryColor);
                    }

                    // Muzzle / Snout / Beak
                    if (ch.species == SpeciesType.BeastFalcon)
                    {
                        // Downward Curved Beak
                        FillTriangle(tex, new Vector2Int(cx - 10, cy - 2), new Vector2Int(cx + 10, cy - 2), new Vector2Int(cx, cy - 20), ch.accentColor);
                    }
                    else if (ch.species == SpeciesType.BeastRhino)
                    {
                        // Heavy Rhino Snout + Upward Horn
                        FillEllipse(tex, cx, cy - 8, 16, 12, ch.secondaryColor);
                        FillTriangle(tex, new Vector2Int(cx - 8, cy - 6), new Vector2Int(cx + 8, cy - 6), new Vector2Int(cx, cy + 24), ch.accentColor);
                    }
                    else
                    {
                        // Wolf / Cat / Bear Muzzle
                        FillEllipse(tex, cx, cy - 8, 16, 10, ch.primaryColor);
                        FillCircle(tex, cx, cy - 4, 4, outline); // Nose pad
                        // Sharp Fangs
                        FillTriangle(tex, new Vector2Int(cx - 9, cy - 12), new Vector2Int(cx - 5, cy - 12), new Vector2Int(cx - 7, cy - 20), Color.white);
                        FillTriangle(tex, new Vector2Int(cx + 5, cy - 12), new Vector2Int(cx + 9, cy - 12), new Vector2Int(cx + 7, cy - 20), Color.white);
                    }

                    // Glowing Feral Predator Eyes
                    FillCircle(tex, cx - 11, cy + 6, 4, ch.accentColor);
                    FillCircle(tex, cx + 11, cy + 6, 4, ch.accentColor);
                    FillRect(tex, cx - 12, cy + 5, cx - 10, cy + 7, outline); // Slit pupil
                    FillRect(tex, cx + 10, cy + 5, cx + 12, cy + 7, outline);
                    break;

                case BodyArchetype.Robot:
                    // Titanium Robot Headplate
                    FillRect(tex, cx - 26, cy - 22, cx + 26, cy + 22, outline);
                    FillRect(tex, cx - 23, cy - 19, cx + 23, cy + 19, ch.fleshOrBaseColor);
                    // Head Antennae / Sensors
                    FillRect(tex, cx - 31, cy - 4, cx - 23, cy + 14, ch.secondaryColor);
                    FillRect(tex, cx + 23, cy - 4, cx + 31, cy + 14, ch.secondaryColor);

                    if (ch.species == SpeciesType.RobotCyborg)
                    {
                        // War Cyborg: Half-metal skull with infrared laser lens
                        FillRect(tex, cx - 23, cy - 19, cx, cy + 19, ch.primaryColor);
                        FillCircle(tex, cx - 11, cy + 3, 7, ch.accentColor);
                        FillCircle(tex, cx - 11, cy + 3, 3, Color.white);
                    }
                    else if (ch.species == SpeciesType.RobotAutomaton)
                    {
                        // Automaton: Core eye aperture
                        FillCircle(tex, cx, cy + 3, 11, ch.accentColor);
                        FillCircle(tex, cx, cy + 3, 5, Color.white);
                    }
                    else
                    {
                        // Android / Cyber Ninja: Glowing LED Visor
                        FillRect(tex, cx - 19, cy, cx + 19, cy + 8, outline);
                        FillRect(tex, cx - 17, cy + 2, cx + 17, cy + 6, ch.accentColor);
                    }
                    break;

                case BodyArchetype.Demon:
                    // Demonic Head
                    FillEllipse(tex, cx, cy, 26, 30, outline);
                    FillEllipse(tex, cx, cy, 24, 28, ch.fleshOrBaseColor);
                    // Large Sweeping Horns
                    FillTriangle(tex, new Vector2Int(cx - 16, cy + 8), new Vector2Int(cx - 40, cy + 48), new Vector2Int(cx - 4, cy + 20), ch.primaryColor);
                    FillTriangle(tex, new Vector2Int(cx + 16, cy + 8), new Vector2Int(cx + 40, cy + 48), new Vector2Int(cx + 4, cy + 20), ch.primaryColor);
                    // Glowing Void Eyes
                    FillRect(tex, cx - 12, cy + 3, cx - 4, cy + 7, ch.accentColor);
                    FillRect(tex, cx + 4, cy + 3, cx + 12, cy + 7, ch.accentColor);
                    // Fangs
                    FillRect(tex, cx - 8, cy - 12, cx - 5, cy - 7, Color.white);
                    FillRect(tex, cx + 5, cy - 12, cx + 8, cy - 7, Color.white);
                    break;
            }

            tex.Apply();
            SaveAndImportSprite(path, tex);
        }

        #endregion

        #region In-Game 2D Gameplay Sprite (64 x 96)

        private static void GenerateUniqueGameplaySprite(string path, CharArtDef ch)
        {
            int w = 64;
            int h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            // Transparent background
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }

            int cx = 32;
            Color outline = new Color(0.06f, 0.08f, 0.12f, 1f);

            // 1. Legs (Stance)
            int legW = (ch.archetype == BodyArchetype.Beast || ch.archetype == BodyArchetype.Robot) ? 7 : 5;
            int leftLeg = cx - 9;
            int rightLeg = cx + 3;
            FillRect(tex, leftLeg, 10, leftLeg + legW, 40, ch.secondaryColor);
            FillRect(tex, rightLeg, 10, rightLeg + legW, 40, ch.secondaryColor);
            // Feet / Boots
            FillRect(tex, leftLeg - 1, 10, leftLeg + legW + 2, 18, ch.primaryColor);
            FillRect(tex, rightLeg - 1, 10, rightLeg + legW + 2, 18, ch.primaryColor);

            // Beast Tail
            if (ch.archetype == BodyArchetype.Beast)
            {
                FillEllipse(tex, cx - 14, 30, 4, 12, ch.fleshOrBaseColor);
            }

            // 2. Torso
            int torsoW = (ch.archetype == BodyArchetype.Robot || ch.archetype == BodyArchetype.Beast) ? 12 : 9;
            FillEllipse(tex, cx, 52, torsoW, 14, ch.primaryColor);

            // Core / Accent on Torso
            if (ch.archetype == BodyArchetype.Robot)
            {
                FillCircle(tex, cx, 52, 3, ch.accentColor); // Reactor core
            }
            else
            {
                FillRect(tex, cx - 4, 44, cx + 4, 47, ch.accentColor); // Belt
            }

            // 3. Arms
            FillRect(tex, cx - torsoW - 4, 42, cx - torsoW, 60, ch.secondaryColor);
            FillRect(tex, cx + torsoW, 42, cx + torsoW + 4, 60, ch.secondaryColor);

            // 4. Head & Face Silhouette
            int headY = 74;
            switch (ch.archetype)
            {
                case BodyArchetype.Human:
                    FillCircle(tex, cx, headY, 7, ch.fleshOrBaseColor);
                    // Hair / Helmet
                    FillEllipse(tex, cx, headY + 3, 8, 5, ch.accentColor);
                    // Eye pixels
                    SetPixelSafe(tex, cx - 3, headY, outline);
                    SetPixelSafe(tex, cx + 3, headY, outline);
                    break;

                case BodyArchetype.Beast:
                    FillCircle(tex, cx, headY, 8, ch.fleshOrBaseColor);
                    // Ears
                    SetPixelSafe(tex, cx - 6, headY + 7, ch.primaryColor);
                    SetPixelSafe(tex, cx - 5, headY + 8, ch.primaryColor);
                    SetPixelSafe(tex, cx + 5, headY + 8, ch.primaryColor);
                    SetPixelSafe(tex, cx + 6, headY + 7, ch.primaryColor);
                    // Muzzle / Beak
                    FillRect(tex, cx - 3, headY - 4, cx + 3, headY - 1, ch.primaryColor);
                    // Eyes
                    SetPixelSafe(tex, cx - 3, headY + 1, ch.accentColor);
                    SetPixelSafe(tex, cx + 3, headY + 1, ch.accentColor);
                    break;

                case BodyArchetype.Robot:
                    FillRect(tex, cx - 7, headY - 6, cx + 7, headY + 6, ch.fleshOrBaseColor);
                    // Visor
                    FillRect(tex, cx - 5, headY, cx + 5, headY + 2, ch.accentColor);
                    break;

                case BodyArchetype.Demon:
                    FillCircle(tex, cx, headY, 7, ch.fleshOrBaseColor);
                    // Horns
                    SetPixelSafe(tex, cx - 6, headY + 6, ch.primaryColor);
                    SetPixelSafe(tex, cx - 8, headY + 10, ch.primaryColor);
                    SetPixelSafe(tex, cx + 6, headY + 6, ch.primaryColor);
                    SetPixelSafe(tex, cx + 8, headY + 10, ch.primaryColor);
                    // Eyes
                    SetPixelSafe(tex, cx - 3, headY, ch.accentColor);
                    SetPixelSafe(tex, cx + 3, headY, ch.accentColor);
                    break;
            }

            // Weapon accent
            FillRect(tex, cx + torsoW + 3, 40, cx + torsoW + 6, 75, ch.accentColor);

            tex.Apply();
            SaveAndImportSprite(path, tex);
        }

        #endregion

        #region Ability Icon Generation (64 x 64)

        private static void GenerateUniqueAbilityIcon(string path, CharArtDef ch, AbilityType type)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                float ny = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size;

                    // Border
                    if (x < 3 || x > size - 4 || y < 3 || y > size - 4)
                    {
                        tex.SetPixel(x, y, new Color(0.08f, 0.10f, 0.14f, 1f));
                        continue;
                    }

                    float dx = (nx - 0.5f) * 2f;
                    float dy = (ny - 0.5f) * 2f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    Color bg = ch.secondaryColor;

                    // Shape according to ability type
                    bool inShape = false;
                    switch (type)
                    {
                        case AbilityType.Primary:
                            // Slash / triangle
                            inShape = (Mathf.Abs(dx - dy) < 0.25f && r < 0.7f);
                            break;
                        case AbilityType.Skill:
                            // Diamond
                            inShape = ((Mathf.Abs(dx) + Mathf.Abs(dy)) < 0.65f);
                            break;
                        case AbilityType.Ultimate:
                            // Star / radiant burst
                            inShape = (r < 0.40f || Mathf.Abs(dx * dy) < 0.05f && r < 0.75f);
                            break;
                        case AbilityType.Passive:
                            // Shield / circle
                            inShape = (r < 0.55f);
                            break;
                    }

                    if (inShape)
                    {
                        tex.SetPixel(x, y, ch.primaryColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, bg);
                    }
                }
            }

            tex.Apply();
            SaveAndImportSprite(path, tex);
        }

        #endregion

        private static GameObject CreateCharacter2DPrefab(string prefabPath, string charName, Sprite sprite)
        {
            GameObject root = new GameObject($"Character2D_{charName}");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 0;

            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.64f, 0.96f);

            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            return savedPrefab;
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
