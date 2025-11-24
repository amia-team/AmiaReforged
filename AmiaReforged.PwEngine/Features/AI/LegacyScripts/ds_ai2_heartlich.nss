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
    int nSunkill        = GetLocalInt( oCritter, F_SUNKILL );
    int nCount          = GetLocalInt( oCritter, L_INACTIVE );
    effect eHeal = EffectHeal(10000);

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

    // If the lich has been inactive for 5 minutes, heal and reset it
    if ( nCount >= 50)
    {

      if((GetPercentageHPLoss(oCritter) != 100))
      {
       ApplyEffectToObject(DURATION_TYPE_INSTANT, eHeal, oCritter, 0.0);
       object oAltar = GetWaypointByTag("necroboss");
       AssignCommand( oCritter, ClearAllActions() );
       AssignCommand( oCritter, JumpToObject(oAltar));
       DeleteLocalInt(oCritter,"1%AbilityFired");
       DeleteLocalInt(oCritter,"10%AbilityFired");
       DeleteLocalInt(oCritter,"20%AbilityFired");
       DeleteLocalInt(oCritter,"30%AbilityFired");
       DeleteLocalInt(oCritter,"40%AbilityFired");
       DeleteLocalInt(oCritter,"50%AbilityFired");
       DeleteLocalInt(oCritter,"60%AbilityFired");
       DeleteLocalInt(oCritter,"70%AbilityFired");
       DeleteLocalInt(oCritter,"80%AbilityFired");
       DeleteLocalInt(oCritter,"90%AbilityFired");

       // Unlock outside doors so noone else can come in
       object oDoor1 = GetObjectByTag ("ost_msc_3");
       AssignCommand(oDoor1,ActionUnlockObject(oDoor1));
       SetLockKeyRequired(oDoor1, FALSE);
      }

      SetLocalInt( oCritter, L_INACTIVE, 0 );

    }


}
