// Frostspear + support mobs boss mechanics
// Edited: 07/23/19 - Maverick00053. Added in speak scripts and boss mechanics.
#include "ds_ai2_include"
#include "inc_ds_ondeath"

void IceBlast(object oArea);
void FlyBack(object oWayPoint2, int nPhase, int nWaypointPicked);
void LootDrop(object oArea, object oWayPoint3, object oWaypoint4);
void CreateLoot(string sResRef, object Chest);

void main()
{

    object oArea = GetArea(OBJECT_SELF);
    object oDamager = GetLastDamager();
    object oFirst;
    object oTarget      = GetLocalObject( OBJECT_SELF, L_CURRENTTARGET );
    object oWayPoint = GetWaypointByTag("FlyToZone");
    object oWayPoint2;
    object oWayPoint3 = GetWaypointByTag("DragonHorde");
    object oWayPoint4 = GetWaypointByTag("dbossexit");

    int nReaction       = GetReaction( OBJECT_SELF, oDamager );
    int nBlasts = 0;
    int nRandom = Random(20); //nRandom is Wing buffet fire chance, 1 in 20
    int nRandom2 =  Random(3);
    int nTalked = GetLocalInt(OBJECT_SELF, "talkedrecently");
    int nAreaLieutentants = GetLocalInt(oArea,"InvasionLieutenants");
    int n75PercentHP = GetLocalInt(OBJECT_SELF,"75%AbilityFired");
    int n50PercentHP = GetLocalInt(OBJECT_SELF,"50%AbilityFired");
    int n25PercentHP = GetLocalInt(OBJECT_SELF,"25%AbilityFired");
    int n10PercentHP = GetLocalInt(OBJECT_SELF,"10%AbilityFired");
    int nWaypointPicked;
    string sArchetype   = GetLocalString( OBJECT_SELF, L_ARCHETYPE );
    string sMob = GetResRef(OBJECT_SELF);
    string sMobTag = GetTag(OBJECT_SELF);

    location lCreature = GetLocation(OBJECT_SELF);
    location lAhead = GetAheadLocation(OBJECT_SELF);
    location lBehind = GetBehindLocation(OBJECT_SELF);
    location lLeft  = GetStepLeftLocation(OBJECT_SELF);
    location lRight = GetStepRightLocation(OBJECT_SELF);
    location lAheadL = GetAheadLeftLocation(OBJECT_SELF);
    location lAheadR = GetAheadRightLocation(OBJECT_SELF);

    effect eFlyAway = EffectDisappear();
    effect eFly = EffectDisappearAppear(GetLocation(oWayPoint));
    effect eVisualAura = EffectVisualEffect(VFX_DUR_AURA_COLD);
    effect eVisualSummon = EffectVisualEffect(VFX_FNF_SUMMON_MONSTER_1);

    // Waypoint return check
    if(nRandom2 == 0)
    {
      oWayPoint2 = GetWaypointByTag("dragonreturn");
      nWaypointPicked = 1;
    }
    else if(nRandom2 == 1)
    {
      oWayPoint2 = GetWaypointByTag("dragonreturn2");
      nWaypointPicked = 2;
    }
    else if(nRandom2 == 2)
    {
      oWayPoint2 = GetWaypointByTag("dragonreturn3");
      nWaypointPicked = 3;
    }

    // HP based reactions for white dragon boss mobs
    if(sMob == "wdb_mob" || sMob == "wdb_mob2")
    {

      if(GetPercentageHPLoss(OBJECT_SELF) <= 10)
      {
        if((n10PercentHP == 0))  // Only lets the speak fire once
        {
        SpeakString("*Snarls and takes off out of the cave*");
        }
        ApplyEffectToObject(DURATION_TYPE_INSTANT,eFlyAway,OBJECT_SELF);
        SetLocalInt(OBJECT_SELF,"10%AbilityFired",1);
      }

    }
    else
    {
          // ds_ai2_damaged script

          if ( nReaction == 2 ){

           if ( GetDistanceBetween( OBJECT_SELF, oDamager ) < 5.0 ){

            ClearAllActions();
            ActionMoveAwayFromObject( oDamager, TRUE, 10.0 );
            SetLocalObject( OBJECT_SELF, L_CURRENTTARGET, oDamager );
           }
          }
          else if ( nReaction == 1 )
          {

           if ( oTarget != oDamager )
           {

             if ( GetObjectSeen( oDamager, OBJECT_SELF ) && ( d100() - 20 ) < 25 )
             {

                SetLocalObject( OBJECT_SELF, L_CURRENTTARGET, oDamager );
             }
            }
           }



    }
    // end of white dragon mobs

    // HP based reactions for white dragon boss
    if(sMob == "whitedragonboss")
    {
      if((GetPercentageHPLoss(OBJECT_SELF) <= 75) && (n75PercentHP == 0) && (GetLocalInt(OBJECT_SELF,"75%AbilityFired") != 1) && (sMobTag == "Phase1"))
      {  // 75% HP and the boss retreats for a moment, entering phase 2 of fight


        SpeakString("*Recoils from the damage before taking flight! The white dragon begins to soar around the cave as it bombards the attackers with its cold blasts from above!*");
        CreateObject(OBJECT_TYPE_CREATURE,"wdb_mob",lLeft,TRUE);
        //ApplyEffectAtLocation(DURATION_TYPE_INSTANT,eVisualSummon,lLeft,0.0);
        CreateObject(OBJECT_TYPE_CREATURE,"wdb_mob",lRight,TRUE);
        //ApplyEffectAtLocation(DURATION_TYPE_INSTANT,eVisualSummon,lRight,0.0);

        // Launches 12 blasts in the area
        while(nBlasts <= 12)
        {
          nBlasts++;
          DelayCommand(IntToFloat(nBlasts*2),IceBlast(oArea));

        }


        SetLocalInt(OBJECT_SELF,"75%AbilityFired",1);
        ApplyEffectToObject(DURATION_TYPE_INSTANT,eFly,OBJECT_SELF);
        DelayCommand(24.5,FlyBack(oWayPoint2,1, nWaypointPicked));
        DestroyObject(OBJECT_SELF, 25.0);


      }
      else if((GetPercentageHPLoss(OBJECT_SELF) <= 50) && (n50PercentHP == 0) && (sMobTag == "Phase2"))
      {  // 50% HP and the boss retreats for a moment, entering phase 3 of fight


        // Spawn in Wyrmlings
        SpeakString("*Once more the white dragon takes flight and begins to soar around the cave as it bombards the attackers with its cold blasts from above!");
        CreateObject(OBJECT_TYPE_CREATURE,"wdb_mob",lLeft,TRUE);
        //ApplyEffectAtLocation(DURATION_TYPE_INSTANT,eVisualSummon,lLeft,0.0);
        CreateObject(OBJECT_TYPE_CREATURE,"wdb_mob",lRight,TRUE);
        //ApplyEffectAtLocation(DURATION_TYPE_INSTANT,eVisualSummon,lRight,0.0);
        CreateObject(OBJECT_TYPE_CREATURE,"wdb_mob",lAhead,TRUE);
        //ApplyEffectAtLocation(DURATION_TYPE_INSTANT,eVisualSummon,lAhead,0.0);
        CreateObject(OBJECT_TYPE_CREATURE,"wdb_mob",lBehind,TRUE);
        //ApplyEffectAtLocation(DURATION_TYPE_INSTANT,eVisualSummon,lBehind,0.0);


        // Launches 12 blasts in the area
        while(nBlasts <= 12)
        {
          nBlasts++;
          DelayCommand(IntToFloat(nBlasts*2),IceBlast(oArea));

        }

        SetLocalInt(OBJECT_SELF,"50%AbilityFired",1);
        ApplyEffectToObject(DURATION_TYPE_INSTANT,eFly,OBJECT_SELF);
        DelayCommand(24.5,FlyBack(oWayPoint2,2,nWaypointPicked));
        DestroyObject(OBJECT_SELF, 25.0);
      }
      else if((GetPercentageHPLoss(OBJECT_SELF) <= 25) && (n25PercentHP == 0) && (sMobTag == "Phase3"))
      {   // 25% HP and the boss retreats for a moment, entering phase 3 of fight

        // Spawn in Wyrmlings
        SpeakString("*The fatigued beast takes off again and begins to soar around the cave as it bombards the attackers with its cold blasts from above!");
        CreateObject(OBJECT_TYPE_CREATURE,"wdb_mob",lLeft,TRUE);
        //ApplyEffectAtLocation(DURATION_TYPE_INSTANT,eVisualSummon,lLeft,0.0);
        CreateObject(OBJECT_TYPE_CREATURE,"wdb_mob",lRight,TRUE);
        //ApplyEffectAtLocation(DURATION_TYPE_INSTANT,eVisualSummon,lRight,0.0);
        CreateObject(OBJECT_TYPE_CREATURE,"wdb_mob",lAhead,TRUE);
        //ApplyEffectAtLocation(DURATION_TYPE_INSTANT,eVisualSummon,lAhead,0.0);
        CreateObject(OBJECT_TYPE_CREATURE,"wdb_mob",lBehind,TRUE);
        //ApplyEffectAtLocation(DURATION_TYPE_INSTANT,eVisualSummon,lBehind,0.0);
        CreateObject(OBJECT_TYPE_CREATURE,"wdb_mob",lAheadL,TRUE);
        CreateObject(OBJECT_TYPE_CREATURE,"wdb_mob",lAheadR,TRUE);


        // Launches 12 blasts in the area
        while(nBlasts <= 12)
        {
          nBlasts++;
          DelayCommand(IntToFloat(nBlasts*2),IceBlast(oArea));


        }



        SetLocalInt(OBJECT_SELF,"25%AbilityFired",1);
        ApplyEffectToObject(DURATION_TYPE_INSTANT,eFly,OBJECT_SELF);
        DelayCommand(24.5,FlyBack(oWayPoint2,3,nWaypointPicked));
        DestroyObject(OBJECT_SELF, 25.0);
      }
      else if((GetPercentageHPLoss(OBJECT_SELF) <= 10)  && (sMobTag == "Phase4"))  // 10 percent HP and last phase the boss runs
      {

        if((n10PercentHP == 0))  // Only lets the speak fire once
        {
        SpeakString("*Wounded, the mighty Wyrm takes flight but this time soars up and out of the cave, disappearing into the sky*");
        LootDrop(oArea,oWayPoint3,oWayPoint4);
        }
        ApplyEffectToObject(DURATION_TYPE_INSTANT,eFlyAway,OBJECT_SELF);
        SetLocalInt(OBJECT_SELF,"10%AbilityFired",1);

      }
      else
      {

        if(nRandom == 5)// 1 in 10 percent chance to fire
        {
         ExecuteScript("j_ai_wingbuffet2",OBJECT_SELF);
        }
        else
        {


          // ds_ai2_damaged script

          if ( nReaction == 2 ){

           if ( GetDistanceBetween( OBJECT_SELF, oDamager ) < 5.0 ){

            ClearAllActions();
            ActionMoveAwayFromObject( oDamager, TRUE, 10.0 );
            SetLocalObject( OBJECT_SELF, L_CURRENTTARGET, oDamager );
           }
          }
          else if ( nReaction == 1 )
          {

           if ( oTarget != oDamager )
           {

             if ( GetObjectSeen( oDamager, OBJECT_SELF ) && ( d100() - 20 ) < 25 )
             {

                SetLocalObject( OBJECT_SELF, L_CURRENTTARGET, oDamager );
             }
            }
           }

            }

          }





      }



}





 // Based on the phase the fire blasts will randomly fire with in the area
