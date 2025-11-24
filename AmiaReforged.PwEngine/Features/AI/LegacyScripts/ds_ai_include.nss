//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_include
//group:   ds_ai
//used as: include
//date:    dec 23 2007
//author:  disco


//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "x0_i0_talent"
#include "ds_inc_randstore"

//-------------------------------------------------------------------------------
// constants
//-------------------------------------------------------------------------------

//local variable names
const string L_CURRENTTARGET    = "ds_ai_target";    //also used in amia_include!
const string L_LASTDAMAGER      = "ds_ai_damager";
const string L_ARCHETYPE        = "ds_ai_archetype";
const string L_LASTSPELL        = "ds_ai_last_spell";
const string L_FEATBUFF         = "ds_ai_featbuff";
const string L_DESTROY          = "ds_ai_destroy";
const string L_MAXCL            = "ds_ai_maxcl";
const string L_VFX              = "ds_ai_vfx";
const string L_PERCEIVED        = "ds_ai_p";
const string L_INACTIVE         = "ds_ai_i";
const string L_ISDEAD           = "ds_ai_dead";
const string L_DISTANCE         = "ds_ai_distance";
const string L_BLOCKED          = "ds_ai_blocked";

//limit castings for each spell to this value
const int SPAM_LIMIT         = 2;

//Delay for applying buffs on spawn
const float SPAWNBUFFDELAY    = 1.0;

//chance to break combat and pursue another attacker
const int BREAKCOMBAT         = 50;

//filters for mage/fighter actions
const int ARCHETYPE_LOW     = 3;    // fighters
const int ARCHETYPE_HIGH    = 7;    // mages

//-------------------------------------------------------------------------------
// prototypes: spawn & death functions
//-------------------------------------------------------------------------------

//perception doesn't work on spawning, so we just buff
void OnSpawnBuff( object oCritter );

//safely cleanup oTarget
void SafeDestroyObject( object oTarget );

//set onSpawn effects
void SetOnSpawnEffects( object oCritter );

// moves critter towards CURRENTTARGET
// this is used when the citter is spawned
int MoveToTriggerEvent( object oCritter, int nArchetype );


//-------------------------------------------------------------------------------
// prototypes: targeting functions
//-------------------------------------------------------------------------------

//checks if current target is valid and attemps to find new one if not
//returns OBJECT_INVALID when it cannot find a new target
object GetTarget( object oCritter );

//safe version of GetIsEnemy
//return values:
// -2: error, no reputation
// -1: invalid, dead, or plot
//  0: not hostile
//  1: hostile & undetectable
//  2: hostile & heard
//  3: hostile & seen
int GetIsValidHostile( object oCritter, object oTarget );

// Checks if oTarget is detectable by oCritter.
// Use this in pre-perception checks.
// NB: It doesn't say IF the creature is seen or heard
//return values:
// -1: invalid target
//  0: undetectable
//  1: can be heard
//  2: can be seen
//  3: can be seen and be heard
int GetIsDetectable( object oCritter, object oTarget );

//tries to find a suitable enemy before or after perception event
object FindSingleTarget( object oCritter );

//-------------------------------------------------------------------------------
// prototypes: action functions
//-------------------------------------------------------------------------------

//performs an action based on round and archetype
//return values:
//-1 when no valid target is around
// 0 when no action is performed
// 1 when an action is performed
int PerformAction( object oCritter );

//attempt to use melee type buff feats and return 1 if success
int DoFeatBuff( object oCritter );

//try to heal and return 1 if success
int DoSpellHeal( object oCritter, object oTarget );

//try to buff first and return 1 if success
int DoSpellBuff( object oCritter, object oTarget );

//try to summon and return 1 if success
int DoSpellSummon( object oCritter, object oTarget );

//try to cast an offensive spell and return 1 if success
int DoSpellAttack( object oCritter, object oTarget );

//simple attack and return 1 if attacking
int DoAttack( object oCritter, object oTarget, int bPassive );

//perform special attack and return 1 if success
int DoSpecialAttack( object oCritter, object oTarget );


//-------------------------------------------------------------------------------
// prototypes: spell functions
//-------------------------------------------------------------------------------

//try to cast a cached spell of sType and return 1 if casting
int DoSpellCast( object oCritter, object oTarget, string sType );

//creates cache of spells on Critter
void MakeSpellList( object oCritter );

//returns spell from cache of spells on Critter
int ParseSpellList( object oCritter, object oTarget, string sReturnType );

//makes fake CL to promote use of specific spells
int GetCorrectedCL( int nSpell, int nCL );

//files spells in a few types
string GetSpellType( object oCritter, object oTarget, int nCategory, int nSpell );

//unfucks a few spells
string PrePostFilter( object oCritter, object oTarget, int nSpell, int nPostCast );

//makes sure Undead get the right spells
string FixSpellsVersusUndead( object oCritter, object oTarget, int nCategory, int nSpell );

