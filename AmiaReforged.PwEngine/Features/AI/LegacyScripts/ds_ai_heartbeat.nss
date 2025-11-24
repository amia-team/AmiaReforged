//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_heartbeat
//group:   ds_ai
//used as: OnBlocked
//date:    dec 23 2007
//author:  disco


//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "ds_ai_include"

void main(){

    object oCritter     = OBJECT_SELF;
    int nCount          = GetLocalInt( oCritter, L_INACTIVE );

    if ( nCount > 5 ){

        //go into sleep mode after 5 rounds of inactivity
        //until a perception or combat event is triggered
        //which resets L_INACTIVE to 1
        SetLocalInt( oCritter, L_INACTIVE, (nCount + 1) );
        return;
    }
    else if ( nCount == 100 ){

        //Warn a DM about inactive spawns
        SendMessageToAllDMs( "DS AI message: "+GetName( oCritter )+" in "+GetName( GetArea( oCritter ) )+ " has been inactive for 10 minutes now." );
        SetLocalInt( oCritter, L_INACTIVE, (nCount + 1) );
        return;
    }
    else if ( PerformAction( oCritter ) < 1 ){

        //perform an action: nothing gets done, so critter is marked as inactive
        SetLocalInt( oCritter, L_INACTIVE, (nCount + 1) );
    }
    else{

        //perform an action: something gets done, so critter is marked as active
        //by resetting L_INACTIVE to 1
        SetLocalInt( oCritter, L_INACTIVE, 1 );
    }
}
