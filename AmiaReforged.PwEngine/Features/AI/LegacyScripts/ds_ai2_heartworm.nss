//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------

// Edit: Purple Worm heartbeat script - Maverick00053
//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "ds_ai2_include"

// AoE Attack that launches if the worm is left alone for too long. There is a duplicate in the ds_ai2_dampworm script
void LaunchAoEDamage(object oCritter);

// Removes the mobs if the worm is inactive too long
void RemoveMobs(object oWorm);

void main(){

    object oCritter     = OBJECT_SELF;
    object oEnemy       = GetNearestCreature(CREATURE_TYPE_REPUTATION, REPUTATION_TYPE_ENEMY);
    object oNearestPC   = GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC);
    float fDistanceEnemy  = GetDistanceBetween(oCritter,oEnemy);
    float fDistancePC     = GetDistanceBetween(oCritter,oNearestPC);
    int nSunkill        = GetLocalInt( oCritter, F_SUNKILL );
    int nCount          = GetLocalInt( oCritter, L_INACTIVE );
    int nAoETimer = GetLocalInt(oCritter,"aoeTimer");
    int nAoEFired = GetLocalInt(oCritter,"aoeFired");
    effect eHeal = EffectHeal(10000);

    SetLocalInt( oCritter, L_INACTIVE, (nCount + 1) );

    // Heartbeat code to make sure PCs are in range otherwise worm will activate AoE
    if(nAoEFired >= 1) // If the ability fires it has a 1 round cool down.
    {
     SetLocalInt(oCritter,"aoeFired",0);
    }
    else if((!GetIsObjectValid(oEnemy)) || (GetIsObjectValid(oEnemy) && !GetIsInsideTrigger(oEnemy,"purplewormzone")))
    {
     // If there is no enemy or if they are not in the combat zone area then this will NOT FIRE
    }
    else if(((fDistanceEnemy>0.0) && (fDistanceEnemy<4.0)) || ((fDistancePC>0.0) && (fDistancePC<4.0))) // This is the perfect mele range for the Sand Worm Skin
    {
     // --- TEST SCRIPT ---
     // SpeakString("Enemy Distance: " + FloatToString(fDistanceEnemy));
     // SpeakString("Enemy in range!");

     SetLocalInt(oCritter,"aoeTimer",0);
    }
    else if(nAoETimer >= 2)
    {
     LaunchAoEDamage(oCritter);
     SetLocalInt(oCritter,"aoeTimer",0);
     SetLocalInt(oCritter,"aoeFired",1);
     SpeakString("*Finally with the enemy just out of range the worm becomes enraged and begins to bite and thrash against the ceiling causing it begin to crumble down on everything in the area*");
    }
    else if(nAoETimer==1)
    {
     SetLocalInt(oCritter,"aoeTimer",nAoETimer+1);
     SpeakString("*The worm roars in annoyance as the enemy keep their distance and begins to thrash around hitting the cave walls and ceiling*");
    }
    else
    {
     SetLocalInt(oCritter,"aoeTimer",nAoETimer+1);
    }


    // If the Worm has been inactive for 5 minutes, heal and reset it. Removes mobs too.
    if ( nCount >= 50)
    {

      if((GetPercentageHPLoss(oCritter) != 100) || (GetTag(oCritter)!="Phase1"))
      {
       effect eNoMovement = EffectCutsceneImmobilize();
       ApplyEffectToObject(DURATION_TYPE_INSTANT, eHeal, oCritter, 0.0);
       SetTag(oCritter,"Phase1");
       DeleteLocalInt(oCritter,"10%AbilityFired");
       DeleteLocalInt(oCritter,"25%AbilityFired");
       DeleteLocalInt(oCritter,"40%AbilityFired");
       DeleteLocalInt(oCritter,"55%AbilityFired");
       DeleteLocalInt(oCritter,"70%AbilityFired");
       DeleteLocalInt(oCritter,"85%AbilityFired");
       RemoveMobs(oCritter);
      }

      SetLocalInt( oCritter, L_INACTIVE, 0 );

    }


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

void RemoveMobs(object oWorm)
{
   object oArea = GetArea(oWorm);
   object oCritters = GetFirstObjectInArea(oArea);

   while(GetIsObjectValid(oCritters))
   {
     if((GetTag(oCritters)=="respawnslow") || (GetTag(oCritters)=="respawnfast"))
     {
       SetTag(oCritters,"DIE");
       DestroyObject(oCritters);
     }
     oCritters = GetNextObjectInArea(oArea);
   }
}