//check if the spell list and combat situation allow for casting
//return values suggest the suitable action:
// -1 Don't cast but move away
//  0 Don't cast but stand ground
//  1 Cast away
int GetAllowCasting( object oCritter, object oTarget );


//-------------------------------------------------------------------------------
// prototypes: utility functions
//-------------------------------------------------------------------------------

//this returns a float between 1 (100% fighter) and 10 (100% mage)
int GetArchetype( object oCritter );

//helper function for GetArchetype
int GetClassFactor( object oCritter, int nPosition );

//attempt to use feat and return TRUE if success
int SafeUseFeat( object oCritter, object oTarget, int nFeat );

// Wrapper around speakstring
// Feedback level is a local int on the critter that returns all nStatus messages.
// 1. Perceive events
// 2. Actions
// 3. Spells
void DebugMessage( string sMessage, int nStatus );


//-------------------------------------------------------------------------------
// onspawn and death functions
//-------------------------------------------------------------------------------

void OnSpawnBuff( object oCritter ){

    //set effects
    SetOnSpawnEffects( oCritter );

    //get the ratio between fighter and mage levels
    int nArchetype = GetArchetype( oCritter );

    //set magic/melee factor on critter
    SetLocalInt( oCritter, L_ARCHETYPE, nArchetype );

    //set bonus buff
    int nSpell     = ParseSpellList( oCritter, OBJECT_INVALID, "buff" );

    ActionCastSpellAtObject( nSpell, oCritter, METAMAGIC_ANY, FALSE, 0, PROJECTILE_PATH_TYPE_DEFAULT, TRUE );

    SetLocalInt( oCritter, L_LASTSPELL, nSpell );

    if ( TalentBuffSelf() ){

        // take potion (if possible) and return
        return;
    }
    else if ( TalentBardSong() ){

        //Sing (if possible) and return
        return;
    }
    else if ( DoSpellCast( oCritter, oCritter, "buff" ) ){

        //Buff (if possible) and return
        return;
    }
    else if ( MoveToTriggerEvent( oCritter, nArchetype ) ){

        //move to trigger dude
    }
    else{

        PerformAction( oCritter );
    }
}

int MoveToTriggerEvent( object oCritter, int nArchetype ){

    object oTriggerer = GetLocalObject( oCritter, L_CURRENTTARGET );

    DebugMessage( "MoveToTriggerEvent: "+IntToString( GetObjectSeen( oTriggerer ) ), 1 );
    DebugMessage( "MoveToTriggerEvent: "+IntToString( GetObjectSeen( oTriggerer ) ), 2 );

    if ( GetIsValidHostile( oCritter, oTriggerer ) > 0 ){

        DoAttack( oCritter, oTriggerer, TRUE );
        return 1;
    }

    SetLocalObject( oCritter, L_CURRENTTARGET, OBJECT_INVALID );

    return 0;
}

/*void SafeDestroyObject( object oTarget ){

    if ( oTarget != OBJECT_INVALID ){

        object oItem = GetFirstItemInInventory( oTarget );

        while ( GetIsObjectValid( oItem ) == TRUE ){

            SetPlotFlag( oItem, FALSE );

            DestroyObject( oItem );

            oItem = GetNextItemInInventory( oTarget );
        }

        DestroyObject( oTarget, 0.5 );
    }
} */

void SetOnSpawnEffects( object oCritter ){

    int i       = 1;
    string sVar = L_VFX + "_" + IntToString( i );
    int nVal    = GetLocalInt( oCritter, sVar );
    effect eVis;

    while ( nVal > 0 ){

        eVis    = EffectVisualEffect( nVal );
        ApplyEffectToObject( DURATION_TYPE_PERMANENT, eVis, oCritter );

        ++i;
        sVar    = L_VFX + "_" + IntToString( i );
        nVal    = GetLocalInt( oCritter, sVar );

    }

    //function checks if critter should drop stuff
    InjectIntoStore( oCritter );
}

//-------------------------------------------------------------------------------
// targeting functions
//-------------------------------------------------------------------------------
object GetTarget( object oCritter ){

    object oTarget;
    object oCurrentTarget         = GetLocalObject( oCritter, L_CURRENTTARGET );
    int nIsCurrentValidHostile    = GetIsValidHostile( oCritter, oCurrentTarget );

    DebugMessage( "CurrentTarget = "+GetName( oCurrentTarget ), 1 );
    DebugMessage( "nIsCurrentValidHostile = "+IntToString( nIsCurrentValidHostile ), 1 );

    //-------------------------------------------------------------------------------
    //option 1: the current target is available and a valid hostile
    //-------------------------------------------------------------------------------
    if ( nIsCurrentValidHostile > 1 ){

        //we already have a suitable target. Check for a change of attention.
        int nAttentionSpan = 8 + nIsCurrentValidHostile;

        if ( !GetIsPC( oCurrentTarget ) || GetIsPossessedFamiliar( oCurrentTarget ) ){

            //critters have a preference for PCs
            nAttentionSpan = 4;
        }

        if ( d12() <= nAttentionSpan ){

            //no change in attention
            return oCurrentTarget;
        }
    }

