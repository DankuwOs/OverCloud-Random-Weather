using System;
using System.Timers;
using UnityEngine;
using UnityEngine.Events;
using System.Reflection;
using System.IO;
using Valve.Newtonsoft.Json;
using Harmony;

namespace OvercloudRandomWeather
{
    public class OCsettings
    {
        /// <summary>
        /// Enables OverCloud itself.
        /// </summary>
        public bool useOvercloud = true;

        /// <summary>
        /// Disables only the clouds, still leaves the atmosphere and the time of day.
        /// </summary>
        public bool disableCloudsOnly = false;

        /// <summary>
        /// Uses a much darker water material so it doesn't glow at night.
        /// </summary>
        public bool fixWater = false;

        /// <summary>
        /// Uses the random weather.
        /// </summary>
        public bool useRandomWeather = true;

        /// <summary>
        /// Allows you to load into a scenario if it's nighttime.
        /// </summary>
        public bool doNightTime = true;

        /// <summary>
        /// The length in seconds inbetween each randomly selected weather preset.
        /// </summary>
        public int randomWeatherTimerLength = 800;

        /// <summary>
        /// Uses the day / night cycle.
        /// </summary>
        public bool useDynamicTimeOfDay = true;

        /// <summary>
        /// The multiplier of the day / night cycle. 1 is realtime, 2 is double, etc.
        /// </summary>
        public int dynamicTimeOfDaySpeed = 120;

        /// <summary>
        /// Uses the latitude and longitude of where the biomes would be in real life for the ingame scenario.
        /// </summary>
        public bool doLatLong = true;

        /// <summary>
        /// The radius of the 3D cloud plane.
        /// </summary>
        public float volumetricCloudRadius = 8000f;

        /// <summary>
        /// The total amount of particles to use for the clouds.
        /// </summary>
        public float particleCount = 4000f;

        /// <summary>
        /// When you load into a scenario it will select a specific preset instead of a random one.
        /// </summary>
        public bool usePresetOnLoad = false;

        /// <summary>
        /// Selects which preset (1-6) for usePresetOnLoad to use.
        /// </summary>
        public int presetToUse = 2;

        /// <summary>
        /// Disables the atmosphere, for use with disableCloudsOnly for maybe more performance. I didn't actually check.
        /// </summary>
        public bool disableAtmosphere = false;

        /// <summary>
        /// Disables the atmosphere, but this time will disable it with clouds enabled.
        /// </summary>
        public bool disableAtmosphereOverall = false;

        /// <summary>
        /// Maximum distance for the lightning to spawn.
        /// </summary>
        public float maximumDistance = 20000f;

        /// <summary>
        /// Minimum amount of seconds to pass before another strike can occur
        /// </summary>
        public float minimumInterval = 3f;

        /// <summary>
        /// Maximum amount of seconds to pass before another strike can occur
        /// </summary>
        public float maximumInterval = 13f;

        /// <summary>
        /// Chance for lightning to strike multiple times (in the same spot?)
        /// </summary>
        public float restrikeChance = 15f;
    }

    public class Main : VTOLMOD
    {
        #region Variables or whatever

        /// <summary>
        /// Settings. Wow!
        /// </summary>
        public static OCsettings settings;

        /// <summary>
        /// ARE THE SETTINGS CHANGED?!
        /// </summary>
        public bool settingsChanged;

        /// <summary>
        /// Current environment (day, night, morning)
        /// </summary>
        public static string currentEnv;

        /// <summary>
        /// Current biome, for the suns position in the sky.
        /// </summary>
        public static string currentBiome;

        /// <summary>
        /// A list of all the weather presets included with OC.
        /// </summary>
        public static string[] weatherPresets = {
                        "Clear", "Broken", "Overcast", "Foggy", "Rain", "Storm"
                        };

        /// <summary>
        /// The light used by the game whenever OC is not enabled. STILL ENABLED WITH OVERCLOUD FOR SOME REASON.
        /// </summary>
        public static Light ngss_Directional;

        /// <summary>
        /// Is the scene loaded?
        /// </summary>
        public static bool sceneLoaded = false;

        /// <summary>
        /// Whenever you quicksave this gets set to your current time of day.
        /// </summary>
        public static double quickSavedTimeOfDay;

