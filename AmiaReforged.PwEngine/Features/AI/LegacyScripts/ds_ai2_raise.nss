//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_death
//group:   ds_ai
//used as: OnDamage
//date:    dec 23 2007
//author:  disco
// Edited: Maverick00053, August 19th 2019
// Raise Mechanic

//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "inc_ds_ondeath"
#include "ds_ai2_include"

void RaiseMe(object oCritter, location eLoc);
void RewardTrophy(object oArea);
//-------------------------------------------------------------------------------
// main
//-------------------------------------------------------------------------------
void main(){

    object oCritter         = OBJECT_SELF;
    object oKiller          = GetLastKiller();
    object oArea            = GetArea(oCritter);
    string sCritterRes      = GetResRef(oCritter);
    effect eVFX             = EffectVisualEffect(VFX_FNF_GAS_EXPLOSION_FIRE);
    effect eVis             = EffectVisualEffect(VFX_IMP_RAISE_DEAD);
    location eLoc           = GetLocation(oCritter);

    // Random generated to see if creature will rise again
    int nRandomDeath        = Random(10) + 1;

    // Undead invasion auto raise mechanics, currently set at about 30% chance to raise
    if((GetTag(oCritter) == "raiseme") && (nRandomDeath >= 8))
    {
      DelayCommand(6.0,ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVis, eLoc));
      DelayCommand(6.5,RaiseMe(oCritter, eLoc));
      //DelayCommand(6.5,AssignCommand(OBJECT_SELF, SpeakString("*Negative energy crackles as bones begin to reform*",TALKVOLUME_TALK)));

    }
    else if((GetTag(oCritter) == "raiseme2") && (nRandomDeath >= 7))
    {
      DelayCommand(12.0,ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVis, eLoc));
      DelayCommand(12.5,RaiseMe(oCritter, eLoc));
      //DelayCommand(6.5,AssignCommand(OBJECT_SELF, SpeakString("*Negative energy crackles as bones begin to reform*",TALKVOLUME_TALK)));

    }
    else
    {
      AssignCommand(OBJECT_SELF, SpeakString("*The bones crack and crumble into dust. No more movement or magic is detected*",TALKVOLUME_TALK));
    }




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

        // Generate treasure ONLY on non rasiable guys.
        if((GetTag(oCritter) != "raiseme") || (GetTag(oCritter) != "raiseme2") || (GetTag(oCritter) != "raiseme3"))
        {
        GenerateLoot( oCritter, nXPResult );
        }
    }


}

void RaiseMe(object oCritter, location eLoc)
{
    CreateObject(OBJECT_TYPE_CREATURE,GetResRef(oCritter),eLoc, FALSE,"raiseme");

}

