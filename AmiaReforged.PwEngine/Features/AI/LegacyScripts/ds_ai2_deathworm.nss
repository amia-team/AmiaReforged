//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_deathworm

//
// Edit: 12/9/2023 - Maverick00053 - Added in raidsummoner support for normal bosses



//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "inc_ds_ondeath"
#include "ds_ai2_include"


//-------------------------------------------------------------------------------
// main
//-------------------------------------------------------------------------------
void main(){

    object oCritter         = OBJECT_SELF;
    object oKiller          = GetLastKiller();


    // If this variable is set on the boss or other critter it will function alongside the raidsummoner plc
    string sRaidSpawner = GetLocalString(oCritter,"raidsummoner");
    if(sRaidSpawner != "")
    {
     // Removes the variable on the raid summoner when the boss "dies" so it can be summoned again
     object oRaidSpawner = GetObjectByTag(sRaidSpawner);
     DeleteLocalInt(oRaidSpawner,"bossOut");
     DestroyObject(GetObjectByTag("wormentrance"));
     CreateObject(OBJECT_TYPE_PLACEABLE,"wormexit",GetLocation(GetObjectByTag("purplewormenarrival")));
    }
}