void IceBlast(object oArea)
{

    string sBlastObject;
    object oFirstBlast;
    location lObject;
    effect eExplode = EffectVisualEffect(VFX_FNF_ICESTORM);
    effect eVisualCold = EffectVisualEffect(VFX_IMP_FROST_S);
    effect eDamCold = EffectDamage(60,DAMAGE_TYPE_COLD,DAMAGE_POWER_NORMAL);
    effect eDamBlud = EffectDamage(60,DAMAGE_TYPE_BLUDGEONING,DAMAGE_POWER_NORMAL);
    effect eLink = EffectLinkEffects(eVisualCold,eDamCold);
    eLink = EffectLinkEffects(eLink,eDamBlud);


    // Random generation variables
    int iWidthInTiles;
    int iHeightInTiles;
    int iWidthInMeters;
    int iHeightInMeters;
    int iWidthInMetersRandom;
    int iHeightInMetersRandom;
    float  fXPosition;
    float  fYPosition;
    float  fZPosition;
    vector vRandomPos;
    vector vRandomPosN;
    vector vRandomPosS;
    vector vRandomPosE;
    vector vRandomPosW;
    vector vRandomPosNE;
    vector vRandomPosNW;
    vector vRandomPosSE;
    vector vRandomPosSW;
    float    fFacing;
    location lRandom;
    string sMob;

     // If a valid area was found, compute the random location.
    if(GetIsObjectValid(oArea))
    {
      // Determine the width and height of the area in tiles.

      // We want to restrict the blasts to the part of the map the players are at

       iWidthInTiles  = 4;
       iHeightInTiles = 2;

      // Convert the width and height from tiles into meters.
       iWidthInMeters  = iWidthInTiles  * 10;
       iHeightInMeters = iHeightInTiles * 10;


      // Generate a random position in the area.
        fXPosition = IntToFloat(Random(iWidthInMeters  * 10)) / 10.0;
        fYPosition = IntToFloat(Random(iHeightInMeters * 10)) / 10.0;

      // Adjusting the blast area up and to the right
        fXPosition = fXPosition + 10.0;
        fYPosition = fYPosition + 10.0;


        fZPosition = 0.0;
        vRandomPos = Vector(fXPosition, fYPosition, fZPosition);

      // Convert the random position to obtain the random location.
        fFacing = 0.0;
        lRandom = Location(oArea, vRandomPos, fFacing);
     }

     //Declare the spell shape, size and the location.  Capture the first target object in the shape.
     oFirstBlast  = GetFirstObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_HUGE, lRandom, FALSE, OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR | OBJECT_TYPE_PLACEABLE);


     //Apply the ice storm VFX at the location captured above.
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eExplode, lRandom);

     //Cycle through the targets within the spell shape until an invalid object is captured.
     while (GetIsObjectValid(oFirstBlast))
     {

       sMob = GetResRef(oFirstBlast);
       if(sMob != "wdb_mob" && sMob != "wdb_mob2")
       {
         ApplyEffectToObject(DURATION_TYPE_INSTANT,eLink,oFirstBlast);
       }

       //Declare the spell shape, size and the location.  Capture the first target object in the shape.
       oFirstBlast  = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_HUGE, lRandom, FALSE, OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR | OBJECT_TYPE_PLACEABLE);

     }



}

