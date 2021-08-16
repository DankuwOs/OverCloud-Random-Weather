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
    class SkyStuffs
    {

        public static void DisableNGSS_Directional() // If you've ever used OverCloud and wondered "Why is there 2 different shadows?" this is why.
        {
            GameSettings.TryGetGameSettingValue<bool>("USE_OVERCLOUD", out bool isOvercloudEnabled);
            Light ngss_Directional = GameObject.Find("Directional Light").GetComponent<Light>();
            if (isOvercloudEnabled == true) // I realize now I could do this in Main, but I don't want to break anything..
            {
                ngss_Directional.intensity = 0f;
            }
            else
            {
                if (VTScenario.current.selectableEnv == true && Main.currentEnv != "night")
                {
                    ngss_Directional.intensity = 1f;
                }
                if (VTScenario.current.selectableEnv == false && PilotSaveManager.currentScenario.environmentName != "night")
                {
                    ngss_Directional.intensity = 1f;
                }
                if (VTScenario.current.selectableEnv == true && Main.currentEnv == "night")
                {
                    ngss_Directional.intensity = 0.26f;
                }
                if (VTScenario.current.selectableEnv == false && PilotSaveManager.currentScenario.environmentName == "night")
                {
                    ngss_Directional.intensity = 0.26f;
                }
            }
        }

        public static void EnableDynamicTimeOfDay()
        {
            OC.OverCloud.timeOfDay.enable = true;
            OC.OverCloud.timeOfDay.playSpeed = 1000f;
            OC.OverCloud.timeOfDay.Advance();
        }

        public static void UpdateCloudSettings()
        {
            OC.OverCloud.volumetricClouds.cloudPlaneRadius = Main.settings.volumetricCloudRadius;
            OC.OverCloud.volumetricClouds.particleCount = (int)Main.settings.particleCount;
        }

        public static void SunPositionInSky()
        {
            /* ARCTIC: OC.OverCloud.timeOfDay.latitude = 89f;
             *         OC.OverCloud.timeOfDay.longitude = 20f;
             *         
             * BOREAL: OC.OverCloud.timeOfDay.latitude = 62f;
             *         OC.OverCloud.timeOfDay.longitude = -3f;
             *         
             * DESERT: OC.OverCloud.timeOfDay.latitude = 28f;
             *         OC.OverCloud.timeOfDay.longitude = 23f;
             *
             * TROPICAL: OC.OverCloud.timeOfDay.latitude = 1f;
             *           OC.OverCloud.timeOfDay.longitude = 20f;
             *
            */

            switch (Main.currentBiome) // Longitude isn't in the real life position of the location because time zones n stuff? Morning has to be morning, day has to be day. At least somewhat, arctic is hard.
            {
                case "Boreal":
                    OC.OverCloud.timeOfDay.latitude = 62f;
                    OC.OverCloud.timeOfDay.longitude = 28f;

                    break;
                case "Desert":
                    OC.OverCloud.timeOfDay.latitude = 28f;
                    OC.OverCloud.timeOfDay.longitude = 23f;

                    break;
                case "Arctic":
                    OC.OverCloud.timeOfDay.latitude = 89f;
                    OC.OverCloud.timeOfDay.longitude = 20f;

                    break;
                case "Tropical":
                    OC.OverCloud.timeOfDay.latitude = 1f;
                    OC.OverCloud.timeOfDay.longitude = 20f;

                    break;
            }
        }
    }
}