        /// <summary>
        /// Probably used for something to do with the weather timer.
        /// </summary>
        public static bool runTimer = false;

        /// <summary>
        /// Random weather timer.
        /// </summary>
        private static Timer rwTimer;

        /// <summary>
        /// Timer that checks if OC should be enabled, and then enables it.
        /// </summary>
        private static Timer ocTimer; // System.Threading.Thread.Sleep(x); was freezing the game, so a timer was the first working solution I found.

        /// <summary>
        /// Timer that checks if OC should be enabled, and then enables it. Only runs whenever selectableEnv = false.
        /// </summary>
        private static Timer ocTimerSelect;

        /// <summary>
        /// Name of the current weather preset, used for the weather forecast.
        /// </summary>
        public static string weatherName;

        /// <summary>
        /// Length of the random weather timer but divided into seconds / minutes / hours / but not days because that's a lot of work.
        /// </summary>
        public static double weatherForecastLength;

        /// <summary>
        /// If the length is whatever turns it into "second", "minute", or "hour". For example, "This 13.3 minute weather forecast shows a high chance of low visibility demolition derby!"
        /// </summary>
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
        public UnityAction<bool> disableAtmosphereOverall_changed;
        public UnityAction<float> maximumDistance_changed;
        public UnityAction<float> minimumInterval_changed;
        public UnityAction<float> maximumInterval_changed;
        public UnityAction<float> restrikeChance_changed;
        #endregion

        public override void ModLoaded()
        {
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("dankuwos.overcloud");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            base.ModLoaded();

            VTOLAPI.SceneLoaded += SceneLoaded;
            VTOLAPI.MissionReloaded += MissionReloaded;

            #region Settings

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

            doNightTime_changed += DoNightTime_Setting;
            modSettings.CreateCustomLabel("  Allow night time? Water is really bright if you go from day to night.");
            modSettings.CreateBoolSetting("  Night Time | Default: True", doNightTime_changed, settings.doNightTime);


            modSettings.CreateCustomLabel("");


            randomWeatherTimerLength_changed += RandomWeatherTimerLength_Setting;
            modSettings.CreateCustomLabel("  Random Weather Timer Length | Default: 800");
            modSettings.CreateIntSetting("  (in seconds)", randomWeatherTimerLength_changed, settings.randomWeatherTimerLength);


            modSettings.CreateCustomLabel("");


            dynamicTimeOfDaySpeed_changed += DynamicTimeOfDaySpeed_Setting;
            modSettings.CreateCustomLabel("  Dynamic Time Of Day Speed | Default: 120 | 1 Is realtime,");
            modSettings.CreateIntSetting("  60 is 1 minute every second", dynamicTimeOfDaySpeed_changed, settings.dynamicTimeOfDaySpeed);


            modSettings.CreateCustomLabel("");


            volumetricCloudRadius_changed += VolumetricCloudRadius_Setting;
            modSettings.CreateCustomLabel("  Volumetric Cloud Radius | Default: 8000 | After this radius the clouds");
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

            disableAtmosphere_changed += DisableAtmosphere_Setting;
            modSettings.CreateCustomLabel("  DOESN'T DO ANYTHING IF DISABLE CLOUDS ISN'T ENABLED");
            modSettings.CreateCustomLabel("  Disable Atmosphere when you have disable clouds enabled, turns");
            modSettings.CreateCustomLabel("  off atmospheric scattering, fog, and probably some other stuff.");
            modSettings.CreateBoolSetting("  NO CLOUDS ATMOSPHERE | Default: False", disableAtmosphere_changed, settings.disableAtmosphere);



            modSettings.CreateCustomLabel("");

            disableAtmosphereOverall_changed += DisableAtmosphereOverall_Setting;
            modSettings.CreateCustomLabel("  Disable Atmosphere with clouds on, turns");
            modSettings.CreateCustomLabel("  off atmospheric scattering, fog, and probably some other stuff.");
            modSettings.CreateBoolSetting("  Disable Atmosphere | Default: False", disableAtmosphereOverall_changed, settings.disableAtmosphereOverall);

            modSettings.CreateCustomLabel("");
            modSettings.CreateCustomLabel("");
            modSettings.CreateCustomLabel("LIGHTNING STRIKE SETTINGS");
            modSettings.CreateCustomLabel("");


            maximumDistance_changed += MaximumDistance_Setting;
            modSettings.CreateCustomLabel("  Maximum distance lightning can strike from you.");
            modSettings.CreateFloatSetting("  Maximum Distance (in meters)", maximumDistance_changed, settings.maximumDistance, 0, 20000);
            modSettings.CreateCustomLabel("  Default 20000 (50 will hit)");

            modSettings.CreateCustomLabel("");


            minimumInterval_changed += MinimumInterval_Setting;
            modSettings.CreateCustomLabel("  Minimum amount of time between strikes.");
            modSettings.CreateFloatSetting("  Minimum Interval (in seconds)", minimumInterval_changed, settings.minimumInterval, 0, 20000);
            modSettings.CreateCustomLabel("  Default 3");

            modSettings.CreateCustomLabel("");


            maximumInterval_changed += MaximumInterval_Setting;
            modSettings.CreateCustomLabel("  Maximum amount of time between strikes.");
            modSettings.CreateFloatSetting("  Maximum Interval (in seconds)", maximumInterval_changed, settings.maximumInterval, 0, 20000);
            modSettings.CreateCustomLabel("  Default 13");

            modSettings.CreateCustomLabel("");


            restrikeChance_changed += RestrikeChance_Setting;
            modSettings.CreateCustomLabel("  Chance for lightning to strike again.");
            modSettings.CreateFloatSetting("  Restrike Chance", restrikeChance_changed, settings.restrikeChance, 0, 100);
            modSettings.CreateCustomLabel("  Default 15% (maybe doesn't work?)");

            VTOLAPI.CreateSettingsMenu(modSettings);

            #endregion

        }


