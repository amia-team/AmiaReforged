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
    int nAbility        = GetLocalInt( oCritter, F_SPECIAL );

    //Declare major variables
    ClearAllActions();

    if ( nAbility == 1 ){

        effect eKnockDown = EffectKnockdown();
        effect eVis       = EffectVisualEffect(VFX_IMP_PULSE_WIND);
        effect eDam;
        int nDamage;
        int nDC           = GetHitDice( oCritter );
        location lSelf    = GetLocation( oCritter );
        location lTarget;
        float fRandomKnockdown;
        float fDelay;
        string sMessage   = GetName( oCritter ) + " is using its wing buffet against you!";

        // Pulse of wind applied...
        ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVis, lSelf);

        //Apply the VFX impact and effects
        //Get first target in spell area
        object oTarget = GetFirstObjectInShape( SHAPE_SPHERE, RADIUS_SIZE_GARGANTUAN, lSelf );

        while ( GetIsObjectValid( oTarget ) ) {

            lTarget = GetLocation( oTarget );
            fDelay  = GetDistanceToObject( oTarget ) / 20.0;

            // Wind pulse to all
            DelayCommand( fDelay, ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVis, lTarget ) );

            //Get next target in spell area
            // Do not effect allies.
            if ( !GetIsFriend( oTarget ) && !GetFactionEqual( oTarget ) ){

                SendMessageToPC( oTarget, sMessage );

                if ( GetCreatureSize( oTarget ) != CREATURE_SIZE_HUGE ) {

                    if ( !ReflexSave( oTarget, nDC ) ) {

                        // Randomise damage. (nDC = Hit dice)
                        nDamage = Random( nDC ) + 11;

                        eDam = EffectDamage(nDamage, DAMAGE_TYPE_BLUDGEONING);

                        // Randomise knockdown, to minimum of 6.0 (1.0/2 = 0.5. + 5.5 = 6.0)
                        fRandomKnockdown = 6.0 + IntToFloat( Random( 13 ) );

                        // We'll have a windy effect..depending on range
                        DelayCommand(fDelay, ApplyEffectToObject(DURATION_TYPE_INSTANT, eDam, oTarget ) );
                        DelayCommand(fDelay, ApplyEffectToObject(DURATION_TYPE_TEMPORARY, eKnockDown, oTarget, fRandomKnockdown));
                    }
                }
            }

            oTarget = GetNextObjectInShape(SHAPE_SPHERE, RADIUS_SIZE_GARGANTUAN, lSelf);
        }


        // Do a great flapping  wings on land effect.
        effect eAppear = EffectAppear();
        ApplyEffectToObject( DURATION_TYPE_INSTANT, eAppear, oCritter );
    }

    DelayCommand( 3.0, ExecuteScript( "ds_ai2_endround", oCritter ) );
}
