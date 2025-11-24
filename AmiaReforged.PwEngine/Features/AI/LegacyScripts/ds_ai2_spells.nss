//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai2_spells
//group:   ds_ai
//used as: Custom spell scripts
//date:    dec 23 2007
//author:  disco


//-------------------------------------------------------------------------------
// constants
//-------------------------------------------------------------------------------
//spells
const int SPELL_WEREWOLF        = 856;


//-------------------------------------------------------------------------------
// main
//-------------------------------------------------------------------------------
void main(){



    object oCritter = GetLastSpellCaster();
    object oTarget  = GetSpellTargetObject();
    int nSpell      = GetSpellId();
    effect eVis;
    effect eSpell;

    if ( nSpell == SPELL_WEREWOLF ){

        eVis   = EffectVisualEffect( VFX_IMP_POLYMORPH );
        eSpell = EffectPolymorph( 123 );

        SignalEvent( oTarget, EventSpellCastAt( OBJECT_SELF, nSpell, FALSE ) );

        //Apply the VFX impact and effects
        AssignCommand(oTarget, ClearAllActions()); // prevents an exploit
        ApplyEffectToObject( DURATION_TYPE_INSTANT, eVis, oTarget );
        ApplyEffectToObject( DURATION_TYPE_PERMANENT, eSpell, oTarget );
    }
}