void FlyBack(object oWayPoint2, int nPhase, int nWaypointPicked)
{
  string sPhase;
  object oJuvSpawn1;
  object oJuvSpawn2;
  // Figure out the phase the dragon is in
  if(nPhase == 1)
  {
    sPhase = "Phase2";
  }
  else if(nPhase == 2)
  {
    sPhase = "Phase3";
  }
  else if(nPhase == 3)
  {
    sPhase = "Phase4";
  }

  // Based on the way point that was randomly picked flying back, use the other two waypoints to spawn in juv dragons
  if(nWaypointPicked == 1)
  {
    oJuvSpawn1 = GetWaypointByTag("dragonreturn2");
    oJuvSpawn2 = GetWaypointByTag("dragonreturn3");
  }
  else if(nWaypointPicked == 2)
  {
    oJuvSpawn1 = GetWaypointByTag("dragonreturn");
    oJuvSpawn2 = GetWaypointByTag("dragonreturn3");
  }
  else if(nWaypointPicked == 3)
  {
    oJuvSpawn1 = GetWaypointByTag("dragonreturn");
    oJuvSpawn2 = GetWaypointByTag("dragonreturn2");
  }

  // Spawns in two other juvenile dragons to support her
  CreateObject(OBJECT_TYPE_CREATURE,"wdb_mob2",GetLocation(oJuvSpawn1), TRUE);
  CreateObject(OBJECT_TYPE_CREATURE,"wdb_mob2",GetLocation(oJuvSpawn2), TRUE);
  // Spawns Frostspear back
  CreateObject(OBJECT_TYPE_CREATURE,"whitedragonboss",GetLocation(oWayPoint2), TRUE, sPhase);


}

void LootDrop(object oArea, object oWayPoint3, object oWaypoint4)
{
    int nRandom  = Random(100);
    int nRandom2 = Random(100);
    int nRandom3 = Random(5);
    object oHorde = CreateObject(OBJECT_TYPE_PLACEABLE,"whitedragonhorde",GetLocation(oWayPoint3));
    CreateObject(OBJECT_TYPE_PLACEABLE,"dbossexit",GetLocation(oWaypoint4));

    // Removes the variable on the raid summoner when the boss "dies" so it can be summoned again
    object oRaidSpawner = GetObjectByTag("raidsummonerfrosty");
    DeleteLocalInt(oRaidSpawner,"bossOut");
    //

    if(nRandom <= 10)      //  wdragonbossrewar - Frostspear's Treasure, and 4 more unique bin drops
    {
      DelayCommand(0.5,CreateLoot("raid_base_frosty",oHorde));
    }

    if(nRandom2 <= 25)        // mythal5   - Flawless Mythal
    {
      DelayCommand(0.5,CreateLoot("mythal5",oHorde));
    }

    DelayCommand(0.5,GenerateEpicLoot(oHorde));

}

void CreateLoot(string sResRef, object Chest)
{
    CreateItemOnObject(sResRef,Chest);
}
