//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_gibdeath
//group:   ds_ai
//used as: OnDamage
//date:    dec 23 2007
//author:  disco
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
    object oPC              = GetLastKiller();
    object oArea            = GetArea(oCritter);
    string AreaName         = GetName(oArea);
    object oKiller          = GetLastKiller();
    object oWP              = GetWaypointByTag("gib_climb_out");
    location lLoc           = GetLocation(oWP);
    float fFacing           = GetFacing(oWP);

    object oPLC = CreateObject(OBJECT_TYPE_PLACEABLE,"gib_boss_plc",lLoc);
    AssignCommand(oPLC,SetFacing(fFacing));
    AssignCommand(oPLC,SpeakString("<c¥  >*Once you clear away the rest of the creatures the way to climb back is safe now!*</c>"));

    //OnDeath custom ability usage
    string sDE = GetLocalString( oCritter, "DeathEffect" );
    if( sDE != "" )
    {
        SetLocalObject( oCritter, sDE, oKiller );
        ExecuteScript( sDE, oCritter );
    }

    if( GetLastKiller() != OBJECT_SELF && GetLocalInt( oCritter, L_ISDEAD ) != 1 ){

        // Set have died once, stops giving out multiple amounts of XP.
        SetLocalInt( oCritter, L_ISDEAD, 1 );

        // Reward XP.
        int nXPResult = RewardXPForKill( );

        // Generate treasure.
        GenerateLoot( oCritter, nXPResult );
    }

    // If this variable is set on the boss or other critter it will function alongside the raidsummoner plc
    string sRaidSpawner = GetLocalString(oCritter,"raidsummoner");
    if(sRaidSpawner != "")
    {
     // Removes the variable on the raid summoner when the boss "dies" so it can be summoned again
     object oRaidSpawner = GetObjectByTag(sRaidSpawner);
     DeleteLocalInt(oRaidSpawner,"bossOut");
    }

    // Global Announcement for Boss Death

    if(GetTag(oCritter) == "GlobalBoss")
    {
     // Announcer Test
     SetLocalString(GetModule(),"announcerMessage","``` *The Guilds have released an announcement that the creature of note has been slain* ```");
     ExecuteScript("webhook_announce");
     SetLocalString(GetModule(),"staffMessage","Global Boss Slain: " + AreaName + " by " + GetName(oKiller));
     ExecuteScript("webhook_staff");
     //
    }

}
