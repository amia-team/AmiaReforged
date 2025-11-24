//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_death
//group:   ds_ai
//used as: OnDamage
//date:    dec 23 2007
//author:  disco


//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "inc_ds_ondeath"
#include "inc_ds_qst"
#include "ds_ai2_include"


//-------------------------------------------------------------------------------
// main
//-------------------------------------------------------------------------------
void main(){

    object oCritter         = OBJECT_SELF;
    object oKiller          = GetLastKiller();

    //OnDeath custom ability usage
    string sDE = GetLocalString( oCritter, "DeathEffect" );
    if( sDE != "" )
    {
        SetLocalObject( oCritter, sDE, oKiller );
        ExecuteScript( sDE, oCritter );
    }

    if ( GetLastKiller() != OBJECT_SELF && GetLocalInt( oCritter, L_ISDEAD ) != 1 ){

        // Set have died once, stops giving out multiple amounts of XP.
        SetLocalInt( oCritter, L_ISDEAD, 1 );

        // Reward XP.
        int nXPResult = RewardXPForKill( );

        // Generate treasure.
        GenerateLoot( oCritter, nXPResult );

        //run quest stuff
        int nNextState = qst_check( oKiller );
        qst_resolve_party( oKiller, nNextState );
    }
}
