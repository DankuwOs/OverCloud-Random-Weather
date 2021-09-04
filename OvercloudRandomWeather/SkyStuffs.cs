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
            Light[] ngss_Directional;
            ngss_Directional = GameObject.FindObjectsOfType(typeof(Light)) as Light[];
            foreach (Light _ngss_Directional in ngss_Directional)
            {
                if (_ngss_Directional.name.Contains("Directional"))
                {
                   _ngss_Directional.intensity = 0f;
                    Debug.Log("Set " + _ngss_Directional.name + " intensity to 0" + ". OCRW");
                }
            }
        }

        public static void EnableDynamicTimeOfDay()
        {
            OC.OverCloud.timeOfDay.enable = true;
            OC.OverCloud.timeOfDay.playSpeed = Main.settings.dynamicTimeOfDaySpeed;
            OC.OverCloud.timeOfDay.Advance();
            if (OC.OverCloud.timeOfDay.time <= 4.0)
            {
                Shader.SetGlobalFloat("_CityLightBrightness", 1f);
            }
            if (OC.OverCloud.timeOfDay.time >= 4.0)
            {
                Shader.SetGlobalFloat("_CityLightBrightness", 0f);
            }
            if (OC.OverCloud.timeOfDay.time >= 15.5)
            {
                Shader.SetGlobalFloat("_CityLightBrightness", 1f);
            }
        }

        public static void UpdateCloudSettings()
        {
            OC.OverCloud.volumetricClouds.cloudPlaneRadius = Main.settings.volumetricCloudRadius;
            OC.OverCloud.volumetricClouds.particleCount = (int)Main.settings.particleCount;
            Debug.Log("Set cloud settings to " + OC.OverCloud.volumetricClouds.cloudPlaneRadius + " & " + OC.OverCloud.volumetricClouds.particleCount + ". OCRW");
        }

        public static void SunPositionInSky()
        {
            /* ARCTIC: OC.OverCloud.timeOfDay.latitude = 83f;
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
                    OC.OverCloud.timeOfDay.latitude = 83f;
                    OC.OverCloud.timeOfDay.longitude = 20f;

                    break;
                case "Tropical": // Tropical seems kinda boring. Giving it a unique latitude.
                    OC.OverCloud.timeOfDay.latitude = -37f;
                    OC.OverCloud.timeOfDay.longitude = 20f;

                    break;
            }
        }
    }
}
