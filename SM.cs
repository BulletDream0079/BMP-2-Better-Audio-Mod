/*          
 *          
 *          \\ HUGE THANKS TO ATLAS/BENINATOR FOR THE METHOD FOR AUDIO MODDING //
 * 
 * 
 */

using System;
using System.IO;
using GHPC.Weapons;
using MelonLoader;
using UnityEngine;
using HarmonyLib;
using FMOD;
using FMODUnity;
using MelonLoader.Utils;
using GHPC.Audio;
using GHPC.Player;

[assembly: MelonInfo(typeof(betterAudio.Mod), "BMP-2 Better Audio", "1.0", "BulletDream0079")]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace betterAudio
{
    [HarmonyPatch(typeof(WeaponAudio), "FinalStartLoop")]
    public class ReplaceSound
    {
        public static FMOD.Sound sound_exterior;
        public static FMOD.Sound? interior_cannon_sound;
        public static FMOD.Sound[] interior_casing_sounds = new FMOD.Sound[4];
        public static bool hasExteriorSound = false;
        public static bool hasInteriorCannonSound = false;

        public static bool Prefix(WeaponAudio __instance)
        {
            if (__instance.LoopEventPath != "event:/Weapons/autocannon_2a42_600rpm")
            {
                return true;
            }

            var corSystem = RuntimeManager.CoreSystem;
            bool isInterior = __instance.IsInterior &&
                              __instance == Mod.player_manager.CurrentPlayerWeapon.Weapon.WeaponSound;

            FMOD.Sound soundToPlay;

            if (isInterior)
            {
                if (hasInteriorCannonSound)
                {
                    soundToPlay = interior_cannon_sound.Value;
                }
                else
                {
                    /* \\ DEBUG METRIC //
                    MelonLogger.Msg("No custom interior sound found, playing vanilla.");
                    */
                    return true;
                  }
              }
              else
              {
                  if (hasExteriorSound)
                  {
                      soundToPlay = sound_exterior;
                  }
                  else
                  {

                    /* \\ DEBUG METRIC //
                      MelonLogger.Msg("No custom exterior sound found, playing vanilla.");
                    */
                    return true;
                }
            }

            Vector3 vec = __instance.transform.position;
            VECTOR pos = new VECTOR { x = vec.x, y = vec.y, z = vec.z };
            VECTOR vel = new VECTOR { x = 0f, y = 0f, z = 0f };

            corSystem.createChannelGroup("master", out ChannelGroup channelGroup);
            channelGroup.setMode(MODE._3D_WORLDRELATIVE);

            corSystem.playSound(soundToPlay, channelGroup, true, out FMOD.Channel channel);

            float game_vol = Mod.audio_settings_manager._previousVolume;
            float gun_vol = isInterior
                ? (game_vol + 0.10f * (game_vol * 10))
                : (game_vol + 0.07f * (game_vol * 10));

            // custom modulation for a slightly randomized audio pitch to avoid audio fatigue

            float randomPitch = UnityEngine.Random.Range(0.95f, 1.05f);
            channel.setPitch(randomPitch);
            channel.setVolume(gun_vol);
            channel.set3DAttributes(ref pos, ref vel);
            channel.setPaused(false);

            if (isInterior)
            {
                int casingIdx = UnityEngine.Random.Range(0, interior_casing_sounds.Length);
                FMOD.Sound casingSound = interior_casing_sounds[casingIdx];
                corSystem.playSound(casingSound, channelGroup, true, out FMOD.Channel casingChannel);

                float pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                casingChannel.setPitch(pitch);
                casingChannel.set3DAttributes(ref pos, ref vel);
                casingChannel.setVolume(game_vol * 0.6f);
                casingChannel.setPaused(false);
            }

            /* \\ DEBUG METRIC //
                MelonLogger.Msg($"Played BMP-2 {(isInterior ? "interior" : "exterior")} shot with pitch: {randomPitch:F2}");
            */
            return false;
        }
    }

    public class Mod : MelonMod
    {
        private GameObject game_manager;
        public static AudioSettingsManager audio_settings_manager;
        public static PlayerInput player_manager;

        public override void OnInitializeMelon()
        {
            var corSystem = RuntimeManager.CoreSystem;
            string modPath = Path.Combine(MelonEnvironment.ModsDirectory, "bmp2BA");

            string cannonPath = Path.Combine(modPath, "Cannon_Int.wav");
            if (File.Exists(cannonPath))
            {
                corSystem.createSound(cannonPath, MODE._3D_INVERSETAPEREDROLLOFF, out var sound);
                sound.set3DMinMaxDistance(35f, 5000f);
                ReplaceSound.interior_cannon_sound = sound;
                ReplaceSound.hasInteriorCannonSound = true;
                /* \\ DEBUG METRIC //
            MelonLogger.Msg("Loaded custom interior cannon sound.");
            */
            }
            /* \\ DEBUG METRIC //
             else
            {
                MelonLogger.Warning("Custom interior cannon sound not found. Vanilla sound will be used.");
            }
            */

            for (int i = 0; i < 4; i++)
            {
                string file = Path.Combine(modPath, $"Cannon_Casing_Int_{i + 1}.wav");
                if (File.Exists(file))
                {
                    RESULT result = corSystem.createSound(file, MODE._3D_INVERSETAPEREDROLLOFF, out ReplaceSound.interior_casing_sounds[i]);
                    if (result == RESULT.OK)
                    {
                        ReplaceSound.interior_casing_sounds[i].set3DMinMaxDistance(1f, 30f); // Casings shouldn't be heard from far away
                        /* \\ DEBUG METRIC //
                        MelonLogger.Msg($"Loaded interior casing sound: {Path.GetFileName(file)}");
                        */
                    }
                }
            }

            // Load exterior sound
            string extPath = Path.Combine(modPath, "Cannon_Ext.wav");
            if (File.Exists(extPath))
            {
                corSystem.createSound(extPath, MODE._3D_INVERSETAPEREDROLLOFF, out ReplaceSound.sound_exterior);
                ReplaceSound.sound_exterior.set3DMinMaxDistance(35f, 5000f);
                ReplaceSound.hasExteriorSound = true;
                /* \\ DEBUG METRIC //
                MelonLogger.Msg("Loaded custom exterior sound.");
                */
            }
            else
            {
                ReplaceSound.hasExteriorSound = false;
                /* \\ DEBUG METRIC //
                MelonLogger.Msg("Custom exterior sound not found || REVERTING TO VANILLA");
              */
            }
        }

        public override void OnSceneWasLoaded(int idx, string scene_name)
        {
            if (scene_name == "MainMenu2_Scene" || scene_name == "LOADER_MENU" || scene_name == "LOADER_INITIAL" || scene_name == "t64_menu") return;
            game_manager = GameObject.Find("_APP_GHPC_");
            if (game_manager != null)
            {
                audio_settings_manager = game_manager.GetComponent<AudioSettingsManager>();
                player_manager = game_manager.GetComponent<PlayerInput>();
            }
        }
    }
}