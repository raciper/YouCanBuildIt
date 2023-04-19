using System;
//using HarmonyLib;
using ICities;
using UnityEngine;
using ColossalFramework;
//using CitiesHarmony.API;
using ColossalFramework.UI;
using static UnityStandardAssets.CinematicEffects.TemporalAntiAliasing;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using ColossalFramework.Plugins;

namespace YouCanBuildIt
{
    /// <summary>
    /// The You Can Build It Mod for Cities Skylines
    /// 
    /// Never stops the player from building even when at a negative balance.   
    /// Negative players are charged interest on their negative balance on a weekly basis.   
    /// The interest rate is 0 to 36% based on the settings in the control panel.  This can be turned off.
    /// </summary>
    public class YouCanBuildItMod : IUserMod
    {
        public static string ModName = "YouCanBuildit";
        public string Name => "You Can Build It";
        public string Description => "Allows you to build building even if you cannot affort it.";
        public const string ANNUAL_RATE = "CanYouBuildIt.AnnualRate";
        public const string CHARGE_INTEREST = "CanYouBuildIt.ChargeInterest";
        private static float? _annualRate;
        private static int? _chargeInterest;

        public static bool  ChargeInterest 
        { 
            get => (_chargeInterest.GetValueOrDefault(1) == 1); 
            set => _chargeInterest = (value ? 1 : 0);

        }
        public static float AnnualRate
        { 
            get => _annualRate.GetValueOrDefault(0.03f); 
            set => _annualRate = value;
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            if (!_chargeInterest.HasValue) // If No Value has been set, then first entry
            {
                LoadPlayerPrefs();         // Load Player Defaults
            }

            // determine if in Game or in Start Menu
            bool inGame = Singleton<SimulationManager>.instance.SimulationPaused;

            // Create a slider and label for the mod options
            UIHelperBase group = (UIHelperBase)helper.AddGroup("You Can Build It - " + (inGame ? "Game" : "Default") + " Interest Charges");
            UICheckBox label = (UICheckBox)group.AddCheckbox(rateText, ChargeInterest, OnChargeInterestChanged);
            UISlider slider = (UISlider)group.AddSlider("Set Annual Interest Rate", 0.0f, 0.36f, 0.0025f, AnnualRate, OnSliderValueChanged);
            UIButton resetButton = (UIButton)group.AddButton("Reset to 3%", () => { slider.value = 0.03f; });
            UIHelperBase version = (UIHelperBase)helper.AddGroup("You Can Build It, Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + ", Copyright 2023");

            // Save the Interest Rate Saving.
            void OnChargeInterestChanged(bool value)
            {
                ChargeInterest = value;
                if (!inGame)
                {
                    PlayerPrefs.SetInt(CHARGE_INTEREST, ChargeInterest ? 1 : 0);
                    PlayerPrefs.Save();
                }
            }

            // Update the label text when the slider value changes
            void OnSliderValueChanged(float value)
            {
                AnnualRate = value;
                label.text = rateText;
                if (!inGame)
                {
                    PlayerPrefs.SetFloat(ANNUAL_RATE, AnnualRate);
                    PlayerPrefs.Save();
                }
            }
        }

        public static void LoadPlayerPrefs()
        {
            _chargeInterest = PlayerPrefs.GetInt(CHARGE_INTEREST, 1);
            _annualRate = PlayerPrefs.GetFloat(ANNUAL_RATE, 0.03f);
        }
                
        private string rateText
        {
            get => $"Charge Interest on Negative Balances at {AnnualRate.ToString("P2")}";
        }
    }


    public class BlankCheck : EconomyExtensionBase
    {
        public override int OnPeekResource(EconomyResource resource, int amount)
        {
            EconomyManager manager = Singleton<EconomyManager>.instance;
            manager.m_properties.m_bailoutLimit = int.MinValue;           // Set Bailout to Max to prevent Bankruptcy
            return amount;                                                // Always allow to build
        }
        public override bool OverrideDefaultPeekResource => true;
    }
    
    // Charge Interest once a week
    public class ChargeInterest : ThreadingExtensionBase
    {
        static DateTime next_payment = DateTime.MinValue;

        public override void OnBeforeSimulationFrame()
        {
            if (!YouCanBuildItMod.ChargeInterest) return;
            DateTime game_datetime = Singleton<SimulationManager>.instance.m_currentGameTime;

            if (game_datetime.Ticks > next_payment.Ticks)
            {
                long balance = EconomyManager.playerMoney;

                // If Balance is below 0 charge interest
                if (balance < 0)
                {
                    int fee = (int)Math.Ceiling(balance * 100 * YouCanBuildItMod.AnnualRate / 52.143);
                    EconomyManager.instance.FetchResource(EconomyManager.Resource.LoanPayment, -fee, ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Level.None);
                }
                next_payment = game_datetime.AddDays(7);

            }
            base.OnBeforeSimulationFrame();
        }
        
    }

}