using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Harmony;
using System.Reflection;
using System.Collections;
using System.Net.Sockets;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using Random = System.Random;
using Valve.Newtonsoft.Json;

namespace OvercloudRandomWeather
{
    class SkyStuffs : MonoBehaviour
    {
        public static Random rand = new Random();

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
            }
        }


        public static void LightningUpdate(GameObject lightning, GameObject vehicle)
        {
            VTOLVehicles whatAmI = VTOLAPI.GetPlayersVehicleEnum();
            ModuleEngine[] engines = vehicle.GetComponentsInChildren<ModuleEngine>();
            Battery battery = vehicle.GetComponentInChildren<Battery>();

            SkyStuffs skyStuffs = (new GameObject("LightningUpdate")).AddComponent<SkyStuffs>();

            float distance = Vector2.Distance(new Vector2(lightning.transform.position.x, lightning.transform.position.z), new Vector2(vehicle.transform.position.x, vehicle.transform.position.z));

            if (distance <= 50f && (OC.OverCloud.IsInsideCloudVolume(vehicle.transform.position) || OC.OverCloud.IsBelowCloudVolume(vehicle.transform.position)))
            {
                skyStuffs.Events(vehicle, engines, battery, whatAmI);
            }
        }

        public void Events(GameObject vehicle, ModuleEngine[] engines, Battery battery, VTOLVehicles whatAmI)
        {
            StartCoroutine(LightningStriked(vehicle, engines, battery, whatAmI));
        }

        public IEnumerator LightningStriked(GameObject vehicle, ModuleEngine[] engines, Battery battery, VTOLVehicles whatAmI)
        {
            StartCoroutine(AlternatorBoost(engines, whatAmI, 2));
            battery.Drain(battery.maxCharge * 0.99f);

            FuelTank fuelTank = vehicle.GetComponentInChildren<FuelTank>();
            double range = 0.1 - 0.05;
            fuelTank.RequestFuel(fuelTank.fuel * ((rand.NextDouble() * range) - 0.02));

            switch (whatAmI)
            {
                case VTOLVehicles.AV42C:
                case VTOLVehicles.FA26B:

                    foreach (ModuleEngine engine in engines)
                    {
                        if (rand.Next(1, 20) == 1)
                        {
                            engine.FailEngine();
                        }
                    }

                    break;
                case VTOLVehicles.F45A:
                    foreach (ModuleEngine engine in engines)
                    {
                        if (rand.Next(1, 5) == 1)
                        {
                            engine.FailEngine();
                        }
                    }

                    break;
            }
            yield return new WaitForSeconds(3f);

            foreach (ModuleEngine engine in engines)
            {
                engine.FullyRepairEngine();
            }

            yield return new WaitForSeconds(35f);
            StopCoroutine(AlternatorBoost(engines, whatAmI, 2));

            foreach (ModuleEngine engine in engines)
            {
                engine.alternatorChargeRate = 450f;
            }
        }

        public IEnumerator AlternatorBoost(ModuleEngine[] engines, VTOLVehicles whatAmI, float boostage)
        {
            switch (whatAmI)
            {
                case VTOLVehicles.AV42C:
                case VTOLVehicles.FA26B:

                    foreach (ModuleEngine engine in engines)
                    {
                        engine.alternatorChargeRate = boostage;
                    }

                    break;
                case VTOLVehicles.F45A:

                    foreach (ModuleEngine engine in engines)
                    {

                        engine.alternatorChargeRate = boostage * 0.001f;
                    }

                    break;
            }

            yield return new WaitForSeconds(0.01f);

            if (whatAmI != VTOLVehicles.F45A)
            {
                boostage *= 1.004f;
            }
            else
                boostage *= 1.013f;
            StartCoroutine(AlternatorBoost(engines, whatAmI, boostage));
        }
    }
}
