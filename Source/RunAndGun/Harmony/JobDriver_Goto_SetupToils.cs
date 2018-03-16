﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;
using Verse.AI;
using RimWorld;


namespace RunAndGun.Harmony
{
    [HarmonyPatch(typeof(JobDriver), "SetupToils")]
    static class JobDriver_SetupToils
    {
        static void Postfix(JobDriver __instance)
        {
            if(!(__instance is JobDriver_Goto))
            {
                return;
            }
            JobDriver_Goto jobDriver = (JobDriver_Goto)__instance;
            List<Toil> toils = Traverse.Create(jobDriver).Field("toils").GetValue<List<Toil>>();
            if (toils.Count() > 0)
            {

                Toil toil = toils.ElementAt(0);
                toil.AddPreTickAction(delegate
                {
                    if (jobDriver.pawn != null && !jobDriver.pawn.IsBurning() && (jobDriver.pawn.Drafted || !jobDriver.pawn.IsColonist) && !jobDriver.pawn.Downed)
                    {
                        checkForAutoAttack(jobDriver);
                    }
                });

            }
        }
        static void checkForAutoAttack(JobDriver_Goto __instance)
        {
            if ((__instance.pawn.story == null || !__instance.pawn.story.WorkTagIsDisabled(WorkTags.Violent))
                && __instance.pawn.Faction != null
                && !(__instance.pawn.stances.curStance is Stance_RunAndGun)
                && __instance.pawn.jobs.curJob.def == JobDefOf.Goto
                && (__instance.pawn.drafter == null || __instance.pawn.drafter.FireAtWill))
            {
                CompRunAndGun comp = __instance.pawn.TryGetComp<CompRunAndGun>();
                if (comp == null || comp.isEnabled == false)
                {
                    return;
                }
                Verb verb = __instance.pawn.TryGetAttackVerb(true);
                if (verb != null && !verb.verbProps.MeleeRange)
                {
                    TargetScanFlags targetScanFlags = TargetScanFlags.NeedLOSToPawns | TargetScanFlags.NeedLOSToNonPawns | TargetScanFlags.NeedThreat;
                    if (verb.IsIncendiary())
                    {
                        targetScanFlags |= TargetScanFlags.NeedNonBurning;
                    }
                    Thing thing = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(__instance.pawn, null, verb.verbProps.range, verb.verbProps.minRange, targetScanFlags);
                    if (thing != null)
                    {
                        __instance.pawn.TryStartAttack(thing);
                        return;
                    }
                }
            }
        }

    }

}