    //-------------------------------------------------------------------------------
    //option 2: oCurrentTarget is not suitable or there's a change of attention
    //-------------------------------------------------------------------------------
    oTarget = GetNearestPerceivedEnemy();

    if ( GetIsValidHostile( oCritter, oTarget ) > 0 ){

        SetLocalObject( oCritter, L_CURRENTTARGET, oTarget );

        return oTarget;
    }
    else if ( nIsCurrentValidHostile > 0 ){

        //no change in attention after all
        return oCurrentTarget;
    }
    else{

        //let's check for another enemy
        oTarget = FindSingleTarget( oCritter );

        if ( GetIsValidHostile( oCritter, oTarget ) > 1 ){

            SetLocalObject( oCritter, L_CURRENTTARGET, oTarget );

            //pick this target now
            return oTarget;
        }
    }

    //delete current target on critter
    DeleteLocalObject( oCritter, L_CURRENTTARGET );

    return OBJECT_INVALID;
}

object FindSingleTarget( object oCritter ){

    int nCount;
    object oCount       = GetFirstObjectInShape( SHAPE_SPHERE, 30.0, GetLocation( oCritter ) );
    object oTarget1;
    object oTarget2;
    object oTarget3;

    //cheack for close by targets
    while ( GetIsObjectValid( oCount ) ) {

        if ( oCount != oCritter ){

            if( GetIsValidHostile( oCritter, oCount ) > 0 ){

                ++nCount;

                if ( nCount == 1 ){

                    oTarget1 = oCount;
                }
                else if ( nCount == 2 ){

                    oTarget2 = oCount;
                }
                else if ( nCount == 3 ){

                    oTarget3 = oCount;
                }
                else{

                    break;
                }
            }
        }

        oCount = GetNextObjectInShape( SHAPE_SPHERE, 30.0, GetLocation( oCritter ) );
    }

    //pick a random target out of the available list
    int nDie = Random( nCount ) + 1;

    if ( nDie == 1 ){

        return oTarget1;
    }
    else if ( nDie == 2 ){

        return oTarget2;
    }
    else if ( nDie == 3 ){

        return oTarget3;
    }

    return OBJECT_INVALID;
}

int GetIsValidHostile( object oCritter, object oTarget ){

    if ( oTarget == OBJECT_INVALID ){

        return -2;
    }
    else if ( GetPlotFlag( oTarget ) || GetIsDead( oTarget ) || GetImmortal( oTarget ) ){

        return -1;
    }
    else if ( !GetIsEnemy( oTarget, oCritter ) ){

        return 0;
    }
    else if ( GetObjectSeen( oTarget, oCritter ) ){

        return 3;
    }

    //this means no perception check has been made yet
    //or the critter has disappeared from perception
    int nDetectable = GetIsDetectable( oCritter, oTarget );

    if ( nDetectable == 0 ){

        DebugMessage( "GetIsValidHostile: Undetectable", 1 );
        return 1;
    }
    else if ( nDetectable == 1 ){

        DebugMessage( "GetIsValidHostile: Not seen", 1 );
        return 2;
    }
    else if ( nDetectable > 2 ){

        DebugMessage( "GetIsValidHostile: Seen", 1 );
        return 3;
    }

    return -2;
}

int GetIsDetectable( object oCritter, object oTarget ){

    if ( !GetIsObjectValid( oTarget ) ){

        return -1;
    }
    else if ( GetHasEffect( EFFECT_TYPE_SANCTUARY, oTarget ) ){

        //GS-ed
        DebugMessage( "GetIsDetectable: GS-ed target", 1 );
        return 0;
    }
    else if ( GetHasEffect( EFFECT_TYPE_INVISIBILITY, oTarget ) ){

        //invis
        DebugMessage( "GetIsDetectable: Invis target", 1 );

        float fDistance = GetDistanceToObject( oTarget );

        if ( fDistance > 0.0 && fDistance < 6.0 ){

            //this is not really 'seen' but alas
            return 3;
        }
        else{

            return 0;
        }
    }

    //check if the object has been perceived or not
    string sVar     = L_PERCEIVED+GetStringLeft( GetName( oTarget ), 12 );
    int nPerceived  = GetLocalInt( oCritter, sVar );
    int nResult;

    if ( nPerceived == 1 ){

        //certain to be seen
        DebugMessage( "GetIsDetectable: 3", 1 );
        nResult = 3;
    }
    else if ( nPerceived == -1 ){

        //certain to be vanished
        DebugMessage( "GetIsDetectable: 0", 1 );
        nResult = 0;
    }
    else{

        //this is either not a PC or a pre-perception event

        //check stealth status
        int nStealthed  = GetStealthMode( oTarget );
        float fDistance = GetDistanceToObject( oTarget );

        if ( nStealthed == STEALTH_MODE_ACTIVATED ){

            int nSpot = GetSkillRank( SKILL_SPOT, oCritter ) + d20();
            int nHide = GetSkillRank( SKILL_SPOT, oTarget ) + d20();

            if ( nSpot > nHide && fDistance < 15.0 ){

                nResult = 2;
            }

            int nListen = GetSkillRank( SKILL_LISTEN, oCritter ) + d20();
            int nSneak  = GetSkillRank( SKILL_MOVE_SILENTLY, oTarget ) + d20();

            if ( nListen > nSneak && fDistance < 5.0 ){

                nResult = nResult + 1;
            }
        }
        else{

            nResult = 3;
        }
    }

    DebugMessage( "GetIsDetectable: "+IntToString( nResult ), 1 );

    return nResult;
}


