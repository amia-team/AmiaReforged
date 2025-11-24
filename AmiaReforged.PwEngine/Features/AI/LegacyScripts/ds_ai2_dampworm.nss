//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_dampworm
//group:   ds_ai
//used as: OnDamage
//date:    dec 23 2007
//author:  disco

/*
    On damage script made for Purple Worm Raid Boss
    - Maverick00053
*/

//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "ds_ai2_include"
#include "inc_ds_ondeath"


// These values dictate your Waypoint count for respawning Worm and Worm Mobs
int nWormWP = 14;
int nWormMobWP = 18;

// Purple Worm Burrow Back Ability
void BurrowBack(string sPhase, object oArea);

// Grabs a mob waypoint
location GrabMobWaypoint();

// The worm AoE damage launch. There is a duplicate in the ds_ai2_heartworm script
void LaunchAoEDamage(object oCritter);

// Generates the loot for the purple worm
void GenerateWormLoot(object oWorm);

//-------------------------------------------------------------------------------
// main
//-------------------------------------------------------------------------------
void main(){

    object oCritter     = OBJECT_SELF;
    object oDamager     = GetLastDamager();
    object oTarget      = GetLocalObject( oCritter, L_CURRENTTARGET );
    object oArea        = GetArea(oCritter);
    string sArchetype   = GetLocalString( oCritter, L_ARCHETYPE );

    effect eRun = EffectDisappearAppear(GetLocation(oCritter)); // Have to use EffectDisappearAppear and not EffectDisappear
    int nReaction = GetReaction( oCritter, oDamager );
    int nHPLoss = GetPercentageHPLoss(oCritter);


    if ( nReaction == 1 ){

        if ((oTarget != oDamager) && (GetDistanceBetween( oCritter, oDamager ) < 3.65) ){

            if ( GetObjectSeen( oDamager, oCritter ) && ( d100() - 20 ) < 25 ){

                SetLocalObject( oCritter, L_CURRENTTARGET, oDamager );
            }
        }
    }

    //Check for %health based triggers
    string sMobTag = GetTag(OBJECT_SELF);
    int n85PercentHP = GetLocalInt(OBJECT_SELF,"85%AbilityFired");
    int n70PercentHP = GetLocalInt(OBJECT_SELF,"70%AbilityFired");
    int n55PercentHP = GetLocalInt(OBJECT_SELF,"55%AbilityFired");
    int n40PercentHP = GetLocalInt(OBJECT_SELF,"40%AbilityFired");
    int n25PercentHP = GetLocalInt(OBJECT_SELF,"25%AbilityFired");
    int n10PercentHP = GetLocalInt(OBJECT_SELF,"10%AbilityFired");

    if((nHPLoss <= 85) && (GetLocalInt(oCritter,"85%AbilityFired") != 1) && (sMobTag == "Phase1"))
    {
      SpeakString("*Roars and dives back underground. The chamber trembles and shakes as cracks form on the walls and sinsiter creatures begin to come investigate*");
      SetLocalInt(OBJECT_SELF,"85%AbilityFired",1);
      ApplyEffectToObject(DURATION_TYPE_INSTANT,eRun,oCritter);
      // Mob Spawns
      CreateObject(OBJECT_TYPE_CREATURE,"raid_giantspider",GrabMobWaypoint(),FALSE);
      CreateObject(OBJECT_TYPE_CREATURE,"raid_giantspider",GrabMobWaypoint(),FALSE);
      CreateObject(OBJECT_TYPE_CREATURE,"raid_giantspider",GrabMobWaypoint(),FALSE);
      CreateObject(OBJECT_TYPE_CREATURE,"raid_formorian",GrabMobWaypoint(),FALSE);

      DelayCommand(12.0,BurrowBack(sMobTag,oArea));

    }
    else if((nHPLoss <= 70) && (GetLocalInt(oCritter,"70%AbilityFired") != 1) && (sMobTag == "Phase2"))
    {
      SpeakString("*Roars and dives back underground. It thrashes around so violently it causes rocks to collapse inside the chamber*");
      SetLocalInt(OBJECT_SELF,"70%AbilityFired",1);
      LaunchAoEDamage(oCritter);
      ApplyEffectToObject(DURATION_TYPE_INSTANT,eRun,oCritter);

      DelayCommand(12.0,BurrowBack(sMobTag,oArea));
    }
    else if((nHPLoss <= 55) && (GetLocalInt(oCritter,"55%AbilityFired") != 1) && (sMobTag == "Phase3"))
    {
      SpeakString("*Roars and dives back underground. The chamber trembles and shakes as more sinsiter creatures come forth!*");
      SetLocalInt(OBJECT_SELF,"55%AbilityFired",1);
      ApplyEffectToObject(DURATION_TYPE_INSTANT,eRun,oCritter);
      // Mob Spawns
      CreateObject(OBJECT_TYPE_CREATURE,"raid_giantspider",GrabMobWaypoint(),FALSE);
      CreateObject(OBJECT_TYPE_CREATURE,"raid_giantspider",GrabMobWaypoint(),FALSE);
      CreateObject(OBJECT_TYPE_CREATURE,"raid_giantspider",GrabMobWaypoint(),FALSE);
      CreateObject(OBJECT_TYPE_CREATURE,"raid_formorian",GrabMobWaypoint(),FALSE);

      DelayCommand(12.0,BurrowBack(sMobTag,oArea));
    }
    else if((nHPLoss <= 40) && (GetLocalInt(oCritter,"40%AbilityFired") != 1) && (sMobTag == "Phase4"))
    {
      SpeakString("*Roars and dives back underground. It thrashes around so violently it causes rocks to collapse inside the chamber*");
      SetLocalInt(OBJECT_SELF,"40%AbilityFired",1);
      LaunchAoEDamage(oCritter);
      ApplyEffectToObject(DURATION_TYPE_INSTANT,eRun,oCritter);

      DelayCommand(12.0,BurrowBack(sMobTag,oArea));
    }
    else if((nHPLoss <= 25) && (GetLocalInt(oCritter,"25%AbilityFired") != 1) && (sMobTag == "Phase5"))
    {
      SpeakString("*Roars and dives back underground. The chamber trembles and shakes as more sinsiter creatures come forth!*");
      SetLocalInt(OBJECT_SELF,"25%AbilityFired",1);
      ApplyEffectToObject(DURATION_TYPE_INSTANT,eRun,oCritter);
      // Mob Spawns
      CreateObject(OBJECT_TYPE_CREATURE,"raid_giantspider",GrabMobWaypoint(),FALSE);
      CreateObject(OBJECT_TYPE_CREATURE,"raid_giantspider",GrabMobWaypoint(),FALSE);
      CreateObject(OBJECT_TYPE_CREATURE,"raid_giantspider",GrabMobWaypoint(),FALSE);
      CreateObject(OBJECT_TYPE_CREATURE,"raid_formorian",GrabMobWaypoint(),FALSE);

      DelayCommand(12.0,BurrowBack(sMobTag,oArea));
    }
    else if((nHPLoss <= 10) && (GetLocalInt(oCritter,"10%AbilityFired") != 1) && (sMobTag == "Phase6"))
    {
      SpeakString("*Roars and dives back underground. It thrashes around so violently it causes rocks to collapse inside the chamber*");
      SetLocalInt(OBJECT_SELF,"10%AbilityFired",1);
      LaunchAoEDamage(oCritter);
      ApplyEffectToObject(DURATION_TYPE_INSTANT,eRun,oCritter);

      DelayCommand(12.0,BurrowBack(sMobTag,oArea));
    }
    else if((nHPLoss <= 5) && (sMobTag == "Phase7"))
    {
      SetImmortal(oCritter,FALSE);
    }


}

