using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Reflection;
using System.IO;
using Valve.Newtonsoft.Json;
using Harmony;
using TMPro;
using Rewired.Platforms;
using Rewired.Utils;
using Rewired.Utils.Interfaces;

namespace OvercloudRandomWeather
{
    [HarmonyPatch(typeof(EnvironmentManager), "SetCurrent")]
    public static class EnvironmentPatch3
    {
        public static bool Prefix(EnvironmentManager __instance)
        {
            if (Main.settings.useOvercloud == true && Main.settings.fixWater == true && PilotSaveManager.currentScenario.mapSceneName != "Akutan")
            {
                __instance.SetEnvironment("night");
            }
            else
            {
                __instance.SetEnvironment(__instance.currentEnvironment);
            }
            return false;
        }
    }
}