//-------------------------------------------------------------------------------
// action functions
//-------------------------------------------------------------------------------

int PerformAction( object oCritter ){

    //stop doing what you are doing now.
    ClearAllActions( TRUE );

    object oTarget = GetTarget( oCritter );
    int nResult;

    DebugMessage( "PerformAction Target = "+GetName( oTarget ), 2 );

    if ( oTarget == OBJECT_INVALID ){

        return -1;
    }

    int nDie        = d6();

    //check if there are spells to cast
    int nAllowCasting = GetAllowCasting( oCritter, oTarget );


    if ( nAllowCasting == 1 ){

        DebugMessage( "AllowCasting", 2 );

        //random set of preferences
        if ( nDie == 4 ){

            nResult = DoSpellBuff( oCritter, oTarget );
        }
        else if ( nDie == 5 ){

            nResult = DoSpellHeal( oCritter, oTarget );
        }
        else if ( nDie == 6 ){

            nResult = DoSpellSummon( oCritter, oTarget );
        }

        if ( nResult != 1 ){

            nResult = DoSpellAttack( oCritter, oTarget );
        }

        if ( nResult == 1 ){

             return 1;
        }
    }

    if ( nAllowCasting == -1 ){

        //move away for one round
        ActionMoveAwayFromObject( oTarget, TRUE, 10.0 );

         return 1;
    }

    //buff from feats
    if ( DoFeatBuff( oCritter ) == 1 ){

         return 1;
    }

    //perform special attack
    if ( DoSpecialAttack( oCritter, oTarget ) == 1 ){

         return 1;
    }

    //basic attack
    int nAttack = DoAttack( oCritter, oTarget, FALSE );

    if ( nAttack == 1 ){

         return 1;
    }
    else if ( nAttack == -1 ){

        return 0;
    }

    //move towards critter if heard/seen
    if ( GetIsDetectable( oCritter, oTarget ) > 0 ){

         ActionMoveToObject( GetLocalObject( oCritter, L_CURRENTTARGET ), TRUE, 2.0 );

         return 1;
    }

    return 0;
}

int DoSpellHeal( object oCritter, object oTarget ){

    DebugMessage( "DoSpellHeal", 2 );

    if ( GetCurrentHitPoints( oCritter ) < ( GetMaxHitPoints( oCritter ) / 2 ) ){

        if ( DoSpellCast( oCritter, oCritter, "heal" ) ){

            //healed with spell
            return 1;
        }
        else if ( TalentHealingSelf() ){

            //healed with potion
            return 1;
        }

        return 0;
    }

    return 0;
}

int DoSpellBuff( object oCritter, object oTarget ){

    DebugMessage( "DoSpellBuff", 2 );

    if ( DoSpellCast( oCritter, oTarget, "buff" ) ){

        return 1;
    }

    return 0;
}

int DoFeatBuff( object oCritter ){

    DebugMessage( "DoFeatBuff", 2 );

    int nFeatBuffed = GetLocalInt( oCritter, L_FEATBUFF );

    if ( nFeatBuffed == 0 ){

        SetLocalInt( oCritter, L_FEATBUFF, 1 );

        if ( SafeUseFeat( oCritter, oCritter, FEAT_BARBARIAN_RAGE ) ){ return 1; }
        if ( SafeUseFeat( oCritter, oCritter, FEAT_DIVINE_SHIELD ) ){ return 1; }
        if ( SafeUseFeat( oCritter, oCritter, FEAT_DIVINE_MIGHT ) ){ return 1; }
        if ( SafeUseFeat( oCritter, oCritter, FEAT_DWARVEN_DEFENDER_DEFENSIVE_STANCE ) ){ return 1; }
    }

    return 0;
}

int DoSpellSummon( object oCritter, object oTarget ){

    DebugMessage( "DoSpellSummon", 2 );

    if ( DoSpellCast( oCritter, oTarget, "summon" ) ){

        return 1;
    }

    return 0;
}

int DoSpellAttack( object oCritter, object oTarget ){

    DebugMessage( "DoSpellAttack", 2 );

    if ( DoSpellCast( oCritter, oTarget, "attack" ) ){

        return 1;
    }

    return 0;
}

