//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_attacked
//group:   ds_ai
//used as: OnAttack
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
    object oAttacker    = GetLastAttacker();
    object oTarget      = GetLocalObject( oCritter, L_CURRENTTARGET );
    int nArchetype      = GetLocalInt( oCritter, L_ARCHETYPE );

    //set reputation
    if ( GetIsPC( oAttacker ) || GetIsPC( GetMaster( oAttacker ) ) ){

        AdjustReputation( oAttacker, oCritter, -100 );
    }

    if ( d10() + 5 < nArchetype ){

        if ( GetDistanceBetween( oCritter, oAttacker ) < 5.0 ){

            ClearAllActions();
            ActionMoveAwayFromObject( oAttacker, TRUE, 5.0 );
            SetLocalObject( oCritter, "L_CURRENTTARGET", oAttacker );
        }
    }
    else{

        if ( oTarget != oAttacker ){

            if ( GetObjectSeen( oAttacker, oCritter ) && d100() < BREAKCOMBAT ){

                SetLocalObject( oCritter, "L_CURRENTTARGET", oAttacker );
            }
        }
    }
}
