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
    [HarmonyPatch(typeof(VehicleConfigScenarioUI), "UpdateEnvText")]
    public static class EnvironmentPatch0
    {
        public static void Postfix(ref Text ___envSelectText)
        {
            CampaignScenario currentScenario = PilotSaveManager.currentScenario;

            ___envSelectText.text = VTLocalizationManager.GetString(string.Format("env_{0}", currentScenario.envOptions[currentScenario.envIdx].envLabel.ToLower()), currentScenario.envOptions[currentScenario.envIdx].envLabel.ToLower());

            // If this if statement is gone, when you launch the scenario, it'll be set to nothing which causes the loop to set use overcloud to false.
            // ME V2: I think this is useless, as I've basically got nighttime working. Only thing not right with it is the water, so this stays until then.
            if (___envSelectText.text.ToLower() == "day" || ___envSelectText.text.ToLower() == "morning" || ___envSelectText.text.ToLower() == "night")
            {
                Main.currentEnv = ___envSelectText.text.ToLower();
                Debug.Log("Current environment is " + Main.currentEnv + ". OCRW");
            }
        }
    }

    [HarmonyPatch(typeof(VTOverCloudTester), "Update")] // Changing the keybind for the debug menu because particle testing is a thing i think.
    public static class EnvironmentPatch1
    {
        public static void Postfix(ref bool ___showDebug)
        {
            if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.N))
            {
                ___showDebug = !___showDebug;
                Debug.Log("Toggled debug menu. OCRW");
            }
        }
    }

    [HarmonyPatch(typeof(EnvironmentManager), "SetEnvironment")] // Setting the OC time of day and turning useOverCloud off and on to use the current environments water.
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

            if (Main.quickSavedTimeOfDay != 25)
            {
                OC.OverCloud.timeOfDay.time = Main.quickSavedTimeOfDay;
                Debug.Log("Set OC time of day to " + OC.OverCloud.timeOfDay.time + " from " + Main.quickSavedTimeOfDay + ". OCRW");
            }
        }
    }

    [HarmonyPatch(typeof(EnvironmentManager), "SetCurrent")] // Setting the current environment to nighttime to use the night water instead.
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
