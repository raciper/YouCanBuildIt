using ColossalFramework.Plugins;
using ICities;
using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace YouCanBuildIt
{
    //Attach Settings to Save Game
    public class YouCanBuildItGameSettings : SerializableDataExtensionBase
    {
        public const string MOD_NAME = "YouCanBuildIt";

        // Try to get settings from GameData at load
        public override void OnLoadData()
        {
            base.OnLoadData();

            try
            {
                byte[] byteArray = serializableDataManager.LoadData(MOD_NAME);
                if (byteArray == null || byteArray.Length == 0)
                {
                    throw new Exception("No Existing YouCanBuildIt Settings in Game Data, using Player Preferences.");
                }
                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream stream = new MemoryStream(byteArray))
                {
                    Settings settings = (Settings)formatter.Deserialize(stream);
                    YouCanBuildItMod.ChargeInterest = settings.ChargeInterest;
                    YouCanBuildItMod.AnnualRate = settings.AnnualRate;
                }
            }
            catch // If something goes wrong load the defaults
            {
                YouCanBuildItMod.LoadPlayerPrefs();  
            }

        }

        public override void OnSaveData()
        {
            base.OnSaveData();
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                Settings settings = new Settings();
                settings.ChargeInterest = YouCanBuildItMod.ChargeInterest;
                settings.AnnualRate = YouCanBuildItMod.AnnualRate;

                formatter.Serialize(stream, settings);
                byte[] byteArray = stream.ToArray();
                serializableDataManager.SaveData(MOD_NAME, byteArray);
            }
        }

        [Serializable]
        public class Settings
        {
            public bool ChargeInterest;
            public float AnnualRate;
        }
    }
}