int DoSpecialAttack( object oCritter, object oTarget ){

    int nDie = d12();

    if ( nDie < 9 ){

        return FALSE;
    }

    DebugMessage( "SpecialAttack", 2 );

    if ( nDie == 09 && SafeUseFeat( oCritter, oTarget, FEAT_KNOCKDOWN ) ){ return 1; }
    if ( nDie == 10 && SafeUseFeat( oCritter, oTarget, FEAT_CALLED_SHOT ) ){ return 1; }
    if ( nDie == 11 && SafeUseFeat( oCritter, oTarget, FEAT_DISARM ) ){ return 1; }
    if ( nDie == 12 && SafeUseFeat( oCritter, oTarget, FEAT_TURN_UNDEAD ) ){ return 1; }

    return 0;
}

int DoAttack( object oCritter, object oTarget, int bPassive  ){

    DebugMessage( "DoAttack", 2 );

    if ( GetIsValidHostile( oCritter, oTarget ) > 2 ){

        if ( GetIsMeleeAttacker( oCritter ) ){

            float fCurrentDistance  = GetDistanceBetween( oCritter, oTarget );
            float fPreviousDistance = GetLocalFloat( oCritter, L_DISTANCE );
            object oBlocked         = GetLocalObject( oCritter, L_BLOCKED );

            if ( fPreviousDistance != 0.0 ){

                if ( oBlocked != oTarget && GetIsObjectValid( oBlocked ) ){

                    DeleteLocalFloat( oCritter, L_DISTANCE );
                    DeleteLocalObject( oCritter, L_BLOCKED );
                    ActionAttack( oTarget );

                    return 1;
                }
                else if ( fCurrentDistance > 10.0 &&
                     fPreviousDistance > ( fPreviousDistance - 0.5 ) &&
                     fPreviousDistance < ( fPreviousDistance + 0.5 ) ){

                    DeleteLocalObject( oCritter, L_CURRENTTARGET );
                    SetLocalObject( oCritter, L_BLOCKED, oTarget );
                    SetLocalFloat( oCritter, L_DISTANCE, fCurrentDistance );

                    return 0;
                }
            }
            else if ( fCurrentDistance > 10.0 && fPreviousDistance == 0.0 ){

                SetLocalFloat( oCritter, L_DISTANCE, fCurrentDistance );
            }
            else{

                DeleteLocalFloat( oCritter, L_DISTANCE );
                DeleteLocalObject( oCritter, L_BLOCKED );
            }
        }

        ActionAttack( oTarget, bPassive );

        return 1;
    }

    ClearAllActions( TRUE );

    return 0;
}


//-------------------------------------------------------------------------------
// spell functions
//-------------------------------------------------------------------------------
int DoSpellCast( object oCritter, object oTarget, string sType ){

    DebugMessage( "CastSpell", 2 );

    int nResult    = FALSE;
    int nLastSpell = GetLocalInt( oCritter, L_LASTSPELL );
    int nSpell     = ParseSpellList( oCritter, oTarget, sType );

    //indicate that this spell has been used
    SetLocalInt( oCritter, L_LASTSPELL, nSpell );

    if ( nSpell == -1 ){

        //there are no spells of this type
        return 0;
    }

    if ( sType == "attack" ){

        if ( GetHasSpellEffect( nSpell, oTarget ) ){

            return 0;
        }

        if ( !GetObjectSeen( oTarget ) ){

            return 0;
        }

        //cast spell
        ActionCastSpellAtObject( nSpell, oTarget );
        ActionDoCommand( ActionCastSpellAtObject( nSpell, oTarget ) );
        PrePostFilter( oCritter, oTarget, nSpell, 1 );
        return 1;
    }
    else if ( sType == "buff" ){

        if ( nSpell == nLastSpell || GetHasSpellEffect( nSpell, oCritter ) ){

            return 0;
        }

        ActionDoCommand( ActionCastSpellAtObject( nSpell, oCritter ) );
        return 1;
    }
    else if ( sType == "healing" ){

        ActionDoCommand( ActionCastSpellAtObject( nSpell, oCritter ) );
        return 1;
    }
    else if ( sType == "summon" ){

        if ( GetAssociate( ASSOCIATE_TYPE_SUMMONED, oCritter ) != OBJECT_INVALID ){

            return 0;
        }

        ActionDoCommand( ActionCastSpellAtObject( nSpell, oTarget ) );
        return 1;
    }

    return 0;
}

