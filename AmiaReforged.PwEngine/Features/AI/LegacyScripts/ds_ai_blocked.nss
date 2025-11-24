//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_blocked
//group:   ds_ai
//used as: OnBlocked
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
    object oDoor        = GetBlockingDoor();

    if ( GetIsDoorActionPossible( oDoor, DOOR_ACTION_OPEN ) ){

        //ClearAllActions( FALSE );

        if ( GetLocked( oDoor ) ){

            SpeakString( "*bangs on the locked door*" );
        }
        else if( GetAbilityScore( OBJECT_SELF, ABILITY_INTELLIGENCE) >= 5 ){

            SpeakString( "*opens door*" );

            AssignCommand( oDoor, ActionPlayAnimation( ANIMATION_DOOR_OPEN1 ) );
        }
        else{

            SpeakString( "*is too stupid to open a door*" );
        }
    }
}
