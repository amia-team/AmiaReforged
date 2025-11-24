//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_endround
//group:   ds_ai
//used as: OnEndOfRound
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

    object oCritter         = OBJECT_SELF;
    object oCurrentTarget   = GetLocalObject( oCritter, L_CURRENTTARGET );
    int nCount              = GetLocalInt( OBJECT_SELF, L_INACTIVE );

    if ( nCount == 0 || nCount > 1 ){

        if ( PerformAction( oCritter ) > 0 ){

            SetLocalInt( OBJECT_SELF, L_INACTIVE, 0 );
        }
    }
}