void MakeSpellList( object oCritter ){

    int i                = 0;
    int nCL              = -1;
    int nMaxCL           = -1;
    int nCountCL0        = 0;
    int nCountCL1        = 0;
    int nCountCL2        = 0;
    int nCountCL3        = 0;
    int nCountCL4        = 0;
    int nCountCL5        = 0;
    int nCountCL6        = 0;
    int nCountCL7        = 0;
    int nCountCL8        = 0;
    int nCountCL9        = 0;
    int nCountCL10       = 0;
    string sLocalSpell   = "";
    string sCL           = "";

    for ( i=0; i<803; ++i ){

        if ( GetHasSpell( i ) ){

            sCL = Get2DAString( "spells", "Innate", i );

            if ( sCL != "" ){

                nCL = GetCorrectedCL( i, StringToInt( sCL ) );

                switch ( nCL ) {

                    case 0:     ++nCountCL0;  sLocalSpell = "ds_ai_0_"+IntToString( nCountCL0 );    break;
                    case 1:     ++nCountCL1;  sLocalSpell = "ds_ai_1_"+IntToString( nCountCL1 );    break;
                    case 2:     ++nCountCL2;  sLocalSpell = "ds_ai_2_"+IntToString( nCountCL2 );    break;
                    case 3:     ++nCountCL3;  sLocalSpell = "ds_ai_3_"+IntToString( nCountCL3 );    break;
                    case 4:     ++nCountCL4;  sLocalSpell = "ds_ai_4_"+IntToString( nCountCL4 );    break;
                    case 5:     ++nCountCL5;  sLocalSpell = "ds_ai_5_"+IntToString( nCountCL5 );    break;
                    case 6:     ++nCountCL6;  sLocalSpell = "ds_ai_6_"+IntToString( nCountCL6 );    break;
                    case 7:     ++nCountCL7;  sLocalSpell = "ds_ai_7_"+IntToString( nCountCL7 );    break;
                    case 8:     ++nCountCL8;  sLocalSpell = "ds_ai_8_"+IntToString( nCountCL8 );    break;
                    case 9:     ++nCountCL9;  sLocalSpell = "ds_ai_9_"+IntToString( nCountCL9 );    break;
                    case 10:    ++nCountCL10; sLocalSpell = "ds_ai_10_"+IntToString( nCountCL10 );  break;
                }

                if ( nMaxCL < nCL ){

                    nMaxCL = nCL;
                }

                // +1 offset because of Acid Fog (spell 0)
                SetLocalInt( oCritter, sLocalSpell, (i+1));

                //DelayCommand( 4.0, SpeakString( "Detected spell: "+IntToString( i )+" ("+Get2DAString( "spells", "Label", i ) + ")" ) );
            }
        }
    }

    //SpeakString( "CL max: "+IntToString( nMaxCL ) );

    SetLocalInt( oCritter, L_MAXCL, nMaxCL );
}

int ParseSpellList( object oCritter, object oTarget, string sReturnType ){

    int i;
    int j;
    int nSpell              = -1;
    int nSpellUsed          = 0;
    int nSelectedSpells     = 0;
    int nSelectedSpell1     = -1;
    int nSelectedSpell2     = -1;
    int nSelectedSpell3     = -1;
    int nReturnSpell        = -1;
    int nMaxCL              = GetLocalInt( oCritter, L_MAXCL );
    int nTempMaxCL          = -1;
    int nCL                 = -1;
    int nCategory           = -1;
    string sLocalSpell      = "";
    string sLocalSpellUsed  = "";
    string sType            = "";


    for ( i=nMaxCL; i>=0; --i ){

        for( j=1; j<100; ++j ){

            sLocalSpell = "ds_ai_"+IntToString( i )+"_"+IntToString( j );
            nSpell      = GetLocalInt( oCritter, sLocalSpell ) - 1;

            //DelayCommand( 5.0, SpeakString( sLocalSpell + ": "+IntToString( nSpell )+" ("+Get2DAString( "spells", "Label", nSpell ) + ")" ) );

            //no spells in caster level i
            if ( nSpell == -1 ){

                break;
            }

            //don't allow for more than SPAM_LIMIT castings
            sLocalSpellUsed = "ds_ai_used_"+IntToString( nSpell );
            nSpellUsed      = GetLocalInt( oCritter, sLocalSpellUsed );

            if ( nSpellUsed > SPAM_LIMIT ){

                DeleteLocalInt( oCritter, sLocalSpell );
                //DeleteLocalInt( oCritter, sLocalSpellUsed );

            }
            else{

                if ( GetHasSpell( nSpell ) ){

                    nCategory = StringToInt( Get2DAString( "spells", "Category", nSpell ) );
                    nCL       = GetCorrectedCL( nSpell, StringToInt( Get2DAString( "spells", "Innate", nSpell ) ) );
                    sType     = GetSpellType( oCritter, oTarget, nCategory, nSpell );

                    //SpeakString( IntToString( nSpell )+" - "+sType+" == "+sReturnType );

                    if ( sType == sReturnType && nSelectedSpells == 0 ){

                        nSelectedSpell1     = nSpell;
                        nSelectedSpells     = 1;
                    }
                    else if ( sType == sReturnType && nSelectedSpells == 1 ){

                        nSelectedSpell2     = nSpell;
                        nSelectedSpells     = 2;
                    }
                    else if ( sType == sReturnType && nSelectedSpells == 2 ){

                        nSelectedSpell3     = nSpell;
                        nSelectedSpells     = 3;
                    }

                    if ( nTempMaxCL < nCL ){

                        nTempMaxCL = nCL;
                    }
                }
            }

            if ( nSelectedSpells == 3 ){

                break;
            }
        }

        if ( nSelectedSpells == 3 ){

            break;
        }
    }

    //update nMaxCL
    SetLocalInt( oCritter, L_MAXCL, nTempMaxCL );

    nReturnSpell = Random( nSelectedSpells ) + 1;

    switch ( nReturnSpell ) {

        case 1:    return nSelectedSpell1;    break;
        case 2:    return nSelectedSpell2;    break;
        case 3:    return nSelectedSpell3;    break;
    }

    return -1;
}

