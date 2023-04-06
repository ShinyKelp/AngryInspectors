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

namespace CustomRegionQuests
{

    [BepInPlugin("ShinyKelp.AngryInspectors", "AngryInspectors", "1.0.2")]
    public partial class CustomRelationshipsMod : BaseUnityPlugin
    {
        private void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
        }

        private bool IsInit, hasRedHorror;
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
                IL.MoreSlugcats.InspectorAI.IUseARelationshipTracker_UpdateDynamicRelationship += InspectorAI_IUseARelationshipTracker_UpdateDynamicRelationshipIL;
                On.Spear.HitSomething += Spear_HitSomething;
                IsInit = true;
                foreach (ModManager.Mod mod in ModManager.ActiveMods)
                    {
                        if (mod.name == "Red Horror Centipede")
                        {
                            hasRedHorror = true;
                            continue;
                        }
                    }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
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
                }
            }
            if(hasRedHorror)
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
