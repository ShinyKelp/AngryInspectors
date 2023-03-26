using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using MoreSlugcats;
using UnityEngine;
using RWCustom;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace CustomRegionQuests
{

    [BepInPlugin("ShinyKelp.CustomRelationships", "AngryInspectors", "1.0.0")]
    public partial class CustomRelationshipsMod : BaseUnityPlugin
    {
        private void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
        }

        private bool IsInit;
        private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                if (IsInit) return;

                //Your hooks go here
                //On.RainWorldGame.ShutDownProcess += RainWorldGameOnShutDownProcess;
                On.StaticWorld.InitStaticWorld += ModifyInspectorRelationships;
                On.MoreSlugcats.InspectorAI.IUseARelationshipTracker_UpdateDynamicRelationship += InspectorAI_IUseARelationshipTracker_UpdateDynamicRelationship;
                On.Spear.HitSomething += Spear_HitSomething;
                IsInit = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        private bool Spear_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            if(self.thrownBy.abstractCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.Inspector &&
                result.obj is Creature creature && creature.abstractCreature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.DaddyLongLegs)
            {
                float damage = 4f;
                if (creature.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.DaddyLongLegs)
                    damage -= 2f;
                if (creature.abstractCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs)
                    damage += 4f;
                creature.Violence(self.firstChunk, self.firstChunk.vel, creature.firstChunk, null, Creature.DamageType.Stab, damage, 1f);
            }
            return orig(self, result, eu);
        }

        private CreatureTemplate.Relationship InspectorAI_IUseARelationshipTracker_UpdateDynamicRelationship(On.MoreSlugcats.InspectorAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, InspectorAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            if (dRelation.trackerRep.representedCreature is AbstractCreature creature &&
                StaticWorld.creatureTemplates[MoreSlugcatsEnums.CreatureTemplateType.Inspector.Index].relationships[creature.creatureTemplate.type.Index].type == CreatureTemplate.Relationship.Type.Attacks)
            {
                if (creature.realizedCreature.dead)
                {
                    bool isEats = dRelation.currentRelationship.type == CreatureTemplate.Relationship.Type.Eats;
                    if (isEats)
                    {
                        dRelation.currentRelationship.type = CreatureTemplate.Relationship.Type.Ignores;
                        bool itHasPrey = false;
                        foreach (PreyTracker.TrackedPrey prey in self.preyTracker.prey)
                        {
                            if (prey.critRep == dRelation.trackerRep)
                            {
                                itHasPrey = true;
                            }
                        }
                        if (itHasPrey)
                        {
                            self.preyTracker.ForgetPrey(dRelation.trackerRep.representedCreature);
                        }
                    }                    
                }
                else
                {
                    if(StaticWorld.creatureTemplates[MoreSlugcatsEnums.CreatureTemplateType.Inspector.Index].relationships[creature.creatureTemplate.type.Index].intensity >= 1f || UnityEngine.Random.value < 0.001f)
                    {
                        bool itHasPrey = false;
                        foreach (PreyTracker.TrackedPrey prey in self.preyTracker.prey)
                        {
                            if (prey.critRep == dRelation.trackerRep)
                            {
                                itHasPrey = true;
                                break;
                            }
                        }

                        if (!itHasPrey)
                        {
                            self.preyTracker.AddPrey(dRelation.trackerRep);
                            dRelation.currentRelationship.type = CreatureTemplate.Relationship.Type.Eats;
                        }
                    }
                }
            }
            if (dRelation.trackerRep.representedCreature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.DaddyLongLegs)
                return InspectorAI_UpdateDynamicRelationshipAlternate(self, dRelation);
            else return orig(self, dRelation);
        }


        private CreatureTemplate.Relationship InspectorAI_UpdateDynamicRelationshipAlternate(InspectorAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            CreatureTemplate.Relationship currentRelationship = dRelation.currentRelationship;
            if (dRelation.trackerRep is Tracker.SimpleCreatureRepresentation)
            {
                return currentRelationship;
            }
            if (self.preyTracker.MostAttractivePrey != null && self.preyTracker.MostAttractivePrey.representedCreature == dRelation.trackerRep.representedCreature && currentRelationship.type == CreatureTemplate.Relationship.Type.Uncomfortable)
            {
                currentRelationship.type = CreatureTemplate.Relationship.Type.Eats;
                currentRelationship.intensity = 1f;
            }
            if (currentRelationship.type == CreatureTemplate.Relationship.Type.Uncomfortable || dRelation.trackerRep.representedCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.Inspector)
            {
                if (dRelation.trackerRep.VisualContact)
                {
                    if (dRelation.trackerRep.representedCreature.realizedCreature != null && !dRelation.trackerRep.representedCreature.realizedCreature.dead)
                    {
                        Creature realizedCreature = dRelation.trackerRep.representedCreature.realizedCreature;
                        if (realizedCreature.grasps != null && realizedCreature.grasps.Length != 0)
                        {
                            for (int i = 0; i < realizedCreature.grasps.Length; i++)
                            {
                                if (realizedCreature.grasps[i] != null && realizedCreature.grasps[i].grabbed is SSOracleSwarmer)
                                {
                                    if ((realizedCreature.grasps[i].grabbed as SSOracleSwarmer).bites < 3)
                                    {
                                        currentRelationship.type = CreatureTemplate.Relationship.Type.Eats;
                                        currentRelationship.intensity = 1f;
                                        self.preyTracker.AddPrey(dRelation.trackerRep);
                                    }
                                    else
                                    {
                                        currentRelationship.intensity = 1f;
                                        if (!self.myInspector.safariControlled)
                                        {
                                            self.myInspector.anger += 0.09f;
                                            self.OrderAHeadToGrabObject(realizedCreature.grasps[i].grabbed);
                                        }
                                    }
                                }
                            }
                        }
                        if (!self.myInspector.safariControlled && dRelation.trackerRep.representedCreature.creatureTemplate.type != MoreSlugcatsEnums.CreatureTemplateType.Inspector && self.myInspector.DangerousThrowLocations.Count > 0 && UnityEngine.Random.value < 0.1f)
                        {
                            foreach (Vector2 b in self.myInspector.DangerousThrowLocations)
                            {
                                float num = Vector2.Distance(dRelation.trackerRep.representedCreature.realizedCreature.firstChunk.pos, b);
                                if (dRelation.trackerRep.representedCreature.realizedCreature.firstChunk.vel.magnitude > 10f * Mathf.InverseLerp(150f, 600f, num) && num < 230f && Vector2.Distance(dRelation.trackerRep.representedCreature.realizedCreature.firstChunk.pos + dRelation.trackerRep.representedCreature.realizedCreature.firstChunk.vel, b) < num && num < Vector2.Distance(self.myInspector.mainBodyChunk.pos, b))
                                {
                                    self.OrderAHeadToGrabObject(dRelation.trackerRep.representedCreature.realizedCreature);
                                }
                            }
                        }
                        currentRelationship.intensity = 1f;
                    }
                    if (currentRelationship.intensity < 0.5f && UnityEngine.Random.value < 0.02f)
                    {
                        currentRelationship.intensity = 1f;
                    }
                    if (Vector2.Distance(self.myInspector.mainBodyChunk.pos, dRelation.trackerRep.lastSeenCoord.Tile.ToVector2() * 20f) < 100f && currentRelationship.intensity + 0.09f < 1f)
                    {
                        currentRelationship.intensity += 0.09f;
                    }
                }
                if (currentRelationship.intensity > 0f && dRelation.trackerRep.VisualContact)
                {
                    currentRelationship.intensity -= 0.006f;
                }
                if (currentRelationship.intensity > 0f && !dRelation.trackerRep.VisualContact)
                {
                    currentRelationship.intensity -= 0.01f;
                }
                if (currentRelationship.intensity < 0f)
                {
                    currentRelationship.intensity = 0f;
                }
            }
            else if (currentRelationship.type == CreatureTemplate.Relationship.Type.Attacks && dRelation.trackerRep.VisualContact)
            {
                if (self.myInspector.activeEye != -1 && !self.myInspector.safariControlled && Vector2.Distance(self.myInspector.heads[self.myInspector.activeEye].Tip.pos, dRelation.trackerRep.lastSeenCoord.Tile.ToVector2() * 20f) < 500f)
                {
                    self.myInspector.anger += 0.19f;
                    if (self.myInspector.anger > 1f)
                    {
                        if (self.myInspector.anger > 2f)
                        {
                            self.myInspector.anger = 2f;
                        }
                        currentRelationship.intensity += 0.08f;
                        if (currentRelationship.intensity > 1f)
                        {
                            currentRelationship.intensity = 1f;
                            self.behavior = InspectorAI.Behavior.EscapeRain;
                            self.newIdlePosCounter = UnityEngine.Random.Range(300, 400);
                        }
                    }
                }
                else if (!self.myInspector.safariControlled)
                {
                    self.myInspector.anger -= 0.001f;
                    if (self.myInspector.anger < 0f)
                    {
                        self.myInspector.anger = 0f;
                    }
                }
            }
            else if (currentRelationship.type == CreatureTemplate.Relationship.Type.Eats && dRelation.trackerRep.VisualContact)
            {
                if (dRelation.trackerRep.representedCreature.realizedCreature.dead && self.myInspector.anger <= 0f)
                {
                    self.preyTracker.ForgetPrey(dRelation.trackerRep.representedCreature);
                    currentRelationship.type = CreatureTemplate.Relationship.Type.Uncomfortable;
                    currentRelationship.intensity = 1f;
                }
                if (!self.myInspector.abstractCreature.controlled)
                {
                    self.myInspector.anger += 0.19f;
                }
                if (self.myInspector.anger > 1f)
                {
                    self.myInspector.anger = 1f;
                }
                currentRelationship.intensity += 0.05f;
                if (currentRelationship.intensity >= 1f)
                {
                    Creature realizedCreature2 = dRelation.trackerRep.representedCreature.realizedCreature;
                    if (true)//REMOVED LONGLEGS CHECK
                    {
                        if (currentRelationship.intensity >= 1f)
                        {
                            int num2 = -1;
                            for (int j = 0; j < Inspector.headCount(); j++)
                            {
                                if (!self.myInspector.HeadsCrippled(j) && self.myInspector.headGrabChunk[j] != null && self.myInspector.headGrabChunk[j].owner == realizedCreature2)
                                {
                                    num2 = j;
                                    break;
                                }
                            }
                            if (num2 == -1)
                            {
                                if (!self.myInspector.safariControlled)
                                {
                                    self.OrderAHeadToGrabObject(realizedCreature2);
                                }
                            }
                            else
                            {
                                if (self.myInspector.heads[num2].Tip.vel.magnitude < 1f || self.myInspector.heads[num2].Tip.vel.magnitude > 4f)
                                {
                                    self.myInspector.heads[num2].Tip.vel *= 1.2f;
                                    self.myInspector.heads[num2].Tip.vel += new Vector2((float)UnityEngine.Random.Range(-18, 18), (float)UnityEngine.Random.Range(-18, 18));
                                    realizedCreature2.firstChunk.pos = self.myInspector.heads[num2].Tip.pos;
                                    realizedCreature2.firstChunk.vel = self.myInspector.heads[num2].Tip.vel;
                                }
                                float target = Custom.VecToDeg(Custom.DirVec(self.myInspector.mainBodyChunk.pos, realizedCreature2.firstChunk.pos));
                                bool flag = false;
                                float num3 = 2000f;
                                for (int k = 0; k < self.myInspector.DangerousThrowLocations.Count; k++)
                                {
                                    Vector2 vector = Vector2.Lerp(realizedCreature2.firstChunk.pos, self.myInspector.DangerousThrowLocations[k], 0.8f);
                                    if (UnityEngine.Random.value < 0.85f && Vector2.Distance(self.myInspector.mainBodyChunk.pos, self.myInspector.DangerousThrowLocations[k]) < num3 && self.myInspector.room.RayTraceTilesForTerrain((int)(realizedCreature2.firstChunk.pos.x / 20f), (int)(realizedCreature2.firstChunk.pos.y / 20f), (int)(vector.x / 20f), (int)(vector.y / 20f)))
                                    {
                                        num3 = Vector2.Distance(self.myInspector.mainBodyChunk.pos, self.myInspector.DangerousThrowLocations[k]);
                                        flag = true;
                                        target = Custom.VecToDeg(Custom.DirVec(realizedCreature2.firstChunk.pos, self.myInspector.DangerousThrowLocations[k]));
                                    }
                                }
                                float num4 = 35f;
                                if (flag)
                                {
                                    num4 = 10f;
                                }
                                if (!flag && !self.myInspector.room.RayTraceTilesForTerrain((int)(realizedCreature2.firstChunk.pos.x / 20f), (int)(realizedCreature2.firstChunk.pos.y / 20f), (int)(realizedCreature2.firstChunk.pos.x + realizedCreature2.firstChunk.vel.x * 3f / 20f), (int)(realizedCreature2.firstChunk.pos.y + realizedCreature2.firstChunk.vel.y * 3f / 20f)) && (realizedCreature2.firstChunk.vel.magnitude > 50f || (UnityEngine.Random.value < 0.5f && Mathf.DeltaAngle(Custom.VecToDeg(realizedCreature2.firstChunk.vel), target) < num4 && realizedCreature2.firstChunk.vel.magnitude > 30f)))
                                {
                                    currentRelationship.intensity = 0f;
                                    self.myInspector.headWantToGrabChunk[num2] = null;
                                    self.myInspector.headGrabChunk[num2] = null;
                                    self.myInspector.room.PlaySound(SoundID.Vulture_Peck, self.myInspector.heads[num2].Tip.pos);
                                }
                                else if (Mathf.DeltaAngle(Custom.VecToDeg(realizedCreature2.firstChunk.vel), target) < num4 && realizedCreature2.firstChunk.vel.magnitude > 20f)
                                {
                                    currentRelationship.intensity = 0f;
                                    self.myInspector.headWantToGrabChunk[num2] = null;
                                    self.myInspector.headGrabChunk[num2] = null;
                                    self.myInspector.room.PlaySound(SoundID.Vulture_Peck, self.myInspector.heads[num2].Tip.pos);
                                }
                            }
                        }
                    }
                    else
                    {
                        currentRelationship.intensity += 0.03f;
                        if (currentRelationship.intensity > 1f)
                        {
                            currentRelationship.intensity = 1f;
                        }
                    }
                }
            }
            return currentRelationship;
        }

        private void ModifyInspectorRelationships(On.StaticWorld.orig_InitStaticWorld orig)
        {
            orig();

            //Inspector relationships

            CreatureTemplate.Relationship[] inspectorRels = StaticWorld.creatureTemplates[MoreSlugcatsEnums.CreatureTemplateType.Inspector.Index].relationships;

            inspectorRels[CreatureTemplate.Type.BrotherLongLegs.Index].type = CreatureTemplate.Relationship.Type.Attacks;
            inspectorRels[CreatureTemplate.Type.BrotherLongLegs.Index].intensity = 1f;
            inspectorRels[CreatureTemplate.Type.DaddyLongLegs.Index].type = CreatureTemplate.Relationship.Type.Attacks;
            inspectorRels[CreatureTemplate.Type.DaddyLongLegs.Index].intensity = 1f;
            inspectorRels[MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs.Index].type = CreatureTemplate.Relationship.Type.Attacks;
            inspectorRels[MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs.Index].intensity = 1f;

            inspectorRels[CreatureTemplate.Type.MirosBird.Index].type = CreatureTemplate.Relationship.Type.Attacks;
            inspectorRels[CreatureTemplate.Type.MirosBird.Index].intensity = 1f;
            inspectorRels[MoreSlugcatsEnums.CreatureTemplateType.MirosVulture.Index].type = CreatureTemplate.Relationship.Type.Attacks;
            inspectorRels[MoreSlugcatsEnums.CreatureTemplateType.MirosVulture.Index].intensity = 1f;

            inspectorRels[CreatureTemplate.Type.Scavenger.Index].type = CreatureTemplate.Relationship.Type.Attacks;
            inspectorRels[CreatureTemplate.Type.Scavenger.Index].intensity = 1f;
            inspectorRels[MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite.Index].type = CreatureTemplate.Relationship.Type.Attacks;
            inspectorRels[MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite.Index].intensity = 1f;
            inspectorRels[CreatureTemplate.Type.Slugcat.Index].type = CreatureTemplate.Relationship.Type.Attacks;
            inspectorRels[CreatureTemplate.Type.Slugcat.Index].intensity = .2f;

            //Longlegs (for inspectors)
            StaticWorld.creatureTemplates[CreatureTemplate.Type.BrotherLongLegs.Index].relationships[MoreSlugcatsEnums.CreatureTemplateType.Inspector.Index].type = CreatureTemplate.Relationship.Type.Afraid;
            StaticWorld.creatureTemplates[CreatureTemplate.Type.DaddyLongLegs.Index].relationships[MoreSlugcatsEnums.CreatureTemplateType.Inspector.Index].type = CreatureTemplate.Relationship.Type.Afraid;
            StaticWorld.creatureTemplates[MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs.Index].relationships[MoreSlugcatsEnums.CreatureTemplateType.Inspector.Index].type = CreatureTemplate.Relationship.Type.Afraid;

        }
        private void RainWorldGameOnShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            orig(self);
            ClearMemory();
        }
        private void GameSessionOnctor(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
        {
            orig(self, game);
            ClearMemory();
        }

        #region Helper Methods

        private void ClearMemory()
        {
            //If you have any collections (lists, dictionaries, etc.)
            //Clear them here to prevent a memory leak
            //YourList.Clear();
        }

        #endregion
    }
}
