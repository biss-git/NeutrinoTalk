using System;
using System.Collections.Generic;
using System.Text;
using Yomiage.SDK.Settings;
using Yomiage.SDK.Talk;
using Yomiage.SDK.VoiceEffects;

namespace NeutrinoTalk
{
    internal static class SettingsUtil
    {
        public static string GetBaseFolder(this SettingsBase settings)
        {
            var key = "NuetrinoFolder";
            if (settings.Strings.ContainsKey(key) &&
                settings.Strings.TryGetSetting(key, out var setting)){
                return setting.Value;
            }
            return string.Empty;
        }

        public static string GetSyntheType(this SettingsBase settings)
        {
            var key = "SyntheType";
            if (settings.Strings.ContainsKey(key) &&
                settings.Strings.TryGetSetting(key, out var setting))
            {
                return setting.Value;
            }
            return "WORLD";
        }
        
        public static string GetModelName(this SettingsBase settings)
        {
            var key = "ModelName";
            if (settings.Strings.ContainsKey(key) &&
                settings.Strings.TryGetSetting(key, out var setting))
            {
                return setting.Value;
            }
            return "MERROW";
        }

        public static int GetTempo(this VoiceEffectValue effect)
        {
            return effect.Speed != null ? ((int)effect.Speed.Value) : 100;
        }

        public static double GetSpeed(this MasterEffectValue effect)
        {
            return effect.Speed != null ? effect.Speed.Value : 1;
        }

        public static double GetVolume(this VoiceEffectValueBase effect)
        {
            return effect.Volume != null ? (effect.Volume.Value) : 1;
        }

        public static int GetKeyShift(this VoiceEffectValue effect)
        {
            return effect.Pitch != null ? ((int)effect.Pitch.Value) : 100;
        }

        public static int GetDuration(this Mora mora)
        {
            if(mora?.Speed != null)
            {
                return (int)Math.Round(mora.Speed.Value);
            }
            return 2;
        }

        public static int GetPitch(this Mora mora)
        {
            if (mora?.Pitch != null)
            {
                return (int)Math.Round(mora.Pitch.Value);
            }
            return 0;
        }

        public static bool GetBreath(this Mora mora)
        {
            if (mora?.Emphasis != null)
            {
                return mora.Emphasis.Value != 0;
            }
            return false;
        }

    }
}
