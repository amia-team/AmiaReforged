//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_cleanup
//group:   ds_ai
//used as: OnClose of treasure container or critter
//date:    may 08 2007
//author:  disco


//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "ds_ai2_include"


//-------------------------------------------------------------------------------
// main
//-------------------------------------------------------------------------------
void main(){

    object oObject      = OBJECT_SELF;
    int nMarkedForDeath = GetLocalInt( oObject, L_DESTROY );

    //-------------------------------------------------------------------------------
    // this part is for lootbins
    //-------------------------------------------------------------------------------

    object oPC          = GetLastClosedBy();

    if( GetFirstItemInInventory( oObject ) == OBJECT_INVALID ){

        DestroyObject( oObject );
    }
    else if ( !nMarkedForDeath ){

        SendMessageToPC( oPC, "[NB: this lootbag will be deleted in 2 minutes!]" );

        DelayCommand( 120.0, SafeDestroyObject2( oObject ) );

        SetLocalInt( oObject, L_DESTROY, 1 );
    }

    return;
}
