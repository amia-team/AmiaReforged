//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai2_heartbeat
//group:   ds_ai2
//used as: AI
//date:    dec 23 2007
//author:  disco

// Edit: Frostspear heartbeat script - Maverick00053
//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "ds_ai2_include"

void main(){

    object oCritter     = OBJECT_SELF;
    object oWayPoint;
    int nSunkill        = GetLocalInt( oCritter, F_SUNKILL );
    int nCount          = GetLocalInt( oCritter, L_INACTIVE );
    effect eHeal = EffectHeal(5000);

    SetLocalInt( oCritter, L_INACTIVE, (nCount + 1) );

    if ( nSunkill ){

        object oArea = GetArea( oCritter );

        if ( GetIsDay() &&
            !GetIsAreaInterior( oArea )  &&
            GetIsAreaAboveGround( oArea ) ){

            effect eVis    = EffectVisualEffect( VFX_IMP_FLAME_M );
            effect eDamage = EffectDamage( d10( 10 ), DAMAGE_TYPE_FIRE );

            ApplyEffectToObject( DURATION_TYPE_INSTANT, eVis, oCritter );
            ApplyEffectToObject( DURATION_TYPE_INSTANT, eDamage, oCritter );

            SpeakString( "*gets sunburned*" );
        }
    }

    // If the dragon has been inactive for 1 minute, heal and reset it. This will combat people quickly respawning and rushing her
    // to finish her off.
    if ( nCount >= 10)
    {

      if((GetTag(oCritter) != "Phase1") || (GetPercentageHPLoss(oCritter) != 100))
      {
        oWayPoint = GetWaypointByTag("dragonreturn");
        CreateObject(OBJECT_TYPE_CREATURE,"whitedragonboss",GetLocation(oWayPoint), TRUE, "Phase1");
        DestroyObject(OBJECT_SELF,0.1);

      }

      SetLocalInt( oCritter, L_INACTIVE, 0 );

    }


}
