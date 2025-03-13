using MelonLoader;
using UnityEngine;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using System;
using System.Collections.Generic;

[assembly: MelonInfo(typeof(InstantDeliverySupplier.MainMod), "Instant Delivery Supplier", "1.0.0", "Kua8 On Cord")]
[assembly: MelonGame("TVGS", "Schedule I Free Sample")]

namespace InstantDeliverySupplier
{
    public class MainMod : MelonMod
    {
        public static MainMod Instance { get; private set; }
        private static Type supplierType;
        private static Type productManagerType;

        [Obsolete]
        public override void OnApplicationStart()
        {
            try
            {
                Instance = this;

                // Find the Supplier and ProductManager types
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "Assembly-CSharp")
                    {
                        try
                        {
                            supplierType = asm.GetType("ScheduleOne.Economy.Supplier");
                            productManagerType = asm.GetType("ScheduleOne.Product.ProductManager");
                        }
                        catch (Exception ex)
                        {
                            Instance.LoggerInstance.Error($"Error loading types: {ex.Message}");
                        }
                    }
                }

                if (supplierType == null && productManagerType == null)
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

                // Try to patch Supplier methods
                if (supplierType != null)
                {
                    // Patch DeaddropConfirmed
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

        private static bool DeaddropConfirmedPrefix(object __instance, object cart, float totalPrice)
        {
            try
            {
                return true; // Let the original method run
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

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // No logging needed
        }
    }
} 