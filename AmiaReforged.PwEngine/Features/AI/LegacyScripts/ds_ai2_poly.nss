//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai2_attacked
//group:   ds_ai2
//used as: OnAttack
//date:    march 20 2009
//author:  disco

//2009-01-01  disco  added archer fix

//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "ds_ai2_include"

//-------------------------------------------------------------------------------
// main
//-------------------------------------------------------------------------------
void main(){

    object oCritter     = OBJECT_SELF;
    object oTarget      = GetLocalObject( oCritter, L_CURRENTTARGET );

    if ( GetLocalInt( oCritter, "lalala" ) != 1 ){

        ClearAllActions( TRUE );

        DelayCommand( 6.0, ActionAttack( oTarget ) );

        SetLocalInt( oCritter, "lalala", 1 );
    }
    else{

        ActionAttack( oTarget );
    }


    //DelayCommand( 3.0, ExecuteScript( "ds_ai2_endround", oCritter ) );
}
