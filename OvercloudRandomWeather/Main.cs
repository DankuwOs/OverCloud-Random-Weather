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
    public class OCsettings // Settings in the mod settings page
    {
        public bool useOvercloud = true;
        public bool disableCloudsOnly = false;
        public bool fixWater = false;
        public bool useOvercloudRandomWeather = true;
        public bool doNightTime = false;
        public int randomWeatherTimerLength = 800;
        public bool useDynamicTimeOfDay = false;
        public int dynamicTimeOfDaySpeed = 120;
        public bool doLatLong = false;
        public float volumetricCloudRadius = 6000f;
        public float particleCount = 4000f;
        public bool usePresetOnLoad = false;
        public int presetToUse = 2;
        public bool disableAtmosphere = false;
    }
    public class Main : VTOLMOD
    {
        public static OCsettings settings;
        public bool settingsChanged;
        public static string currentEnv; // Current environment (day, night, morning)
        public static string currentBiome; // Current biome, for the suns position in the sky
        public static string envName; // Env name for water timer.
        public static string[] weatherPresets = {
                        "Clear", "Broken", "Overcast", "Foggy", "Rain", "Storm"
                        };
        public static Light ngss_Directional;
        public static bool sceneLoaded = false;
        public static bool isAkutan;

        public static bool runTimer = false;
        private static Timer rwTimer; // Random weather timer
        private static Timer ocTimer; // Timer that does the Overcloud stuffs
        private static Timer ocTimerSelect; // Timer that does the Overcloud stuffs if selectableEnv = false
        public static int timerInt;

        public static string weatherName;
        public static double weatherForecastLength;
        public static string weatherForecastTime;

        public UnityAction<bool> overcloud_changed;
        public UnityAction<bool> disableCloudsOnly_changed;
        public UnityAction<bool> fixWater_changed;
        public UnityAction<bool> overcloudRandomWeather_changed;
        public UnityAction<bool> doNightTime_changed;
        public UnityAction<int> randomWeatherTimerLength_changed;
        public UnityAction<bool> dynamicTimeOfDay_changed;
        public UnityAction<int> dynamicTimeOfDaySpeed_changed;
        public UnityAction<bool> doLatLong_changed;
        public UnityAction<float> volumetricCloudRadius_changed;
        public UnityAction<float> particleCount_changed;
        public UnityAction<bool> usePresetOnLoad_changed;
        public UnityAction<int> presetToUse_changed;
        public UnityAction<bool> disableAtmosphere_changed;


        public override void ModLoaded()
        {

            HarmonyInstance harmonyInstance = HarmonyInstance.Create("dankuwos.overcloud"); // If you're copying my code, change "dankuwos.overcloud"
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());



            base.ModLoaded();

            VTOLAPI.SceneLoaded += SceneLoaded;
            VTOLAPI.MissionReloaded += MissionReloaded;

            // For these settings, you should probably have them in this order below base.ModLoaded() because I ran into issues with saving n stuffs.
            settings = new OCsettings();
            LoadFromFile();

            Settings modSettings = new Settings(this);

            modSettings.CreateCustomLabel("OVERCLOUD SETTINGS");
            modSettings.CreateCustomLabel(" ");

            overcloud_changed += Overcloud_Setting;
            modSettings.CreateBoolSetting("  Overcloud | Default: True", overcloud_changed, settings.useOvercloud);

            modSettings.CreateCustomLabel(" ");

            disableCloudsOnly_changed += DisableCloudsOnly_Setting;
            modSettings.CreateBoolSetting("  Disable Clouds Only | Default: False", disableCloudsOnly_changed, settings.disableCloudsOnly);


            modSettings.CreateCustomLabel(" ");

            fixWater_changed += FixWater_Setting;
            modSettings.CreateCustomLabel("  Fixes the brightness of the water at night, this comes at the cost of darker ");
            modSettings.CreateCustomLabel("  water in the day. (sorry guys -Eyeron)");
            modSettings.CreateBoolSetting("  Fix the Water | Default: False", fixWater_changed, settings.fixWater);


            modSettings.CreateCustomLabel("");

            overcloudRandomWeather_changed += OvercloudRandomWeather_Setting;
            modSettings.CreateBoolSetting("  Overcloud Random Weather | Default: True", overcloudRandomWeather_changed, settings.useOvercloudRandomWeather);


            modSettings.CreateCustomLabel("");

            doNightTime_changed += DoNightTime_Setting;
            modSettings.CreateCustomLabel("  Allow night time? Water is really bright if you go from day to night.");
            modSettings.CreateBoolSetting("  Night Time | Default: False", doNightTime_changed, settings.doNightTime);


            modSettings.CreateCustomLabel("");


            randomWeatherTimerLength_changed += RandomWeatherTimerLength_Setting;
            modSettings.CreateCustomLabel("  Random Weather Timer Length | Default: 800");
            modSettings.CreateIntSetting("  (in seconds)", randomWeatherTimerLength_changed, settings.randomWeatherTimerLength);


            modSettings.CreateCustomLabel("");

            dynamicTimeOfDay_changed += DynamicTimeOfDay_Setting;
            modSettings.CreateBoolSetting("  Dynamic Time Of Day | Default: False", dynamicTimeOfDay_changed, settings.useDynamicTimeOfDay);


            modSettings.CreateCustomLabel("");


            dynamicTimeOfDaySpeed_changed += DynamicTimeOfDaySpeed_Setting;
            modSettings.CreateCustomLabel("  Dynamic Time Of Day Speed | Default: 120 | 1 Is realtime,");
            modSettings.CreateIntSetting("  60 is 1 minute every second", dynamicTimeOfDaySpeed_changed, settings.dynamicTimeOfDaySpeed);

            modSettings.CreateCustomLabel("");

            doLatLong_changed += DoLatLong_Setting;
            modSettings.CreateCustomLabel("  Use latitude / longitude of the biome of the map you're on?");
            modSettings.CreateBoolSetting("  Use Lat / Long | Default: False", doLatLong_changed, settings.doLatLong);


            modSettings.CreateCustomLabel("");


            volumetricCloudRadius_changed += VolumetricCloudRadius_Setting;
            modSettings.CreateCustomLabel("  Volumetric Cloud Radius | Default: 6000 | After this radius the clouds");
            modSettings.CreateCustomLabel("  become 2D, when adjusting this you should also change Particle Count");
            modSettings.CreateFloatSetting("  Cloud Radius | Min: 0 Max: 64000", volumetricCloudRadius_changed, settings.volumetricCloudRadius, 0, 64000);


            modSettings.CreateCustomLabel("");


            particleCount_changed += ParticleCount_Setting;
            modSettings.CreateCustomLabel("  Particle Count | Default: 4000 | Keep this to around half of the");
            modSettings.CreateCustomLabel("  Volumetric Cloud Radius");
            modSettings.CreateFloatSetting("  Particle Count | Min: 0 Max: 16000", particleCount_changed, settings.particleCount, 0, 16000);


            modSettings.CreateCustomLabel("");

            usePresetOnLoad_changed += usePresetOnLoad_Setting;
            modSettings.CreateBoolSetting("  Use custom preset when you load in | Default: False", usePresetOnLoad_changed, settings.usePresetOnLoad);


            modSettings.CreateCustomLabel("");


            presetToUse_changed += PresetToUse_Setting;
            modSettings.CreateIntSetting("  Custom Preset | Default: 2", presetToUse_changed, settings.presetToUse);
            modSettings.CreateCustomLabel("  1: Clear");
            modSettings.CreateCustomLabel("  2: Broken");
            modSettings.CreateCustomLabel("  3: Overcast");
            modSettings.CreateCustomLabel("  4: Foggy");
            modSettings.CreateCustomLabel("  5: Rain");
            modSettings.CreateCustomLabel("  6: Storm");



            modSettings.CreateCustomLabel("");

            disableAtmosphere_changed += disableAtmosphere_Setting;
            modSettings.CreateCustomLabel("  Disable Atmosphere when you have disable clouds enabled, turns");
            modSettings.CreateCustomLabel("  off atmospheric scattering, fog, and probably some other stuff.");
            modSettings.CreateBoolSetting("  Disable Atmosphere | Default: False", disableAtmosphere_changed, settings.disableAtmosphere);


            VTOLAPI.CreateSettingsMenu(modSettings);

        }



        private static void StartRandomWeatherTimer() // Starts the random weather timer,
        {

            rwTimer = new Timer(settings.randomWeatherTimerLength * 1000);
            rwTimer.Elapsed += RandomWeatherTimerElapsed;
            if (settings.usePresetOnLoad == false)
            {
                System.Random randomWeather = new System.Random();
                int wIndex = randomWeather.Next(weatherPresets.Length);
                OC.OverCloud.SetWeatherPreset(weatherPresets[wIndex], 1f);
                Debug.Log("Preset on load is false, setting random weather preset. OCRW");
            }
            else
            {
                OC.OverCloud.SetWeatherPreset(weatherPresets[settings.presetToUse - 1], 1f); // 2 of these, one in the sceneloaded cause I dont want to do excessive amounts of testing.
                Debug.Log("Preset on load is true, setting weather preset to " + weatherPresets[settings.presetToUse - 1] + ". OCRW");
            }
            rwTimer.AutoReset = true;
            rwTimer.Start();
        }

        private static void RandomWeatherTimerElapsed(object sender, ElapsedEventArgs e)
        {
            System.Random randomWeather = new System.Random();
            int wIndex = randomWeather.Next(weatherPresets.Length);

            OC.OverCloud.SetWeatherPreset(weatherPresets[wIndex], 35f);
            weatherName = weatherPresets[wIndex];

            switch (weatherName) // Weather forecast cause I thought it'd be funny
            {
                case "Clear":
                    weatherName = "sunburn";
                    break;
                case "Broken":
                    weatherName = "half sun half.. shack? ITS SUNSHACKS";
                    break;
                case "Overcast":
                    weatherName = "GRAY";
                    break;
                case "Foggy":
                    weatherName = "low visibility demolition derby";
                    break;
                case "Rain":
                    weatherName = "drip drop! Open that canopy";
                    break;
                case "Storm":
                    weatherName = "atmospheric men in your area! Get your own shimmering puff of indistinct love now";
                    break;
            }

            if (settings.randomWeatherTimerLength < 60)
            {
                weatherForecastLength = settings.randomWeatherTimerLength;
                weatherForecastTime = "second";
            }
            if (settings.randomWeatherTimerLength > 60 && settings.randomWeatherTimerLength < 3600)
            {
                weatherForecastLength = settings.randomWeatherTimerLength / 60;
                Math.Round(weatherForecastLength, 1);
                weatherForecastTime = "minute";
            }
            if (settings.randomWeatherTimerLength > 3600 && settings.randomWeatherTimerLength < 86400)
            {
                weatherForecastLength = settings.randomWeatherTimerLength / 3600;
                Math.Round(weatherForecastLength, 1);
                weatherForecastTime = "hour";
            }
            if (settings.randomWeatherTimerLength > 86400)
            {
                weatherForecastTime = "im not gonna calculate this.. you've gone into the damn days. AT LEAST!";
            }



            FlightLogger.Log(String.Format("This {0} {1} weather forecast shows a high chance of {2}!", weatherForecastLength, weatherForecastTime, weatherName));
            if (weatherName == "GRAY" || weatherName == "low visibility demolition derby")
            {
                FlightLogger.Log("Hope you brought some sunscreen!");
            }
        }

        void StartOvercloudTimer() // Starts the Overcloud timer. Use ocTimer.Stop(); and ocTimer.Dispose(); to stop.
        {
            ocTimer = new Timer(100);
            ocTimer.Elapsed += OvercloudElapsed; // Each time ocTimer elapses it runs OvercloudElapsed.
            ocTimer.AutoReset = true;
            ocTimer.Start();
            Debug.Log("Starting overcloud timer, no selectable env. OCRW");
        }

        void OvercloudElapsed(object sender, ElapsedEventArgs e)
        {
            var getScenario = PilotSaveManager.currentScenario;

            if (getScenario.mapSceneName == "CustomMapBase" || getScenario.mapSceneName == "CustomMapBase_OverCloud") // If this isn't here it'll run on Akutan, which results in a black screen.
            {
                if (settings.doNightTime == true)
                {
                    GameSettings.SetGameSettingValue("USE_OVERCLOUD", true, true);
                }

                if (settings.doNightTime == false && currentEnv != "night")
                {
                    GameSettings.SetGameSettingValue("USE_OVERCLOUD", true, true);
                }

                if (settings.doNightTime == false && currentEnv == "night")
                {
                    GameSettings.SetGameSettingValue("USE_OVERCLOUD", false, true);
                }
            }
        }

        void StartOvercloudSelectTimer() // Starts the Overcloud timer. Use ocTimer.Stop(); and ocTimer.Dispose(); to stop.
        {
            ocTimerSelect = new Timer(500);
            ocTimerSelect.Elapsed += OvercloudSelectElapsed; // Each time ocTimer elapses it runs OvercloudElapsed.
            ocTimerSelect.AutoReset = true;
            ocTimerSelect.Start();
            Debug.Log("Starting overcloud timer, selectable environment is true. OCRW");
        }

        void OvercloudSelectElapsed(object sender, ElapsedEventArgs e)
        {
            var getScenario = PilotSaveManager.currentScenario;

            if (getScenario.mapSceneName == "CustomMapBase" || getScenario.mapSceneName == "CustomMapBase_OverCloud") // If this isn't here it'll run on Akutan, which results in a black screen.
            {
                if (settings.doNightTime == true)
                {
                    GameSettings.SetGameSettingValue("USE_OVERCLOUD", true, true);
                }
                else
                {
                    if (getScenario.environmentName != "night")
                    {
                        Debug.Log("environmentName is Day or Morning, attempting to set OC to true");
                        GameSettings.SetGameSettingValue("USE_OVERCLOUD", true, true);
                    }
                    else
                    {
                        Debug.Log("environmentName is Night, attempting to set OC to false");
                        GameSettings.SetGameSettingValue("USE_OVERCLOUD", false, true);
                    }
                }
            }
        }

        public void StopRandomWeatherTimer() // Stops the Random Weather Timer, obviously..
        {
            runTimer = false;
            rwTimer.Stop();
            rwTimer.Dispose();
            Debug.Log("Stopping RW timer. OCRW");
        }

        public void StopOvercloudTimer()
        {
            ocTimer.Stop();
            ocTimer.Dispose();
            Debug.Log("Stopping OC selectable timer. OCRW");
        }
        public void StopOvercloudSelectTimer()
        {
            ocTimerSelect.Stop();
            ocTimerSelect.Dispose();
            Debug.Log("Stopping OC no selectable timer. OCRW");
        }





        public void Overcloud_Setting(bool newval) // Whenever you change a value in the mod settings it runs this.
        {
            settings.useOvercloud = newval;
            settingsChanged = true;
        }
        public void DisableCloudsOnly_Setting(bool newval) // Whenever you change a value in the mod settings it runs this.
        {
            settings.disableCloudsOnly = newval;
            settingsChanged = true;
        }
        public void FixWater_Setting(bool newval) // Whenever you change a value in the mod settings it runs this.
        {
            settings.fixWater = newval;
            settingsChanged = true;
        }
        public void OvercloudRandomWeather_Setting(bool newval)
        {
            settings.useOvercloudRandomWeather = newval;
            settingsChanged = true;
        }
        public void DoNightTime_Setting(bool newval)
        {
            settings.doNightTime = newval;
            settingsChanged = true;
        }
        public void RandomWeatherTimerLength_Setting(int newval)
        {
            settings.randomWeatherTimerLength = newval;
            settingsChanged = true;
        }
        public void DynamicTimeOfDay_Setting(bool newval)
        {
            settings.useDynamicTimeOfDay = newval;
            settingsChanged = true;
        }

        public void DynamicTimeOfDaySpeed_Setting(int newval)
        {
            settings.dynamicTimeOfDaySpeed = newval;
            settingsChanged = true;
        }
        public void DoLatLong_Setting(bool newval)
        {
            settings.doLatLong = newval;
            settingsChanged = true;
        }
        public void VolumetricCloudRadius_Setting(float newval)
        {
            settings.volumetricCloudRadius = newval;
            settingsChanged = true;
        }
        public void ParticleCount_Setting(float newval)
        {
            settings.particleCount = Mathf.RoundToInt(newval);
            settingsChanged = true;
        }
        public void usePresetOnLoad_Setting(bool newval)
        {
            settings.usePresetOnLoad = newval;
            settingsChanged = true;
        }
        public void PresetToUse_Setting(int newval)
        {
            settings.presetToUse = newval;
            settingsChanged = true;
        }
        public void disableAtmosphere_Setting(bool newval)
        {
            settings.disableAtmosphere = newval;
            settingsChanged = true;
        }



        private void SceneLoaded(VTOLScenes scene)
        {
            switch (scene)
            {
                case VTOLScenes.ReadyRoom:
                    StopRandomWeatherTimer();
                    StopOvercloudTimer();
                    StopOvercloudSelectTimer();
                    sceneLoaded = false;

                    break;
                case VTOLScenes.VehicleConfiguration:
                    GameSettings.SetGameSettingValue("USE_OVERCLOUD", false, true);
                    if (settings.useOvercloud == true && PilotSaveManager.currentScenario.mapSceneName != "Akutan")
                    {
                        GameSettings.SetGameSettingValue("USE_OVERCLOUD", true, true);


                        if (VTScenario.currentScenarioInfo.selectableEnv == true)
                        {
                            StartOvercloudTimer();
                        }
                        else
                        {
                            StartOvercloudSelectTimer();
                        }

                    }

                    break;
                case VTOLScenes.CustomMapBase_OverCloud:
                    sceneLoaded = true;

                    if (VTScenario.currentScenarioInfo.selectableEnv == true)
                    {
                        StopOvercloudTimer();
                    }
                    else
                    {
                        StopOvercloudSelectTimer();
                    }
                    
                    GameSettings.TryGetGameSettingValue<bool>("USE_OVERCLOUD", out bool isOvercloudEnabled);

                    if (isOvercloudEnabled == true && settings.useOvercloudRandomWeather && settings.disableCloudsOnly == false)
                    {
                        StartRandomWeatherTimer();
                        runTimer=true; // :~)
                    }
                    if (isOvercloudEnabled == true && settings.usePresetOnLoad == true)
                    {
                        OC.OverCloud.SetWeatherPreset(weatherPresets[settings.presetToUse - 1], 1f);
                    }

                    SkyStuffs.UpdateCloudSettings();

                    if (settings.doLatLong == true)
                    {
                        SkyStuffs.SunPositionInSky();
                    }

                    if (isOvercloudEnabled == true)
                    {
                        if (PilotSaveManager.currentScenario.mapSceneName != "Akutan")
                        {
                            SkyStuffs.DisableNGSS_Directional();
                        }
                    }
                    if (settings.disableCloudsOnly == true)
                    {
                        DisableCloudsOnly();
                    }

                    break;
            }
            Debug.Log("SCENE LOADED: " + scene + ". OCRW");
            CheckSave();
        }

        public static void DisableCloudsOnly()
        {
            Camera[] overCloudCameras = Camera.allCameras;
            foreach (Camera overCloudCamera in overCloudCameras)
            {
                overCloudCamera.GetComponent<OC.OverCloudCamera>().render2DFallback = false;
                overCloudCamera.GetComponent<OC.OverCloudCamera>().renderRainMask = false;
                overCloudCamera.GetComponent<OC.OverCloudCamera>().renderScatteringMask = false;
                overCloudCamera.GetComponent<OC.OverCloudCamera>().renderVolumetricClouds = false;
                overCloudCamera.GetComponent<OC.OverCloudCamera>().renderAtmosphere = !settings.disableAtmosphere;
            }
        }

        public void Update()
        {
            GameSettings.TryGetGameSettingValue<bool>("USE_OVERCLOUD", out bool isOvercloudEnabled);
            if (settings.useOvercloud == true && isOvercloudEnabled == true && sceneLoaded == true)
            {
                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Alpha1))
                {
                    StopRandomWeatherTimer();
                    runTimer = false;
                    OC.OverCloud.SetWeatherPreset(weatherPresets[0], 1f);
                    Debug.Log("Pressed 1, setting weather preset to: " + weatherPresets[0] + ". OCRW");
                }
                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Alpha2))
                {
                    StopRandomWeatherTimer();
                    runTimer = false;
                    OC.OverCloud.SetWeatherPreset(weatherPresets[1], 1f);
                    Debug.Log("Pressed 2, setting weather preset to: " + weatherPresets[1] + ". OCRW");
                }
                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Alpha3))
                {
                    StopRandomWeatherTimer();
                    runTimer = false;
                    OC.OverCloud.SetWeatherPreset(weatherPresets[2], 1f);
                    Debug.Log("Pressed 3, setting weather preset to: " + weatherPresets[2] + ". OCRW");
                }
                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Alpha4))
                {
                    StopRandomWeatherTimer();
                    runTimer = false;
                    OC.OverCloud.SetWeatherPreset(weatherPresets[3], 1f);
                    Debug.Log("Pressed 4, setting weather preset to: " + weatherPresets[3] + ". OCRW");
                }
                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Alpha5))
                {
                    StopRandomWeatherTimer();
                    runTimer = false;
                    OC.OverCloud.SetWeatherPreset(weatherPresets[4], 1f);
                    Debug.Log("Pressed 5, setting weather preset to: " + weatherPresets[4] + ". OCRW");
                }
                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Alpha6))
                {
                    StopRandomWeatherTimer();
                    runTimer = false;
                    OC.OverCloud.SetWeatherPreset(weatherPresets[5], 1f);
                    Debug.Log("Pressed 6, setting weather preset to: " + weatherPresets[5] + ". OCRW");
                }
                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.M))
                {
                    if (runTimer == false && settings.disableCloudsOnly == false)
                    {
                        StartRandomWeatherTimer();
                        runTimer = true;
                        Debug.Log("Pressed M, starting random weather timer. OCRW");
                    }
                }

                if (settings.useDynamicTimeOfDay == true)
                {
                    SkyStuffs.EnableDynamicTimeOfDay();
                }



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

                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Keypad1))
                {
                    OC.OverCloud.timeOfDay.latitude = 83f;
                    OC.OverCloud.timeOfDay.longitude = 20f;
                }

                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Keypad2))
                {
                    OC.OverCloud.timeOfDay.latitude = 62f;
                    OC.OverCloud.timeOfDay.longitude = -3f;
                }

                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Keypad3))
                {
                    OC.OverCloud.timeOfDay.latitude = 28f;
                    OC.OverCloud.timeOfDay.longitude = 23f;
                }

                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Keypad4))
                {
                    OC.OverCloud.timeOfDay.latitude = 1f;
                    OC.OverCloud.timeOfDay.longitude = 20f;
                }
            }
        }


        private void MissionReloaded()
        {
            CheckSave();
        }

        private void OnApplicationQuit() // If you're ingame with overcloud enabled and quit the whole game, this should prevent it from being enabled and fucking you later.
        {
            GameSettings.SetGameSettingValue("USE_OVERCLOUD", false, true);
            CheckSave();
        }

        // The code below was yoinked from cheeeese, I don't know how it works.
        private void CheckSave() 
        {
            if (settingsChanged == true)
            {
                SaveToFile();
            }
        }
        public void LoadFromFile()
        {
            string folder = ModFolder;
            if (Directory.Exists(ModFolder))
            {
                try
                {
                    string temp = File.ReadAllText(folder + @"\settings.json");

                    settings = JsonConvert.DeserializeObject<OCsettings>(temp);
                    settingsChanged = false;
                }
                catch
                {
                    SaveToFile();
                }
            }
        }
        public void SaveToFile()
        {
            string folder = ModFolder;

            if (Directory.Exists(folder))
            {
                File.WriteAllText(folder + @"\settings.json", JsonConvert.SerializeObject(settings));
                settingsChanged = false;
            }
        }

    }
}