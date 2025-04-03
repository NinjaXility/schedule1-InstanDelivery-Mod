using MelonLoader;
using UnityEngine;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using System;
using System.Collections.Generic;

[assembly: MelonInfo(typeof(InstantDeliverySupplier.MainMod), "Instant Delivery & Price Modifier", "1.1.0", "Kua8 On Cord")]
[assembly: MelonGame("TVGS", "Schedule I Free Sample")]

namespace InstantDeliverySupplier
{
    public class MainMod : MelonMod
    {
        public static MainMod Instance { get; private set; }
        private static Type supplierType;
        private static Type productManagerType;
        private static Type economyManagerType; // Added for debt management

        [Obsolete]
        public override void OnApplicationStart()
        {
            try
            {
                Instance = this;

                // Find the required types
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "Assembly-CSharp")
                    {
                        try
                        {
                            supplierType = asm.GetType("ScheduleOne.Economy.Supplier");
                            productManagerType = asm.GetType("ScheduleOne.Product.ProductManager");
                            economyManagerType = asm.GetType("ScheduleOne.Economy.EconomyManager"); // Added
                        }
                        catch (Exception ex)
                        {
                            Instance.LoggerInstance.Error($"Error loading types: {ex.Message}");
                        }
                    }
                }

                if (supplierType == null || productManagerType == null)
                {
                    Instance.LoggerInstance.Error("Could not find required types!");
                    return;
                }

                // Create harmony instance
                var harmony = new HarmonyLib.Harmony("com.instantdeliverysupplier");

                // Try to patch ProductManager first
                if (productManagerType != null)
                {
                    var setIsAcceptingOrder = productManagerType.GetMethod("SetIsAcceptingOrder", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (setIsAcceptingOrder != null)
                    {
                        harmony.Patch(
                            setIsAcceptingOrder,
                            prefix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(SetIsAcceptingOrderPrefix), 
                                BindingFlags.Static | BindingFlags.NonPublic))
                        );
                    }
                }

