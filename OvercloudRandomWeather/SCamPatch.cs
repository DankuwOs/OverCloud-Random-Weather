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
    [HarmonyPatch(typeof(FlybyCameraMFDPage), "EnableCamera")]
    public static class SCamPatch
    {
        public static void Postfix(FlybyCameraMFDPage __instance)
        {
            if (Main.settings.disableCloudsOnly == true)
            {
                Main.overCloudCamera1 = __instance.flybyCam.GetComponent<OC.OverCloudCamera>();
                OC.OverCloudCamera overCloudCamera = __instance.flybyCam.GetComponent<OC.OverCloudCamera>();
                overCloudCamera.render2DFallback = false;
                overCloudCamera.renderRainMask = false;
                overCloudCamera.renderScatteringMask = false;
                overCloudCamera.renderVolumetricClouds = false;
                overCloudCamera.renderAtmosphere = !Main.settings.disableAtmosphere;
            }
        }
    }

}
