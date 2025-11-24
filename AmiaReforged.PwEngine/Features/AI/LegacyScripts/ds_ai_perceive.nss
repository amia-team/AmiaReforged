//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_perceive
//group:   ds_ai
//used as: OnPerception
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

    object oCritter = OBJECT_SELF;
    object oTarget  = GetLastPerceived();
    string sVar     = L_PERCEIVED+GetStringLeft( GetName( oTarget ), 12 );
    int nCount      = GetLocalInt( OBJECT_SELF, L_INACTIVE );

    if ( GetLastPerceptionSeen() && GetIsPC( oTarget ) ){

        SetLocalInt( oCritter, sVar, 1 );

        //heartbeats start when players are around. This is against NPCs
        if ( ( nCount == 0 || nCount > 5 ) && GetIsEnemy( oTarget ) ){

            if ( PerformAction( oCritter ) > 0 ){

                SetLocalInt( OBJECT_SELF, L_INACTIVE, 0 );
            }
        }
    }
    else if ( GetLastPerceptionVanished() && GetIsPC( oTarget ) ){

        SetLocalInt( oCritter, sVar, -1 );
    }
}
