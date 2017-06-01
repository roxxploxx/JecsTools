﻿using Harmony;
using RimWorld;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace CompInstalledPart
{
    [StaticConstructorOnStartup]
    static class HarmonyCompInstalledPart
    {
        static HarmonyCompInstalledPart()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.jecrell.comps.installedpart");
            harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), null, new HarmonyMethod(typeof(HarmonyCompInstalledPart), "AddHumanlikeOrders_PostFix"));
            harmony.Patch(AccessTools.Method(typeof(ITab_Pawn_Gear), "InterfaceDrop"), new HarmonyMethod(typeof(HarmonyCompInstalledPart).GetMethod("InterfaceDrop_PreFix")), null);
            harmony.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), "TryDropEquipment"), new HarmonyMethod(typeof(HarmonyCompInstalledPart), "TryDropEquipment_PreFix"), null);
        }

        // Verse.Pawn_EquipmentTracker
        public static bool TryDropEquipment_PreFix(ThingWithComps eq, out ThingWithComps resultingEq, ref bool __result)
        {
            resultingEq = null;
            return __result = (!(!eq?.TryGetComp<CompInstalledPart>()?.uninstalled) ?? true);
        }

        // RimWorld.FloatMenuMakerMap
        public static void AddHumanlikeOrders_PostFix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);
            foreach (Thing current in c.GetThingList(pawn.Map))
            {
                //Handler for things on the ground
                if (current is ThingWithComps groundThing)
                {
                    if (groundThing != null && pawn != null && pawn != groundThing)
                    {
                        CompInstalledPart groundPart = groundThing.GetComp<CompInstalledPart>();
                        if (groundPart != null)
                        {
                            string text = "CompInstalledPart_Install".Translate();
                            opts.Add(new FloatMenuOption(text, delegate
                            {
                                CompProperties_InstalledPart props = groundPart.Props;
                                if (props != null)
                                {
                                    if (props.allowedToInstallOn != null && props.allowedToInstallOn.Count > 0)
                                    {
                                        SoundDefOf.TickTiny.PlayOneShotOnCamera(null);
                                        Find.Targeter.BeginTargeting(new TargetingParameters
                                        {
                                            canTargetPawns = true,
                                            canTargetBuildings = true,
                                            mapObjectTargetsMustBeAutoAttackable = false,
                                            validator = delegate (TargetInfo targ)
                                            {
                                                if (!targ.HasThing)
                                                {
                                                    return false;
                                                }
                                                return props.allowedToInstallOn.Contains(targ.Thing.def);
                                            }
                                        }, delegate (LocalTargetInfo target)
                                        {
                                            groundPart.GiveInstallJob(pawn, target.Thing);
                                        }, null, null, null);
                                    }
                                    else
                                    {
                                        Log.ErrorOnce("CompInstalledPart :: allowedToInstallOn list needs to be defined in XML.", 3242);
                                    }
                                }
                            }, MenuOptionPriority.Default, null, null, 29f, null, null));
                        }
                    }
                }

                //Handler character with installed parts
                if (current is Pawn targetPawn)
                {
                    if (targetPawn != null && pawn != null && pawn != targetPawn)
                    {
                        //Handle installed weapons
                        if (targetPawn.equipment != null)
                        {
                            if (targetPawn.equipment.Primary != null)
                            {
                                CompInstalledPart installedEq = targetPawn.equipment.Primary.GetComp<CompInstalledPart>();
                                if (installedEq != null)
                                {
                                    string text = "CompInstalledPart_Uninstall".Translate(targetPawn.equipment.Primary.LabelShort);
                                    opts.Add(new FloatMenuOption(text, delegate
                                    {
                                        SoundDefOf.TickTiny.PlayOneShotOnCamera(null);
                                        installedEq.GiveUninstallJob(pawn, targetPawn);
                                    }, MenuOptionPriority.Default, null, null, 29f, null, null));
                                }
                            }
                        }

                        //Handle installed apparel
                        if (targetPawn.apparel != null)
                        {
                            if (targetPawn.apparel.WornApparel != null && targetPawn.apparel.WornApparelCount > 0)
                            {
                                List<Apparel> installedApparel = targetPawn.apparel.WornApparel.FindAll((x) => x.GetComp<CompInstalledPart>() != null);
                                if (installedApparel != null && installedApparel.Count > 0)
                                {
                                    foreach (Apparel ap in installedApparel)
                                    {
                                        string text = "CompInstalledPart_Uninstall".Translate(ap.LabelShort);
                                        opts.Add(new FloatMenuOption(text, delegate
                                        {
                                            SoundDefOf.TickTiny.PlayOneShotOnCamera(null);
                                            ap.GetComp<CompInstalledPart>().GiveUninstallJob(pawn, targetPawn);
                                        }, MenuOptionPriority.Default, null, null, 29f, null, null));
                                    }

                                }
                            }
                        }
                    }
                }
            }
        }

        // RimWorld.Pawn_ApparelTracker
        public static bool InterfaceDrop_PreFix(ITab_Pawn_Gear __instance, Thing t)
        {
            ThingWithComps thingWithComps = t as ThingWithComps;
            Apparel apparel = t as Apparel;
            Pawn __pawn = (Pawn)AccessTools.Method(typeof(ITab_Pawn_Gear), "get_SelPawnForGear").Invoke(__instance, new object[0]);
            if (__pawn != null)
            {
                if (apparel != null)
                {
                    if (__pawn.apparel != null)
                    {
                        if (__pawn.apparel.WornApparel.Contains(apparel))
                        {
                            if (__pawn.apparel.WornApparel != null)
                            {
                                CompInstalledPart installedPart = apparel.GetComp<CompInstalledPart>();
                                if (installedPart != null)
                                {
                                    if (!installedPart.uninstalled)
                                        return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }
        
    }
}