void BurrowBack(string sPhase, object oArea)
{
  effect eNoMovement = EffectCutsceneImmobilize();
  int nWPRandom = Random(nWormWP)+1;
  int nAddLoot;
  object oWormWaypoint = GetWaypointByTag("wormspawn"+IntToString(nWPRandom));
  location lWormWaypoint = GetLocation(oWormWaypoint);

  if(sPhase=="Phase1")
  {
   sPhase="Phase2";
  }
  else if(sPhase=="Phase2")
  {
   sPhase="Phase3";
  }
  else if(sPhase=="Phase3")
  {
   sPhase="Phase4";
  }
  else if(sPhase=="Phase4")
  {
   sPhase="Phase5";
  }
  else if(sPhase=="Phase5")
  {
   sPhase="Phase6";
  }
  else if(sPhase=="Phase6")
  {
   sPhase="Phase7";
   nAddLoot=1;
  }

  object oPlayers = GetFirstPC();
  while(GetIsObjectValid(oPlayers))
  {
   if(GetArea(oPlayers)==oArea)
   {
     SendMessageToPC(oPlayers,"*The nursery shakes as the massive Purple Worm breaks through the ground somewhere nearby to resume the fight!*");
   }
   oPlayers = GetNextPC();
  }

  object oWorm = CreateObject(OBJECT_TYPE_CREATURE,"raid_purpleworm",lWormWaypoint,TRUE,sPhase);
  DelayCommand(1.0,AssignCommand(oWorm,SpeakString("*The ground trembles and cracks as the massive Purple Worm appears!*")));
  SetObjectVisualTransform(oWorm, 10, 0.6);
  ApplyEffectToObject(DURATION_TYPE_PERMANENT,eNoMovement,oWorm);

  if(nAddLoot==1)
  {
    GenerateWormLoot(oWorm);
  }
}

