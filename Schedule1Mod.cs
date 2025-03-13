using MelonLoader;
using UnityEngine;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using System;

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
                LoggerInstance.Msg("[Startup] Instant Delivery Supplier mod initializing...");

                // Find the Supplier and ProductManager types
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "Assembly-CSharp")
                    {
                        try
                        {
                            supplierType = asm.GetType("ScheduleOne.Economy.Supplier");
                            productManagerType = asm.GetType("ScheduleOne.Product.ProductManager");

                            if (supplierType != null)
                            {
                                LoggerInstance.Msg($"[Startup] Found Supplier type: {supplierType.FullName}");
                                var methods = supplierType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                foreach (var method in methods)
                                {
                                    LoggerInstance.Msg($"[Startup] Supplier method: {method.Name}");
                                }
                            }

                            if (productManagerType != null)
                            {
                                LoggerInstance.Msg($"[Startup] Found ProductManager type: {productManagerType.FullName}");
                                var methods = productManagerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                foreach (var method in methods)
                                {
                                    LoggerInstance.Msg($"[Startup] ProductManager method: {method.Name}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggerInstance.Error($"[Startup] Error loading types: {ex.Message}");
                        }
                    }
                }

                if (supplierType == null && productManagerType == null)
                {
                    LoggerInstance.Error("[Startup] Could not find required types!");
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
                        LoggerInstance.Msg("[Startup] Found SetIsAcceptingOrder method, patching...");
                        harmony.Patch(
                            setIsAcceptingOrder,
                            prefix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(SetIsAcceptingOrderPrefix), 
                                BindingFlags.Static | BindingFlags.NonPublic))
                        );
                    }
                }

                // Try to patch Supplier if found
                if (supplierType != null)
                {
                    var placeOrder = supplierType.GetMethod("PlaceOrder", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (placeOrder != null)
                    {
                        LoggerInstance.Msg("[Startup] Found PlaceOrder method, patching...");
                        harmony.Patch(
                            placeOrder,
                            prefix: new HarmonyMethod(typeof(MainMod).GetMethod(nameof(PlaceOrderPrefix), 
                                BindingFlags.Static | BindingFlags.NonPublic))
                        );
                    }
                }

                LoggerInstance.Msg("[Startup] Initialization complete!");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"[Startup] Error during initialization: {ex}");
            }
        }

        private static bool SetIsAcceptingOrderPrefix(object __instance, bool value)
        {
            try
            {
                Instance.LoggerInstance.Msg($"[Order] SetIsAcceptingOrder called with value: {value}");
                // Always allow orders
                return false;
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"[Order] Error in SetIsAcceptingOrder: {ex}");
                return true;
            }
        }

        private static bool PlaceOrderPrefix(object __instance, int amount, ref bool __result)
        {
            try
            {
                Instance.LoggerInstance.Msg($"[Order] Attempting to place order for {amount} seeds");

                // Get the stash amount
                var getStashAmount = __instance.GetType().GetMethod("GetStashAmount", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (getStashAmount == null)
                {
                    Instance.LoggerInstance.Error("[Order] Could not find GetStashAmount method!");
                    return true;
                }

                var stashAmount = (int)getStashAmount.Invoke(__instance, null);
                Instance.LoggerInstance.Msg($"[Order] Current stash amount: {stashAmount}");

                // Check amount
                if (amount > stashAmount)
                {
                    Instance.LoggerInstance.Msg($"[Order] Order amount {amount} exceeds stash amount {stashAmount}");
                    __result = false;
                    return false;
                }

                // Find all components in the scene
                var allComponents = UnityEngine.Object.FindObjectsOfType<Component>();
                Instance.LoggerInstance.Msg($"[Order] Found {allComponents.Length} components in scene");

                // Find player
                var player = allComponents
                    .FirstOrDefault(c => c.GetType().Name == "PlayerController");
                if (player == null)
                {
                    Instance.LoggerInstance.Error("[Order] Could not find player!");
                    return true;
                }

                Instance.LoggerInstance.Msg($"[Order] Found player at {player.transform.position}");

                // Find nearest dead drop
                var deadDrops = allComponents
                    .Where(c => c.GetType().Name == "DeadDropBox")
                    .ToList();

                if (!deadDrops.Any())
                {
                    Instance.LoggerInstance.Error("[Order] No dead drops found!");
                    return true;
                }

                Instance.LoggerInstance.Msg($"[Order] Found {deadDrops.Count} dead drops");

                var nearestDeaddrop = deadDrops
                    .OrderBy(box => Vector3.Distance(box.transform.position, player.transform.position))
                    .First();

                Instance.LoggerInstance.Msg($"[Order] Found nearest dead drop at {nearestDeaddrop.transform.position}");

                // Remove from stash
                var removeFromStash = __instance.GetType().GetMethod("RemoveFromStash", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (removeFromStash == null)
                {
                    Instance.LoggerInstance.Error("[Order] Could not find RemoveFromStash method!");
                    return true;
                }

                removeFromStash.Invoke(__instance, new object[] { amount });
                Instance.LoggerInstance.Msg($"[Order] Removed {amount} from stash");
                
                // Add to nearest deaddrop
                var addSeeds = nearestDeaddrop.GetType().GetMethod("AddSeeds", 
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (addSeeds == null)
                {
                    Instance.LoggerInstance.Error("[Order] Could not find AddSeeds method!");
                    return true;
                }

                addSeeds.Invoke(nearestDeaddrop, new object[] { amount });
                Instance.LoggerInstance.Msg($"[Order] Added {amount} seeds to dead drop");
                
                __result = true;
                return false;
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"[Order] Error processing order: {ex}");
                return true;
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg($"[Scene] Loaded into scene: {sceneName}");
        }
    }
} 