                // Patch Supplier methods
                if (supplierType != null)
                {
                    // Patch DeaddropConfirmed to set totalPrice to 0
                    var deaddropConfirmed = supplierType.GetMethod("DeaddropConfirmed", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (deaddropConfirmed != null)
                    {
                        harmony.Patch(
                            deaddropConfirmed,
                            prefix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(DeaddropConfirmedPrefix), 
                                BindingFlags.Static | BindingFlags.NonPublic)),
                            postfix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(DeaddropConfirmedPostfix),
                                BindingFlags.Static | BindingFlags.NonPublic))
                        );
                    }

                    // Patch SetDeaddrop
                    var setDeaddrop = supplierType.GetMethod("SetDeaddrop", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (setDeaddrop != null)
                    {
                        harmony.Patch(
                            setDeaddrop,
                            prefix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(SetDeaddropPrefix), 
                                BindingFlags.Static | BindingFlags.NonPublic))
                        );
                    }

                    // Patch GetDeadDropLimit to remove limits
                    var getDeadDropLimit = supplierType.GetMethod("GetDeadDropLimit", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (getDeadDropLimit != null)
                    {
                        harmony.Patch(
                            getDeadDropLimit,
                            prefix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(GetDeadDropLimitPrefix), 
                                BindingFlags.Static | BindingFlags.NonPublic))
                        );
                    }

                    // Also patch minsUntilDeaddropReady property
                    var setMinsUntilReady = supplierType.GetMethod("set_minsUntilDeaddropReady",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (setMinsUntilReady != null)
                    {
                        harmony.Patch(
                            setMinsUntilReady,
                            prefix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(SetMinsUntilDeaddropReadyPrefix),
                                BindingFlags.Static | BindingFlags.NonPublic))
                        );
                    }
                }

                // Patch EconomyManager for debt management
                if (economyManagerType != null)
                {
                    // Patch ChangeDebt method
                    var changeDebt = economyManagerType.GetMethod("ChangeDebt", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (changeDebt != null)
                    {
                        harmony.Patch(
                            changeDebt,
                            prefix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(ChangeDebtPrefix), 
                                BindingFlags.Static | BindingFlags.NonPublic))
                        );
                    }

                    // Find and patch SetCashAmount to ensure we always have money (optional)
                    var setCash = economyManagerType.GetMethod("SetCashAmount", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (setCash != null)
                    {
                        harmony.Patch(
                            setCash,
                            postfix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(SetCashAmountPostfix), 
                                BindingFlags.Static | BindingFlags.NonPublic))
                        );
                    }
                }

                Instance.LoggerInstance.Msg("Instant Delivery & Price Modifier initialized successfully!");
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Error during initialization: {ex}");
            }
        }

        private static bool SetIsAcceptingOrderPrefix(object __instance, bool accepting)
        {
            try
            {
                return false; // Always allow orders
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Error in SetIsAcceptingOrder: {ex}");
                return true;
            }
        }

        private static bool DeaddropConfirmedPrefix(object __instance, object cart, ref float totalPrice)
        {
            try
            {
                // Set price to 0 (free orders)
                totalPrice = 0;
                return true; // Let the original method run with our modified price
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Error in DeaddropConfirmed: {ex}");
                return true;
            }
        }

        private static void DeaddropConfirmedPostfix(object __instance)
        {
            try
            {
                // Use the setter method directly instead of property
                var setterMethod = __instance.GetType().GetMethod("set_minsUntilDeaddropReady",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (setterMethod != null)
                {
                    setterMethod.Invoke(__instance, new object[] { 0 });
                }
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Error in DeaddropConfirmedPostfix: {ex}");
            }
        }

        private static bool SetDeaddropPrefix(object __instance, object[] items, ref int minsUntilReady)
        {
            try
            {
                minsUntilReady = 0;
                return true; // Continue with original method with modified delay
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Error in SetDeaddrop: {ex}");
                return true;
            }
        }

        private static bool GetDeadDropLimitPrefix(object __instance, ref float __result)
        {
            try
            {
                __result = float.MaxValue; // Set a very high limit
                return false; // Skip original method
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Error in GetDeadDropLimit: {ex}");
                return true;
            }
        }

        private static bool SetMinsUntilDeaddropReadyPrefix(object __instance, ref int value)
        {
            try
            {
                value = 0;
                return true; // Continue with original method with modified value
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Error in SetMinsUntilDeaddropReady: {ex}");
                return true;
            }
        }

        // New method to handle ChangeDebt
        private static bool ChangeDebtPrefix(object __instance, ref float amount)
        {
            try
            {
                // Always set debt change to 0
                amount = 0;
                
                // Additionally try to reset total debt to 0
                var debtField = economyManagerType.GetField("_debt", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (debtField != null)
                {
                    debtField.SetValue(__instance, 0f);
                }
                
                return true; // Continue with original method but with modified values
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Error in ChangeDebt: {ex}");
                return true;
            }
        }

        // Optional method to ensure we always have money
        private static void SetCashAmountPostfix(object __instance)
        {
            try
            {
                // Get the current cash amount
                var cashField = economyManagerType.GetField("_cash", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (cashField != null)
                {
                    // Ensure we have a high amount of cash
                    float currentCash = (float)cashField.GetValue(__instance);
                    if (currentCash < 10000f)
                    {
                        cashField.SetValue(__instance, 10000f);
                    }
                }
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Error in SetCashAmount: {ex}");
            }
        }

        public override void OnUpdate()
        {
            // You can add a hotkey to reset debt here if needed
            // Example: if (Input.GetKeyDown(KeyCode.F10)) { ResetDebt(); }
        }

        // Optional helper method to reset debt on demand
        private void ResetDebt()
        {
            try
            {
                // Find the EconomyManager instance
                var economyManagerInstance = FindEconomyManager();
                if (economyManagerInstance != null)
                {
                    // Set the _debt field to 0
                    var debtField = economyManagerType.GetField("_debt", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (debtField != null)
                    {
                        debtField.SetValue(economyManagerInstance, 0f);
                        Instance.LoggerInstance.Msg("Debt reset to 0!");
                    }
                }
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Error resetting debt: {ex}");
            }
        }

        // Helper method to find the EconomyManager instance
        private object FindEconomyManager()
        {
            try
            {
                // Try to get a static instance field/property first
                var instanceField = economyManagerType.GetField("Instance", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (instanceField != null)
                {
                    return instanceField.GetValue(null);
                }

                var instanceProperty = economyManagerType.GetProperty("Instance", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (instanceProperty != null)
                {
                    return instanceProperty.GetValue(null);
                }

                // If no static instance, try finding it in the scene
                var findObjectsMethod = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", 
                    new Type[] { typeof(Type) });
                if (findObjectsMethod != null)
                {
                    var managers = (UnityEngine.Object[])findObjectsMethod.Invoke(null, new object[] { economyManagerType });
                    if (managers != null && managers.Length > 0)
                    {
                        return managers[0];
                    }
                }
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Error finding EconomyManager: {ex}");
            }

            return null;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // No logging needed
        }
    }
}