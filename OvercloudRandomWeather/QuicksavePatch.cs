using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.IO;
using Valve.Newtonsoft.Json;
using Harmony;
using TMPro;

namespace OvercloudRandomWeather
{
    [HarmonyPatch(typeof(QuicksaveManager), "Quicksave")]
    public static class QuicksavePatch0
    {
        public static void Postfix()
        {
            Main.quickSavedTimeOfDay = OC.OverCloud.timeOfDay.time;
            Debug.Log("Saved time of day to " + Main.quickSavedTimeOfDay + ". OCRW");
        }
    }
}
