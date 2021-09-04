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
    [HarmonyPatch(typeof(EnvironmentManager), "SetEnvironment")]
    public static class EnvironmentPatch2
    {
        public static void Prefix(EnvironmentManager __instance)
        {
            if (Main.settings.useOvercloud == true && PilotSaveManager.currentScenario.mapSceneName != "Akutan")
            {
                if (VTScenario.currentScenarioInfo.selectableEnv == true)
                {
                    if (Main.currentEnv == "morning")
                    {
                        OC.OverCloud.timeOfDay.time = 5.0;
                    }
                    else
                    if (Main.currentEnv == "night")
                    {
                        OC.OverCloud.timeOfDay.time = 21.0;
                    }
                    else
                    OC.OverCloud.timeOfDay.time = 10.0;
                }
                else
                {
                    var getScenario = PilotSaveManager.currentScenario;
                    if (getScenario.environmentName == "morning")
                    {
                        OC.OverCloud.timeOfDay.time = 5.0;
                    }
                    else
                    if (getScenario.environmentName == "night")
                    {
                        OC.OverCloud.timeOfDay.time = 21.0;
                    }
                    else
                    {
                        OC.OverCloud.timeOfDay.time = 10.0;
                    }
                }
            }
            VTResources.useOverCloud = false;
        }
        public static void Postfix()
        {
            if (Main.settings.useOvercloud == true && PilotSaveManager.currentScenario.mapSceneName != "Akutan")
            VTResources.useOverCloud = true;
        }
    }
}