string GetSpellType( object oCritter, object oTarget, int nCategory, int nSpell ){

    //undead
    if ( GetRacialType( oTarget ) == RACIAL_TYPE_UNDEAD ){

        return FixSpellsVersusUndead( oCritter, oTarget, nCategory, nSpell );
    }

    //bugged spells (ie: no category)
    if ( nCategory == 0 ){

        return PrePostFilter( oCritter, oTarget, nSpell, 0 );
    }

    switch ( nCategory ) {

        case 01: return "attack";   //Harmful_AOE_Discriminant
        case 02: return "attack";   //Harmful_Ranged
        case 03: return "attack";   //Harmful_Touch
        case 04: return "healing";  //Beneficial_Healing_AOE
        case 05: return "healing";  //Beneficial_Healing_Touch
        case 06: return "buff";     //Beneficial_Conditional_AOE
        case 07: return "buff";     //Beneficial_Conditional_Single
        case 08: return "buff";     //Beneficial_Enhancement_Area Effect
        case 09: return "buff";     //Beneficial_Enhancement_Single
        case 10: return "buff";     //Beneficial_Enhancement_Self
        case 11: return "attack";   //Harmful_AOE_Indiscriminant
        case 12: return "buff";     //TALENT_CATEGORY_BENEFICIAL_PROTECTION_SELF
        case 13: return "buff";     //TALENT_CATEGORY_BENEFICIAL_PROTECTION_SINGLE
        case 14: return "buff";     //TALENT_CATEGORY_BENEFICIAL_PROTECTION_AOE
        case 15: return "summon";   //TALENT_CATEGORY_BENEFICIAL_SUMMON
        case 16: return "buff";     //TALENT_CATEGORY_PERSISTENT_AREA_OF_EFFECT
        case 17: return "";         //TALENT_CATEGORY_BENEFICIAL_HEALING_POTION
        case 18: return "";         //TALENT_CATEGORY_BENEFICIAL_CONDITIONAL_POTION
        case 19: return "attack";   //TALENT_CATEGORY_DRAGONS_BREATH
        case 20: return "";         //TALENT_CATEGORY_BENEFICIAL_PROTECTION_POTION
        case 21: return "";         //TALENT_CATEGORY_BENEFICIAL_ENHANCEMENT_POTION
        case 22: return "";         //TALENT_CATEGORY_HARMFUL_MELEE
        case 23: return "attack";   //TALENT_DISPEL
    }

    return "";
}

string PrePostFilter( object oCritter, object oTarget, int nSpell, int nPostCast ){

    if ( nPostCast == 1 ){

        //Update spell uses. They all get SPAM_LIMIT uses.
        string sLocalSpellUsed  = "ds_ai_used_"+IntToString( nSpell );
        int nSpellUsed          = GetLocalInt( oCritter, sLocalSpellUsed );
        SetLocalInt( oCritter, "ds_ai_used_"+IntToString( nSpell ), nSpellUsed+1 );

        //post cast fixes
        switch ( nSpell ) {

        }
    }
    else{

        //precast fixes
        int nLastSpell = GetLocalInt( oCritter, L_LASTSPELL );

        if( nLastSpell == nSpell ){

            return "";
        }

        switch ( nSpell ) {

            case SPELL_INFLICT_CRITICAL_WOUNDS:     return "attack";
            case SPELL_INFLICT_LIGHT_WOUNDS:        return "attack";
            case SPELL_INFLICT_MINOR_WOUNDS:        return "attack";
            case SPELL_INFLICT_MODERATE_WOUNDS:     return "attack";
            case SPELL_INFLICT_SERIOUS_WOUNDS:      return "attack";
        }
    }

    //nothing happened
    return "";
}

