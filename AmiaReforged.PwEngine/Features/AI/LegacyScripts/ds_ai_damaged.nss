//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_damaged
//group:   ds_ai
//used as: OnDamage
//date:    dec 23 2007
//author:  disco


//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "ds_ai_include"


//-------------------------------------------------------------------------------
// main
//-------------------------------------------------------------------------------
void main(){

    object oCritter     = OBJECT_SELF;
    object oDamager     = GetLastDamager();
    object oTarget      = GetLocalObject( oCritter, L_CURRENTTARGET );
    int nArchetype      = GetLocalInt( oCritter, L_ARCHETYPE );

    SetLocalObject( oCritter, L_LASTDAMAGER, oDamager );

    if ( ( d10() + 2 ) < nArchetype ){

        if ( GetDistanceBetween( oCritter, oDamager ) < 5.0 ){

            ClearAllActions();
            ActionMoveAwayFromObject( oDamager, TRUE, 5.0 );
            SetLocalObject( oCritter, "L_CURRENTTARGET", oDamager );
        }
    }
    else{

        if ( oTarget != oDamager ){

            if ( GetObjectSeen( oDamager, oCritter ) && ( d100() - 20 ) < BREAKCOMBAT ){

                SetLocalObject( oCritter, "L_CURRENTTARGET", oDamager );
            }
        }
    }
}