location GrabMobWaypoint()
{
  int nWPRandom = Random(nWormMobWP)+1;
  string sWP = "wormmobspawn"+IntToString(nWPRandom);
  object oWP = GetWaypointByTag(sWP);
  location lWP = GetLocation(oWP);
  return lWP;
}

void LaunchAoEDamage(object oCritter)
{
  object oArea = GetArea(oCritter);
  object oWP;
  effect eVFX1  = EffectVisualEffect(VFX_FNF_METEOR_SWARM);
  effect eVFX2  = EffectVisualEffect(354); // Impact VFX
  effect eDam1;
  effect eDam2;
  effect eLink;
  int nDamage1;
  int nDamage2;

  int nWPCount = 5;

  int i;
  for(i=1;i<=nWPCount;i++)
  {
    oWP = GetWaypointByTag("wormvfx"+IntToString(i));
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT,eVFX1,GetLocation(oWP));
  }

  object oObject = GetFirstObjectInArea(oArea,OBJECT_TYPE_CREATURE);

  while(GetIsObjectValid(oObject))
  {
    if(GetIsInsideTrigger(oObject,"purplewormzone") && (GetResRef(oObject) != "raid_purpleworm"))
    {

     // Mobs will take "less" damage to avoid using the AoE to hard cheese the mobs, but still showing the mobs are getting damaged too
     if((GetResRef(oObject)=="raid_giantspider") || (GetResRef(oObject)=="raid_formorian"))
     {
      nDamage2 = 31 + Random(20);
      nDamage1 = 21 + Random(20);
     }
     else
     {
      nDamage2 = 61 + Random(30);
      nDamage1 = 41 + Random(30);
     }

     if(ReflexSave(oObject,42)==0)
     {
       // Save "failed" do nothing more
     }
     else
     {
       nDamage1 = nDamage1/2;  // Half damage
     }
      eDam1 = EffectDamage(nDamage1,DAMAGE_TYPE_PIERCING);
      eDam2 = EffectDamage(nDamage2,DAMAGE_TYPE_BLUDGEONING);
      eLink = EffectLinkEffects(eDam1, eDam2);
      eLink = EffectLinkEffects(eVFX2, eLink);
      ApplyEffectToObject(DURATION_TYPE_INSTANT,eLink,oObject);
    }

    oObject = GetNextObjectInArea(oArea, OBJECT_TYPE_CREATURE);
  }

}

