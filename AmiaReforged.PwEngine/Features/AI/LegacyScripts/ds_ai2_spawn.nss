//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_spawn
//group:   ds_ai
//used as: OnSpawn
//date:    dec 23 2007
//author:  disco

//  20071119  disco       Moved cleanup scripts to amia_include
//  2013/11/13 Glim       Added functionality for special ability usage onspawn.

//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "ds_ai2_include"
#include "inc_spwn_effects"

//-------------------------------------------------------------------------------
// main
//-------------------------------------------------------------------------------

void main(){

    object oCritter         = OBJECT_SELF;
    float scale             = GetLocalFloat(oCritter, "scale");
    float zAdjust           = GetLocalFloat(oCritter, "z_adjust");
    int spawnEffect         = GetLocalInt( oCritter, "spawn_effect" );
    int effect1             = GetLocalInt( oCritter, "effect1" );
    int effect2             = GetLocalInt( oCritter, "effect2" );
    int effect3             = GetLocalInt( oCritter, "effect3" );
    int effectDurType1      = GetLocalInt( oCritter, "effect_type1" );
    int effectDurType2      = GetLocalInt( oCritter, "effect_type2" );
    int effectDurType3      = GetLocalInt( oCritter, "effect_type3" );
    int collision           = GetLocalInt( oCritter, "no_collision" );
    float effectDur1        = GetLocalFloat( oCritter, "effect_dur1" );
    float effectDur2        = GetLocalFloat( oCritter, "effect_dur2" );
    float effectDur3        = GetLocalFloat( oCritter, "effect_dur3" );


    //Check if creature has an on-spawn effect.
    if (spawnEffect == TRUE)
    {
        effect mob1 = EffectFromConstant(effect1);
        effect mob2 = EffectFromConstant(effect2);
        effect mob3 = EffectFromConstant(effect3);

        ApplyEffectToObject(effectDurType1, mob1, oCritter, effectDur1);
        ApplyEffectToObject(effectDurType2, mob2, oCritter, effectDur2);
        ApplyEffectToObject(effectDurType3, mob3, oCritter, effectDur3);
    }

    //Accounting for floating point error.
    if(scale > 0.05f)
    {
        SetObjectVisualTransform(oCritter, 10, scale);
    }

    if(zAdjust > 0.05f || zAdjust < -0.05){
        SetObjectVisualTransform(oCritter, 33, zAdjust);
    }

    if(collision == 1){
        effect eGhost = EffectCutsceneGhost();
        ApplyEffectToObject(DURATION_TYPE_PERMANENT, eGhost, oCritter);
    }

    DelayCommand( SPAWNBUFFDELAY, OnSpawnRoutines( oCritter ) );

    CreateMySpellLists( oCritter );

    SetLocalString( oCritter, "ai", "ds_ai2" );

    //silent communication
    SetListening( OBJECT_SELF, TRUE );
    SetListenPattern( OBJECT_SELF, M_ATTACKED, 1001 );

    //set TS on self if it's on the hide.
    //you can't detect hide properties like effects
    object oHide = GetItemInSlot( INVENTORY_SLOT_CARMOUR, oCritter );

    itemproperty IP = GetFirstItemProperty( oHide );

    while( GetIsItemPropertyValid( IP ) ){

        if( GetItemPropertyType( IP ) == ITEM_PROPERTY_TRUE_SEEING ) {

            effect eTS = SupernaturalEffect( EffectTrueSeeing() );

            ApplyEffectToObject( DURATION_TYPE_PERMANENT, eTS, oCritter );

            return;
        }

        IP = GetNextItemProperty( oHide );
    }

    // Spawn in Raid Loot for Global Bosses

    if(GetTag(oCritter) == "GlobalBoss")
    {
      string LootDropResRef = "glob_base_res";
      object LootDrop = CreateItemOnObject(LootDropResRef,oCritter);
      SetDroppableFlag(LootDrop,TRUE);
    }


}
