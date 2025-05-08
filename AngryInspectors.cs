using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using MoreSlugcats;
using UnityEngine;
using RWCustom;
using MonoMod.Cil;
using Mono.Cecil.Cil;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace AngryInspectors
{

    [BepInPlugin("ShinyKelp.AngryInspectors", "AngryInspectors", "1.0.3")]
    public partial class CustomRelationshipsMod : BaseUnityPlugin
    {
        private void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
        }

        private bool IsInit = false, hasRedHorror, hasOutspectors;
        private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                if (IsInit)
                    return;

                hasRedHorror = false;
                hasOutspectors = false;
                foreach (ModManager.Mod mod in ModManager.ActiveMods)
                {
                    if (mod.name == "Red Horror Centipede")
                    {
                        hasRedHorror = true;
                        continue;
                    }
                    if (mod.name == "Outspectors")
                    {
                        hasOutspectors = true;
                        continue;
                    }
                }

                //Your hooks go here
                //On.RainWorldGame.ShutDownProcess += RainWorldGameOnShutDownProcess;
                On.StaticWorld.InitStaticWorld += ModifyInspectorRelationships;
                //On.MoreSlugcats.Inspector.Act += InspectorUseFireEggs;
                //On.MoreSlugcats.Inspector.HeadWeaponized += HeadWeaponizedwithFireEgg;
                //IL.MoreSlugcats.Inspector.Act += PreventFireEggCrash;
                //On.MoreSlugcats.InspectorAI.TrackItem += InspectorTrackFireEggs;
                On.MoreSlugcats.InspectorAI.IUseARelationshipTracker_UpdateDynamicRelationship += InspectorAI_IUseARelationshipTracker_UpdateDynamicRelationship;
                IL.MoreSlugcats.InspectorAI.IUseARelationshipTracker_UpdateDynamicRelationship += InspectorAI_IUseARelationshipTracker_UpdateDynamicRelationshipIL;
                On.Spear.HitSomething += Spear_HitSomething;

                
                IsInit = true;

            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        private bool HeadWeaponizedwithFireEgg(On.MoreSlugcats.Inspector.orig_HeadWeaponized orig, Inspector self, int index)
        {
            if ((self.State as Inspector.InspectorState).headHealth[index] > 0f)
            {
                if (self.headWantToGrabChunk[index] != null && self.headWantToGrabChunk[index].owner is FireEgg)
                {
                    return true;
                }
                if (self.headGrabChunk[index] != null && self.headGrabChunk[index].owner is FireEgg)
                {
                    return true;
                }
            }
            return orig(self, index);
        }

        private void PreventFireEggCrash(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                x => x.MatchStloc(18),
                x => x.MatchLdloc(18),
                x => x.MatchLdfld<ItemTracker.ItemRepresentation>("representedItem"),
                x => x.MatchLdfld<AbstractPhysicalObject>("realizedObject"),
                x => x.MatchStloc(19),
                x => x.MatchLdloc(19));
            c.Index += 3;
            c.Emit(OpCodes.Ldloc, 19);
            c.EmitDelegate<Func<PhysicalObject, bool>>((realizedObject) =>
            {
                return realizedObject is Weapon;
            });
            c.Emit(OpCodes.And);
        }

        private bool InspectorTrackFireEggs(On.MoreSlugcats.InspectorAI.orig_TrackItem orig, InspectorAI self, AbstractPhysicalObject obj)
        {
            return orig(self, obj) || (obj.realizedObject != null && obj.realizedObject is FireEgg);
        }

        private void InspectorUseFireEggs(On.MoreSlugcats.Inspector.orig_Act orig, Inspector self)
        {
            
            if (!self.safariControlled && self.anger > .5f)
            {
                for (int l = 0; l < Inspector.headCount(); l++)
                {
                    if ((self.State as Inspector.InspectorState).headHealth[l] > 0f)
                    {
                        if (self.headWantToGrabChunk[l] != null)
                        {
                            for (int n = 0; n < self.AI.itemTracker.ItemCount; n++)
                            {
                                ItemTracker.ItemRepresentation rep = self.AI.itemTracker.GetRep(n);
                                PhysicalObject realizedObject = rep.representedItem.realizedObject;
                                if (realizedObject != null && rep.VisualContact && realizedObject is FireEgg fEgg && fEgg.activeCounter <= 0 && Vector2.Distance(fEgg.firstChunk.pos, self.mainBodyChunk.pos) < 400f && Vector2.Distance(fEgg.firstChunk.pos, self.heads[l].Tip.pos) > 10f && !self.isOtherHeadsGoalChunk(l, fEgg.firstChunk) && fEgg.mode != Weapon.Mode.Thrown && Vector2.Distance(self.heads[l].Tip.pos, fEgg.firstChunk.pos) < Vector2.Distance(self.heads[l].Tip.pos, self.headWantToGrabChunk[l].pos))
                                {
                                    self.headWantToGrabChunk[l] = fEgg.firstChunk;
                                }
                            }
                        }
                        else
                        {
                            for (int n = 0; n < self.AI.itemTracker.ItemCount; n++)
                            {
                                ItemTracker.ItemRepresentation rep = self.AI.itemTracker.GetRep(n);
                                PhysicalObject realizedObject = rep.representedItem.realizedObject;
                                if (realizedObject != null && rep.VisualContact && realizedObject is FireEgg fEgg && fEgg.activeCounter <= 0 && Vector2.Distance(fEgg.firstChunk.pos, self.mainBodyChunk.pos) < 400f && Vector2.Distance(fEgg.firstChunk.pos, self.heads[l].Tip.pos) > 10f && !self.isOtherHeadsGoalChunk(l, fEgg.firstChunk) && fEgg.mode != Weapon.Mode.Thrown)
                                {
                                    self.headWantToGrabChunk[l] = fEgg.firstChunk;
                                }
                            }
                        }
                    }
                }

            }
            orig(self);
        }

        private void InspectorAI_IUseARelationshipTracker_UpdateDynamicRelationshipIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(7)
                );
            c.Index += 6;
            c.Emit(OpCodes.Ldc_I4_1);
            c.Emit(OpCodes.Or);
        }

        private bool Spear_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            if(self.thrownBy.abstractCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.Inspector &&
                result.obj is Creature creature && creature.abstractCreature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.DaddyLongLegs)
            {
                float damage = 4f;
                int stun = 0;
                if (creature.abstractCreature.creatureTemplate.type != CreatureTemplate.Type.BrotherLongLegs)
                    stun = 20;
                if (creature.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.DaddyLongLegs)
                {
                    damage = 2f;
                }
                if (creature.abstractCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs)
                {
                    damage = 8f;
                    stun = 18;
                }
                creature.Violence(self.firstChunk, self.firstChunk.vel, creature.firstChunk, null, Creature.DamageType.Stab, damage, 1f);
                creature.stun = Mathf.Max(creature.stun, stun);
            }
            return orig(self, result, eu);
        }

        private CreatureTemplate.Relationship InspectorAI_IUseARelationshipTracker_UpdateDynamicRelationship(On.MoreSlugcats.InspectorAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig, InspectorAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            if (dRelation is null)
                return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f);

            if(dRelation.trackerRep is null || dRelation.trackerRep.representedCreature is null)
            {
                dRelation.currentRelationship.type = CreatureTemplate.Relationship.Type.Ignores;
                return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f);
            }

            if (dRelation.trackerRep.representedCreature is AbstractCreature creature &&
                StaticWorld.creatureTemplates[MoreSlugcatsEnums.CreatureTemplateType.Inspector.Index].relationships[creature.creatureTemplate.type.Index].type == CreatureTemplate.Relationship.Type.Attacks)
            {
                if (creature.realizedCreature is null || creature.realizedCreature.dead)
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
            return orig(self, dRelation);
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

            inspectorRels[MoreSlugcatsEnums.CreatureTemplateType.FireBug.Index].type = CreatureTemplate.Relationship.Type.Attacks;
            inspectorRels[MoreSlugcatsEnums.CreatureTemplateType.FireBug.Index].intensity = 1f;

            inspectorRels[CreatureTemplate.Type.RedCentipede.Index].type = CreatureTemplate.Relationship.Type.Attacks;
            inspectorRels[CreatureTemplate.Type.RedCentipede.Index].intensity = 1f;

            inspectorRels[CreatureTemplate.Type.Scavenger.Index].type = CreatureTemplate.Relationship.Type.Attacks;
            inspectorRels[CreatureTemplate.Type.Scavenger.Index].intensity = 1f;
            inspectorRels[MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite.Index].type = CreatureTemplate.Relationship.Type.Attacks;
            inspectorRels[MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite.Index].intensity = 1f;
            inspectorRels[MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing.Index].type = CreatureTemplate.Relationship.Type.Attacks;
            inspectorRels[MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing.Index].intensity = 1f;
            inspectorRels[CreatureTemplate.Type.Slugcat.Index].type = CreatureTemplate.Relationship.Type.Attacks;
            inspectorRels[CreatureTemplate.Type.Slugcat.Index].intensity = .2f;
            //Longlegs (for inspectors)
            foreach(CreatureTemplate cTemplate in StaticWorld.creatureTemplates)
            {
                if(cTemplate.TopAncestor().type == CreatureTemplate.Type.DaddyLongLegs)
                {
                    inspectorRels[cTemplate.type.Index].type = CreatureTemplate.Relationship.Type.Attacks;
                    inspectorRels[cTemplate.type.Index].intensity = 1f;
                    cTemplate.relationships[MoreSlugcatsEnums.CreatureTemplateType.Inspector.Index].type = CreatureTemplate.Relationship.Type.Afraid;
                    cTemplate.relationships[MoreSlugcatsEnums.CreatureTemplateType.Inspector.Index].intensity = 0.5f;
                    if (hasOutspectors && new CreatureTemplate.Type("Outspector").Index != -1)
                    {
                        StaticWorld.creatureTemplates[cTemplate.type.Index].relationships[new CreatureTemplate.Type("Outspector").Index].type = CreatureTemplate.Relationship.Type.Afraid;
                        StaticWorld.creatureTemplates[cTemplate.type.Index].relationships[new CreatureTemplate.Type("OutspectorB").Index].type = CreatureTemplate.Relationship.Type.Afraid;
                    }
                }
            }
            if (hasRedHorror)
            {
                inspectorRels[new CreatureTemplate.Type("RedHorrorCenti").Index].type = CreatureTemplate.Relationship.Type.Attacks;
                inspectorRels[new CreatureTemplate.Type("RedHorrorCenti").Index].intensity = 1f;
            }

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
