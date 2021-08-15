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
        public bool useOvercloudRandomWeather = true;
        public bool doNightTime = false;
        public int randomWeatherTimerLength = 800;
        public bool useDynamicTimeOfDay = false;
        public int dynamicTimeOfDaySpeed = 120;
        public float volumetricCloudRadius = 6000f;
        public float particleCount = 4000f;
    }
    public class Main : VTOLMOD
    {
        public static OCsettings settings;
        public bool settingsChanged;

        public static string currentEnv; // Current environment (day, night, morning)

        public static bool runTimer = false;
        private static Timer rwTimer; // Random weather timer
        private static Timer ocTimer; // Timer that does the Overcloud stuffs
        private static Timer ocTimerSelect; // Timer that does the Overcloud stuffs if selectableEnv = false


        public static string weatherName;
        public static double weatherForecastLength;
        public static string weatherForecastTime;

        public UnityAction<bool> overcloud_changed; 
        public UnityAction<bool> overcloudRandomWeather_changed;
        public UnityAction<bool> doNightTime_changed;
        public UnityAction<int> randomWeatherTimerLength_changed;
        public UnityAction<bool> dynamicTimeOfDay_changed;
        public UnityAction<int> dynamicTimeOfDaySpeed_changed;
        public UnityAction<float> volumetricCloudRadius_changed;
        public UnityAction<float> particleCount_changed;


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

            overcloud_changed += Overcloud_Setting; // Whenever you change the value ingame it runs Overcloud_Setting
            modSettings.CreateBoolSetting("  Overcloud | Default: True", overcloud_changed, settings.useOvercloud);
            

            modSettings.CreateCustomLabel("");

            overcloudRandomWeather_changed += OvercloudRandomWeather_Setting;
            modSettings.CreateBoolSetting("  Overcloud Random Weather | Default: True", overcloudRandomWeather_changed, settings.useOvercloudRandomWeather);


            modSettings.CreateCustomLabel("");

            doNightTime_changed += doNightTime_changed;
            modSettings.CreateCustomLabel("  Allow night time? Note: Water is really bright, don't know if I can fix this.");
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


            volumetricCloudRadius_changed += VolumetricCloudRadius_Setting;
            modSettings.CreateCustomLabel("  Volumetric Cloud Radius | Default: 6000 | After this radius the clouds");
            modSettings.CreateFloatSetting("  become 2D | Min: 0 Max: 64000", volumetricCloudRadius_changed, settings.volumetricCloudRadius, 0, 64000);


            modSettings.CreateCustomLabel("");


            particleCount_changed += ParticleCount_Setting;
            modSettings.CreateCustomLabel("  Particle Count | Default: 4000 | Keep this to around half of the Vol ");
            modSettings.CreateFloatSetting("  Cloud Radius | Min: 0 Max: 32000", particleCount_changed, settings.particleCount, 0, 32000);



            VTOLAPI.CreateSettingsMenu(modSettings);

        }



        private static void StartRandomWeatherTimer() // Starts the random weather timer,
        {

            rwTimer = new Timer(settings.randomWeatherTimerLength * 1000);
            rwTimer.Elapsed += RandomWeatherTimerElapsed;
            rwTimer.AutoReset = true;
            rwTimer.Start();
        }

        private static void RandomWeatherTimerElapsed(object sender, ElapsedEventArgs e) 
        {
            System.Random randomWeather = new System.Random();
            string[] weatherPresets = {
                        "Clear", "Broken", "Overcast", "Foggy", "Rain", "Storm"
                        };
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

            if(settings.randomWeatherTimerLength < 60)
            {
                weatherForecastLength = settings.randomWeatherTimerLength;
                weatherForecastTime = "second";
            }
            if(settings.randomWeatherTimerLength > 60 && settings.randomWeatherTimerLength < 3600)
            {
                weatherForecastLength = settings.randomWeatherTimerLength / 60;
                Math.Round(weatherForecastLength, 1);
                weatherForecastTime = "minute";
            }
            if(settings.randomWeatherTimerLength > 3600 && settings.randomWeatherTimerLength < 86400)
            {
                weatherForecastLength = settings.randomWeatherTimerLength / 3600;
                Math.Round(weatherForecastLength, 1);
                weatherForecastTime = "hour";
            }
            if(settings.randomWeatherTimerLength > 86400)
            {
                weatherForecastTime = "im not gonna calculate this.. you've gone into the damn days. AT LEAST!";
            }



            FlightLogger.Log(String.Format("This {0} {1} weather forecast shows a high chance of {2}!", weatherForecastLength, weatherForecastTime, weatherName));
            if(weatherName == "GRAY" || weatherName == "low visibility demolition derby")
            {
                FlightLogger.Log("Hope you brought some sunscreen!");
            }
        }

        void StartOvercloudTimer() // Starts the Overcloud timer. Use ocTimer.Stop(); and ocTimer.Dispose(); to stop.
        {
            runTimer = true; // Im scared to touch this, because I'm too lazy to try and figure out what it does and why I added it
            ocTimer = new Timer(100);
            ocTimer.Elapsed += OvercloudElapsed; // Each time ocTimer elapses it runs OvercloudElapsed.
            ocTimer.AutoReset = true;
            ocTimer.Start();
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
            runTimer = true;
            ocTimerSelect = new Timer(100);
            ocTimerSelect.Elapsed += OvercloudSelectElapsed; // Each time ocTimer elapses it runs OvercloudElapsed.
            ocTimerSelect.AutoReset = true;
            ocTimerSelect.Start();
        }

        void OvercloudSelectElapsed(object sender, ElapsedEventArgs e)
        {
            var getScenario = PilotSaveManager.currentScenario;

            if (getScenario.mapSceneName == "CustomMapBase" || getScenario.mapSceneName == "CustomMapBase_OverCloud") // If this isn't here it'll run on Akutan, which results in a black screen.
            {
                if (settings.doNightTime == true)
                {
                    GameSettings.SetGameSettingValue("USE_OVERCLOUD", true, true);
                    Debug.Log("doNightTime is true, enabling OC");
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
        }

        public void StopOvercloudTimer()
        {
            ocTimer.Stop();
            ocTimer.Dispose();
        }
        public void StopOvercloudSelectTimer()
        {
            ocTimerSelect.Stop();
            ocTimerSelect.Dispose();
        }





        public void Overcloud_Setting(bool newval) // Whenever you change a value in the mod settings it runs this.
        {
            settings.useOvercloud = newval;
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



        private void SceneLoaded(VTOLScenes scene)
        {
            CheckSave();

            switch (scene)
            {
                case VTOLScenes.ReadyRoom:
                    StopRandomWeatherTimer();
                    StopOvercloudTimer();
                    StopOvercloudSelectTimer();

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
                            Debug.Log("Starting OverCloudSelectTimer");
                        }

                    }

                    break;
                case VTOLScenes.CustomMapBase:
                case VTOLScenes.CustomMapBase_OverCloud:
                    StopOvercloudTimer();

                    GameSettings.TryGetGameSettingValue<bool>("USE_OVERCLOUD", out bool isOvercloudEnabled);

                    if (isOvercloudEnabled == true && settings.useOvercloudRandomWeather)
                    {
                        StartRandomWeatherTimer();
                    }
                    break;
            }
        }

        public void Update()
        {

            if (settings.useOvercloud == true)
            {
                string[] weatherPresets = {
                        "Clear", "Broken", "Overcast", "Foggy", "Rain", "Storm"
                        };

                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Alpha1))
                {
                    StopRandomWeatherTimer();
                    runTimer = false;
                    OC.OverCloud.SetWeatherPreset(weatherPresets[0], 1f);
                }
                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Alpha2))
                {
                    StopRandomWeatherTimer();
                    runTimer = false;
                    OC.OverCloud.SetWeatherPreset(weatherPresets[1], 1f);
                }
                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Alpha3))
                {
                    StopRandomWeatherTimer();
                    runTimer = false;
                    OC.OverCloud.SetWeatherPreset(weatherPresets[2], 1f);
                }
                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Alpha4))
                {
                    StopRandomWeatherTimer();
                    runTimer = false;
                    OC.OverCloud.SetWeatherPreset(weatherPresets[3], 1f);
                }
                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Alpha5))
                {
                    StopRandomWeatherTimer();
                    runTimer = false;
                    OC.OverCloud.SetWeatherPreset(weatherPresets[4], 1f);
                }
                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.Alpha6))
                {
                    StopRandomWeatherTimer();
                    runTimer = false;
                    OC.OverCloud.SetWeatherPreset(weatherPresets[5], 1f);
                }
                if (Rewired.ReInput.controllers.Keyboard.GetKeyDown(KeyCode.M))
                {
                    if (runTimer == false)
                    {
                        StartRandomWeatherTimer();
                        runTimer = true;
                    }
                }

                if (PilotSaveManager.currentScenario.mapSceneName != "Akutan")
                {
                    SkyStuffs.DisableNGSS_Directional();
                }

                if (settings.useDynamicTimeOfDay == true)
                {
                    SkyStuffs.EnableDynamicTimeOfDay();
                }

                SkyStuffs.UpdateCloudSettings();
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