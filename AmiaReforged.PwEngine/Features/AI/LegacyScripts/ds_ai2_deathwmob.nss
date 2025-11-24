//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai2_deathwmob

//
// Death script for Raid Purple Worm Mobs
// - Maverick00053

//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "inc_ds_ondeath"
#include "ds_ai2_include"


void RespawnCreature(object oCritter, location lWP);

//-------------------------------------------------------------------------------
// main
//-------------------------------------------------------------------------------
void main(){

    object oCritter         = OBJECT_SELF;
    object oKiller          = GetLastKiller();

    object oRaidSummoner = GetObjectByTag("wormraid");
    int nBossOut = GetLocalInt(oRaidSummoner,"bossOut");

    //OnDeath custom ability usage
    string sDE = GetLocalString( oCritter, "DeathEffect" );
    if( sDE != "" )
    {
        SetLocalObject( oCritter, sDE, oKiller );
        ExecuteScript( sDE, oCritter );
    }


    int nRandom  = Random(18)+1;
    object oWP = GetWaypointByTag("wormmobspawn"+IntToString(nRandom));
    location lWP = GetLocation(oWP);

    if(GetIsObjectValid(oWP) && (nBossOut==1))
    {
     if(GetTag(oCritter)=="respawnfast")
     {
       DelayCommand(24.0,RespawnCreature(oCritter,lWP));
     }
     else if(GetTag(oCritter)=="respawnslow")
     {
       DelayCommand(36.0,RespawnCreature(oCritter,lWP));
     }
    }

}

void RespawnCreature(object oCritter, location lWP)
{
  CreateObject(OBJECT_TYPE_CREATURE,GetResRef(oCritter),lWP);
}