        #region Random Weather Timer

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

        #endregion

        #region OverCloud Timer
        void StartOvercloudTimer() // Starts the Overcloud timer. Use ocTimer.Stop(); and ocTimer.Dispose(); to stop.
        {
            ocTimer = new Timer(2000);
            ocTimer.Elapsed += OvercloudElapsed; // Each time ocTimer elapses it runs OvercloudElapsed.
            ocTimer.AutoReset = true;
            ocTimer.Start();
            Debug.Log("Starting overcloud timer, no selectable env. OCRW");
        }

        void OvercloudElapsed(object sender, ElapsedEventArgs e)
        {
            GameSettings.TryGetGameSettingValue("USE_OVERCLOUD", out bool isOverCloudBreakingShit);
            if (isOverCloudBreakingShit == false)
            {
                if (settings.doNightTime == true)
                {
                    GameSettings.SetGameSettingValue("USE_OVERCLOUD", true, true);
                }

                if (settings.doNightTime == false && currentEnv != "night")
                {
                    GameSettings.SetGameSettingValue("USE_OVERCLOUD", true, true);
                }
            }
            else
            {
                if (settings.doNightTime == false && currentEnv == "night")
                {
                    GameSettings.SetGameSettingValue("USE_OVERCLOUD", false, true);
                }
            }
        }

        #endregion

        #region OverCloud Select Timer
        void StartOvercloudSelectTimer() // Starts the Overcloud timer. Use ocTimer.Stop(); and ocTimer.Dispose(); to stop.
        {
            ocTimerSelect = new Timer(1000);
            ocTimerSelect.Elapsed += OvercloudSelectElapsed; // Each time ocTimer elapses it runs OvercloudElapsed.
            ocTimerSelect.AutoReset = true;
            ocTimerSelect.Start();
            Debug.Log("Starting overcloud timer, selectable environment is true. OCRW");
        }

        void OvercloudSelectElapsed(object sender, ElapsedEventArgs e)
        {
            var getScenario = PilotSaveManager.currentScenario;

            if (settings.doNightTime == true)
            {
                    GameSettings.SetGameSettingValue("USE_OVERCLOUD", true, true);
            }
            else
            {
                if (getScenario.environmentName != "night")
                {
                    Debug.Log("environmentName is Day or Morning, attempting to set OC to true. OCRW");
                        GameSettings.SetGameSettingValue("USE_OVERCLOUD", true, true);
                }
                else
                {
                        Debug.Log("environmentName is Night, attempting to set OC to false. OCRW");
                        GameSettings.SetGameSettingValue("USE_OVERCLOUD", false, true);
                }
            }
        }

