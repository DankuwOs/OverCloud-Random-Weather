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
    [HarmonyPatch(typeof(VTOverCloudTester), "Update")]
    public static class EnvironmentPatch1
    {
        public static void Postfix(ref bool ___showDebug)
        {
            if(Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.N))
            {
                ___showDebug = !___showDebug;
            }
        }
    }
}
