//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_spawn
//group:   ds_ai
//used as: OnSpawn
//date:    dec 23 2007
//author:  disco

//  20071119  disco       Moved cleanup scripts to amia_include

//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "ds_ai_include"

//-------------------------------------------------------------------------------
// main
//-------------------------------------------------------------------------------

void main(){

    object oCritter = OBJECT_SELF;

    DelayCommand( SPAWNBUFFDELAY, OnSpawnBuff( oCritter ) );

    MakeSpellList( oCritter );

    SetLocalString( oCritter, "ai", "ds_ai" );

}
