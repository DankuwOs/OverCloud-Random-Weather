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
    [HarmonyPatch(typeof(VTMapGenerator), "Generate")]
    public static class BiomeStuffs
    {
        [HarmonyPrefix]
        public static void Postfix(VTMapGenerator __instance)
        {
            Main.currentBiome = __instance.biome.ToString();
        }
    }

}