        #endregion

        #region Stop Timers

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

        #endregion



        #region Settings Method Things

        public void Overcloud_Setting(bool newval) // Whenever you change a value in the mod settings it runs this.
        {
            settings.useOvercloud = newval;
            settingsChanged = true;
        }
        public void DisableCloudsOnly_Setting(bool newval)
        {
            settings.disableCloudsOnly = newval;
            settingsChanged = true;
        }
        public void FixWater_Setting(bool newval)
        {
            settings.fixWater = newval;
            settingsChanged = true;
        }
        public void OvercloudRandomWeather_Setting(bool newval)
        {
            settings.useRandomWeather = newval;
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
        public void DisableAtmosphere_Setting(bool newval)
        {
            settings.disableAtmosphere = newval;
            settingsChanged = true;
        }
        public void DisableAtmosphereOverall_Setting(bool newval)
        {
            settings.disableAtmosphereOverall = newval;
            settingsChanged = true;
        }
        public void MaximumDistance_Setting(float newval)
        {
            settings.maximumDistance = newval;
            settingsChanged = true;
        }
        public void MinimumInterval_Setting(float newval)
        {
            settings.minimumInterval = newval;
            settingsChanged = true;
        }
        public void MaximumInterval_Setting(float newval)
        {
            settings.maximumInterval = newval;
            settingsChanged = true;
        }
        public void RestrikeChance_Setting(float newval)
        {
            settings.restrikeChance = newval;
            settingsChanged = true;
        }

        #endregion


        private void SceneLoaded(VTOLScenes scene)
        {
            GameSettings.TryGetGameSettingValue("USE_OVERCLOUD", out bool isOverCloudBreakingShit);
            switch (scene)
            {
                #region ReadyRoom

                case VTOLScenes.ReadyRoom:
                    StopRandomWeatherTimer();
                    sceneLoaded = false;
                    quickSavedTimeOfDay = 25;

                    if (isOverCloudBreakingShit == true)
                    {
                        GameSettings.SetGameSettingValue("USE_OVERCLOUD", false, true);
                    }

                    break;

                #endregion

                #region Vehicle Config

                case VTOLScenes.VehicleConfiguration:

                    Debug.Log("oc is " + isOverCloudBreakingShit.ToString() + ". OCRW");

                    GameSettings.SetGameSettingValue("USE_OVERCLOUD", false, true);

                    if (settings.useOvercloud == true && PilotSaveManager.currentScenario.mapSceneName != "Akutan")
                    {
                        if (VTScenario.currentScenarioInfo.selectableEnv == true)
                        {
                            StartOvercloudTimer();
                        }
                        else
                        {
                            StartOvercloudSelectTimer();
                        }

                    }
                    quickSavedTimeOfDay = 25;

                    break;

                #endregion

                #region OverCloud Scenario

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

                    if (isOverCloudBreakingShit == true && settings.useRandomWeather && settings.disableCloudsOnly == false)
                    {
                        StartRandomWeatherTimer();
                        runTimer = true; // :~)
                    }
                    if (isOverCloudBreakingShit == true && settings.usePresetOnLoad == true)
                    {
                        OC.OverCloud.SetWeatherPreset(weatherPresets[settings.presetToUse - 1], 1f);
                    }

                    SkyStuffs.UpdateCloudSettings();

                    if (settings.doLatLong == true)
                    {
                        SkyStuffs.SunPositionInSky();
                    }

                    if (isOverCloudBreakingShit == true)
                    {
                        SkyStuffs.DisableNGSS_Directional();
                    }

                    if (settings.disableCloudsOnly == true)
                    {
                        DisableCloudsOnly();
                    }
                    else if (settings.disableAtmosphereOverall == true)
                    {
                        DisableAtmosphereOverall();
                    }

                    var lightning = OC.OverCloud.weather.lightning;

                    lightning.distanceMin = 0f;
                    lightning.distanceMax = settings.maximumDistance;

                    lightning.intervalMin = settings.minimumInterval;
                    lightning.intervalMax = settings.maximumInterval;

                    lightning.restrikeChance = settings.restrikeChance;

                    break;

                    #endregion
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
        public static void DisableAtmosphereOverall()
        {
            Camera[] overCloudCameras = Camera.allCameras;
            foreach (Camera overCloudCamera in overCloudCameras)
            {
                overCloudCamera.GetComponent<OC.OverCloudCamera>().renderAtmosphere = !settings.disableAtmosphereOverall;
            }
        }

        public void Update()
        {
            if (settings.useOvercloud == true && sceneLoaded == true)
            {
                #region Weather Keybinds

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

                #endregion

                if (settings.useDynamicTimeOfDay == true)
                {
                    SkyStuffs.EnableDynamicTimeOfDay();
                }

                #region Biome Latitude / Longitude Keybinds

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
                    OC.OverCloud.timeOfDay.longitude = 28f;
                }

                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Keypad3))
                {
                    OC.OverCloud.timeOfDay.latitude = 28f;
                    OC.OverCloud.timeOfDay.longitude = 23f;
                }

                #endregion

                #region Performance Debug Keybinds

                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.A))
                {
                    Camera[] overCloudCameras = Camera.allCameras;
                    foreach (Camera overCloudCamera in overCloudCameras)
                    {
                        overCloudCamera.GetComponent<OC.OverCloudCamera>().render2DFallback = !overCloudCamera.GetComponent<OC.OverCloudCamera>().render2DFallback;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderRainMask = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderRainMask;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderScatteringMask = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderScatteringMask;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderVolumetricClouds = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderVolumetricClouds;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderAtmosphere = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderAtmosphere;
                    }
                }

                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.S))
                {
                    Camera[] overCloudCameras = Camera.allCameras;
                    foreach (Camera overCloudCamera in overCloudCameras)
                    {
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().render2DFallback = !overCloudCamera.GetComponent<OC.OverCloudCamera>().render2DFallback;
                        overCloudCamera.GetComponent<OC.OverCloudCamera>().renderRainMask = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderRainMask;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderScatteringMask = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderScatteringMask;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderVolumetricClouds = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderVolumetricClouds;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderAtmosphere = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderAtmosphere;
                    }
                }

                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.D))
                {
                    Camera[] overCloudCameras = Camera.allCameras;
                    foreach (Camera overCloudCamera in overCloudCameras)
                    {
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().render2DFallback = !overCloudCamera.GetComponent<OC.OverCloudCamera>().render2DFallback;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderRainMask = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderRainMask;
                        overCloudCamera.GetComponent<OC.OverCloudCamera>().renderScatteringMask = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderScatteringMask;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderVolumetricClouds = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderVolumetricClouds;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderAtmosphere = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderAtmosphere;
                    }
                }

                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.F))
                {
                    Camera[] overCloudCameras = Camera.allCameras;
                    foreach (Camera overCloudCamera in overCloudCameras)
                    {
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().render2DFallback = !overCloudCamera.GetComponent<OC.OverCloudCamera>().render2DFallback;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderRainMask = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderRainMask;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderScatteringMask = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderScatteringMask;
                        overCloudCamera.GetComponent<OC.OverCloudCamera>().renderVolumetricClouds = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderVolumetricClouds;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderAtmosphere = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderAtmosphere;
                    }
                }

                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.G))
                {
                    Camera[] overCloudCameras = Camera.allCameras;
                    foreach (Camera overCloudCamera in overCloudCameras)
                    {
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().render2DFallback = !overCloudCamera.GetComponent<OC.OverCloudCamera>().render2DFallback;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderRainMask = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderRainMask;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderScatteringMask = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderScatteringMask;
                        //overCloudCamera.GetComponent<OC.OverCloudCamera>().renderVolumetricClouds = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderVolumetricClouds;
                        overCloudCamera.GetComponent<OC.OverCloudCamera>().renderAtmosphere = !overCloudCamera.GetComponent<OC.OverCloudCamera>().renderAtmosphere;
                    }
                }

                #endregion
            }
        }

        #region Cheese Save

        private void OnApplicationQuit() // APPARENTLY THIS DOESN'T WORK HALF THE TIME!
        {
            GameSettings.SetGameSettingValue("USE_OVERCLOUD", false, true);
            CheckSave();
        }

        // The code below was yoinked from cheese, I don't know how it works.
        private void MissionReloaded()
        {
            CheckSave();

            if (settings.disableAtmosphereOverall == true)
            {
                DisableAtmosphereOverall();
            }
        }
       
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

        #endregion

    }
}