string FixSpellsVersusUndead( object oCritter, object oTarget, int nCategory, int nSpell ){

    //we try to turn them first
    if ( SafeUseFeat( oCritter, oTarget, FEAT_TURN_UNDEAD ) ){

        return "";
    }

    //quick filter on a few unbehaving spells
    switch ( nSpell ) {

        case SPELL_INFLICT_CRITICAL_WOUNDS:     return "";
        case SPELL_INFLICT_LIGHT_WOUNDS:        return "";
        case SPELL_INFLICT_MINOR_WOUNDS:        return "";
        case SPELL_INFLICT_MODERATE_WOUNDS:     return "";
        case SPELL_INFLICT_SERIOUS_WOUNDS:      return "";
        case SPELL_NEGATIVE_ENERGY_RAY:         return "";
        case SPELL_NEGATIVE_ENERGY_BURST:       return "";
    }

    //healing spells can be used as attacks
    if( nCategory == 4 || nCategory == 5 ){

        return "attack";
    }
    else if ( nCategory == 1 ||
              nCategory == 2 ||
              nCategory == 3 ||
              nCategory == 4 ||
              nCategory == 11 ||
              nCategory == 19 ){

        //a lot of spells are obviously no affecting undead

        string sImmunity = Get2DAString( "spells", "ImmunityType", nSpell );

        if( sImmunity == "Negative" ||
            sImmunity == "Death" ||
            sImmunity == "Mind_Affecting"  ||
            sImmunity == "Disease"  ||
            sImmunity == "Poison" ){

            return "";
        }

        return "attack";
    }

    return "";
}

int GetCorrectedCL( int nSpell, int nCL ){

    switch ( nSpell ) {

        case SPELL_MAGIC_MISSILE:               return nCL+2;
        case SPELL_INFLICT_MODERATE_WOUNDS:     return nCL+1;
    }

    return nCL;
}

int GetAllowCasting( object oCritter, object oTarget ){

    int nMaxCL      = GetLocalInt( oCritter, L_MAXCL );

    if ( nMaxCL == -1 ){

        return 0;
    }

    int nArchetype  = GetLocalInt( oCritter, L_ARCHETYPE );
    object oDamager = GetLocalObject( oCritter, L_LASTDAMAGER );

    if ( oDamager == oTarget ){

        if ( GetDistanceBetween( oCritter, oTarget ) < 4.0 && ( d10() + 2 ) > nArchetype ){

            if ( nArchetype > 5 ){

                return -1;
            }

            return 0;
        }
    }

    return 1;
}



//-------------------------------------------------------------------------------
// utility functions
//-------------------------------------------------------------------------------
void DebugMessage( string sMessage, int nStatus ){

    int nFeedbackLevel = GetLocalInt( OBJECT_SELF, "ds_feedback" );
    //int nFeedbackLevel = 2;

    if ( nFeedbackLevel == nStatus ){

        SpeakString( sMessage );
    }
}

int GetArchetype( object oCritter ){

    int nLevelFactor = GetHitDice( oCritter );

    int nClassFactor = GetClassFactor( oCritter, 1 )
                     + GetClassFactor( oCritter, 2 )
                     + GetClassFactor( oCritter, 3 );

    int nArchetype   = nClassFactor/nLevelFactor;

    return nArchetype;
}

int GetClassFactor( object oCritter, int nPosition ){

    int nClass = GetClassByPosition( nPosition, oCritter );
    int nLevel = GetLevelByClass( nClass, oCritter );
    int nMod   = 0;

    switch ( nClass ) {

        case CLASS_TYPE_ABERRATION:     nMod = 5;  break;
        case CLASS_TYPE_BARD:           nMod = 6;  break;
        case CLASS_TYPE_BLACKGUARD:     nMod = 4;  break;
        case CLASS_TYPE_CLERIC:         nMod = 8;  break;
        case CLASS_TYPE_CONSTRUCT:      nMod = 3;  break;
        case CLASS_TYPE_DIVINECHAMPION: nMod = 2;  break;
        case CLASS_TYPE_DRAGON:         nMod = 6;  break;
        case CLASS_TYPE_DRAGONDISCIPLE: nMod = 3;  break;
        case CLASS_TYPE_DRUID:          nMod = 8;  break;
        case CLASS_TYPE_ELEMENTAL:      nMod = 3;  break;
        case CLASS_TYPE_HARPER:         nMod = 8;  break;
        case CLASS_TYPE_MAGICAL_BEAST:  nMod = 5;  break;
        case CLASS_TYPE_MONSTROUS:      nMod = 5;  break;
        case CLASS_TYPE_OUTSIDER:       nMod = 6;  break;
        case CLASS_TYPE_PALADIN:        nMod = 4;  break;
        case CLASS_TYPE_PALEMASTER:     nMod = 9;  break;
        case CLASS_TYPE_RANGER:         nMod = 4;  break;
        case CLASS_TYPE_SHADOWDANCER:   nMod = 3;  break;
        case CLASS_TYPE_SHIFTER:        nMod = 6;  break;
        case CLASS_TYPE_SORCERER:       nMod = 10; break;
        case CLASS_TYPE_WIZARD:         nMod = 10; break;

        default:    nMod = 1;    break;
    }

    return ( nLevel * nMod );
}

int SafeUseFeat( object oCritter, object oTarget, int nFeat ){

    if ( GetHasFeat( nFeat ) ){

        AssignCommand( oCritter, ActionUseFeat( nFeat, oTarget ) );
        return TRUE;
    }

    return FALSE;
}


