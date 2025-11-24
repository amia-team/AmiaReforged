//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_convo
//group:   ds_ai
//used as: OnConversation
//date:    jan 20 2008
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
    object oSpeaker = GetLastSpeaker();

    //undead summon stuff
    if ( GetMaster( oCritter ) == oSpeaker && GetLocalInt( oCritter, "is_undead" ) == 1 ){

        if ( GetLastAssociateCommand( oCritter ) == ASSOCIATE_COMMAND_FOLLOWMASTER ) {

            SpeakString( "Hurghhh hhuh..." );
            ClearAllActions();
            ActionForceFollowObject( oSpeaker );
            return;
        }
        else if ( GetLastAssociateCommand( oCritter ) == ASSOCIATE_COMMAND_STANDGROUND ) {

            SpeakString( "Buhrhh ghaarg..." );
            ClearAllActions();
            return;
        }
    }
}
