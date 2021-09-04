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
}