void GenerateWormLoot(object oWorm)
{
   object oLoot;

   int i;
   int nRandom1 = Random(10)+11;
   for(i=0;i<nRandom1;i++)
   {
    oLoot = CreateItemOnObject("js_scho_anc", oWorm);  // Ancient Device
    SetDroppableFlag(oLoot,TRUE);
   }

   i=0;
   nRandom1 = Random(10)+11;
   for(i=0;i<nRandom1;i++)
   {
    oLoot = CreateItemOnObject("js_corpse", oWorm); // Corpses
    SetDroppableFlag(oLoot,TRUE);
   }

   i=0;
   nRandom1 = Random(10)+11;
   for(i=0;i<nRandom1;i++)
   {
    oLoot = CreateItemOnObject("js_met_adao", oWorm); // Adaminatine Ore
    SetDroppableFlag(oLoot,TRUE);
   }

   i=0;
   nRandom1 = Random(3)+4;
   for(i=0;i<nRandom1;i++)
   {
    oLoot = CreateItemOnObject("js_met_golo", oWorm); // Gold Ore
    SetDroppableFlag(oLoot,TRUE);
   }

   i=0;
   nRandom1 = Random(3)+4;
   for(i=0;i<nRandom1;i++)
   {
    oLoot = CreateItemOnObject("js_met_mito", oWorm); // Mithral Ore
    SetDroppableFlag(oLoot,TRUE);
   }

   i=0;
   nRandom1 = Random(6)+8;
   for(i=0;i<nRandom1;i++)
   {
    oLoot = CreateItemOnObject("js_bla_carb", oWorm); // Carbon
    SetDroppableFlag(oLoot,TRUE);
   }

   i=0;
   nRandom1 = Random(3)+4;
   for(i=0;i<nRandom1;i++)
   {
    oLoot = CreateItemOnObject("js_met_iroo", oWorm); // Iron Ingot
    SetDroppableFlag(oLoot,TRUE);
   }

   i=0;
   nRandom1 = Random(2)+2;
   for(i=0;i<nRandom1;i++)
   {
    oLoot = CreateItemOnObject("js_hun_lvgland", oWorm); // Legendary Vemon Glad
    SetDroppableFlag(oLoot,TRUE);
   }


   nRandom1 = Random(10)+11;
   oLoot = CreateItemOnObject("js_gem_rcry", oWorm,nRandom1); // Raw Crystal
   SetDroppableFlag(oLoot,TRUE);

   nRandom1 = Random(10)+11;
   oLoot = CreateItemOnObject("js_gem_rdia", oWorm,nRandom1); // Raw Diamond
   SetDroppableFlag(oLoot,TRUE);

   nRandom1 = Random(10)+11;
   oLoot = CreateItemOnObject("js_gem_reme", oWorm,nRandom1); // Raw Emerald
   SetDroppableFlag(oLoot,TRUE);

   nRandom1 = Random(10)+11;
   oLoot = CreateItemOnObject("js_gem_rrub", oWorm,nRandom1); // Raw Ruby
   SetDroppableFlag(oLoot,TRUE);

   nRandom1 = Random(10)+11;
   oLoot = CreateItemOnObject("js_gem_rsap", oWorm,nRandom1); // Raw Sapphire
   SetDroppableFlag(oLoot,TRUE);

   // Epic Drops

   oLoot = CreateItemOnObject("nep_largemagical", oWorm);
   SetDroppableFlag(oLoot,TRUE);

   oLoot = GenerateEpicLootReturn(oWorm);
   SetDroppableFlag(oLoot,TRUE);

   // Quest Drop
   oLoot = CreateItemOnObject("qst_purpleworm", oWorm);
   SetDroppableFlag(oLoot,TRUE);

}
