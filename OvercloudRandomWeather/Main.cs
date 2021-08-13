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
        public int randomWeatherTimerLength = 800;
    }
    public class Main : VTOLMOD
    {
        public static OCsettings settings;
        public bool settingsChanged;

        public static string currentEnv; // Current environment (day, night, morning)
        public static string currentDescEnv; // Current environment in the description
        public bool overcloudSet = false;

        public static bool runTimer = false;
        private static Timer rwTimer; // Random weather timer
        private static Timer ocTimer; // Timer that does the Overcloud stuffs


        public static string weatherName;
        public static double weatherForecastLength;
        public static string weatherForecastTime;

        public UnityAction<bool> overcloud_changed; 
        public UnityAction<bool> overcloudRandomWeather_changed;
        public UnityAction<int> randomWeatherTimerLength_changed;

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
            

            randomWeatherTimerLength_changed += RandomWeatherTimerLength_Setting;
            modSettings.CreateCustomLabel("  Random Weather Timer Length | Default: 800");
            modSettings.CreateIntSetting("         (in seconds)", randomWeatherTimerLength_changed, settings.randomWeatherTimerLength);

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
                    weatherName = "fuzzy men in your area! Get zippity zapped now";
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
            runTimer = true;
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
                if (getScenario.environmentName == "night")
                {
                    GameSettings.SetGameSettingValue("USE_OVERCLOUD", false, true);
                }
                if (currentEnv == "day" || currentEnv == "morning")
                {
                    GameSettings.SetGameSettingValue("USE_OVERCLOUD", true, true);
                }
                else
                {
                    if (currentEnv == "night") // Night (also morningish, but it looks fine enough) has shadows from an invisible sun or something. Setting to false incase its true.
                    {
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
        public void RandomWeatherTimerLength_Setting(int newval)
        {
            settings.randomWeatherTimerLength = newval;
            settingsChanged = true;
        }



        private void SceneLoaded(VTOLScenes scene)
        {
            CheckSave();

            switch (scene)
            {
                case VTOLScenes.ReadyRoom: // Using ReadyRoom (main menu) to reset the stuffs since it's where leaving VehicleConfiguration and CustomMapBase leads.
                    StopRandomWeatherTimer();
                    StopOvercloudTimer();
                    GameSettings.SetGameSettingValue("USE_OVERCLOUD", false, true);
                    currentEnv = null;

                    break;
                case VTOLScenes.VehicleConfiguration:
                    if(PilotSaveManager.currentScenario.mapSceneName != "Akutan")
                    {
                        GameSettings.SetGameSettingValue("USE_OVERCLOUD", true, true);
                    }
                    if (settings.useOvercloud == true)
                    {
                        StartOvercloudTimer();
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

        void Update()
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
            }
        }


        private void MissionReloaded() // Honestly I just copied this from Cheeeese.
        {
            CheckSave();
        }

        private void OnApplicationQuit() // If you're ingame with overcloud enabled and quit the whole game, this should prevent it from being enabled and fucking you later.
        {
            CheckSave();
            GameSettings.SetGameSettingValue("USE_OVERCLOUD", false, true);
        }

        // The code below was yoinked from cheeeese, I don't know what it does but you can just copy it.
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