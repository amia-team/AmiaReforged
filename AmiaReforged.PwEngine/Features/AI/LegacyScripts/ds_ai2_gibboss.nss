//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai_damaged
//group:   ds_ai
//used as: OnDamage
//date:    dec 23 2007
//author:  disco

/* Changelog:
    09/11/13    Glim - Added functionality for %health based spell casting.
    02/21/14    Glim - Added functionality for %health based ability usage.

   Special Script specially for the Gibberling Bosses
*/

//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "ds_ai2_include"


//-------------------------------------------------------------------------------
// main
//-------------------------------------------------------------------------------
void main(){

    object oCritter     = OBJECT_SELF;
    object oDamager     = GetLastDamager();
    object oTarget      = GetLocalObject( oCritter, L_CURRENTTARGET );
    string sArchetype   = GetLocalString( oCritter, L_ARCHETYPE );
    int nReaction       = GetReaction( oCritter, oDamager );
    location lLoc1       = GetBehindLocation(oCritter);
    location lLoc2       = GetStepLeftLocation(oCritter);
    location lLoc3       = GetStepRightLocation(oCritter);


    if ( nReaction == 2 ){

        if ( GetDistanceBetween( oCritter, oDamager ) < 5.0 ){

            ClearAllActions();
            ActionMoveAwayFromObject( oDamager, TRUE, 10.0 );
            SetLocalObject( oCritter, L_CURRENTTARGET, oDamager );
        }
    }
    else if ( nReaction == 1 ){

        if ( oTarget != oDamager ){

            if ( GetObjectSeen( oDamager, oCritter ) && ( d100() - 20 ) < 25 ){

                SetLocalObject( oCritter, L_CURRENTTARGET, oDamager );
            }
        }
    }

    //Check for %health based triggers
    int nHP = GetPercentageHPLoss( oCritter );

    string sResRef = GetResRef(oCritter);

    if(sResRef=="gib_boss_1") // Enormous Gibbering Mouther
    {
      if((nHP<=90) && (GetLocalInt(oCritter,"90%")!=1))
      {
        AssignCommand(oCritter, SpeakString( "<c¥  >**Wet, moist, gurgling noises erupt from the massive mouther and smaller mouther's begin to split off as you strike it**</c>" ) );
        CreateObject(OBJECT_TYPE_CREATURE,"gibberingmouthc",lLoc2);
        CreateObject(OBJECT_TYPE_CREATURE,"gibberingmouthc",lLoc3);
        SetLocalInt(oCritter,"90%",1);
      }
      else if((nHP<=75) && (GetLocalInt(oCritter,"75%")!=1))
      {
        AssignCommand(oCritter, SpeakString( "<c¥  >**Wet, moist, gurgling noises erupt again as more smaller mouther's begin to split off!**</c>" ) );
        CreateObject(OBJECT_TYPE_CREATURE,"gibberingmouthc",lLoc2);
        CreateObject(OBJECT_TYPE_CREATURE,"gibberingmouthc",lLoc3);
        SetLocalInt(oCritter,"75%",1);
      }
      else if((nHP<=55) && (GetLocalInt(oCritter,"55%")!=1))
      {
        AssignCommand(oCritter, SpeakString( "<c¥  >**Wet, moist, gurgling noises erupt again as more smaller mouther's begin to split off!**</c>" ) );
        CreateObject(OBJECT_TYPE_CREATURE,"gibberingmouthc",lLoc2);
        CreateObject(OBJECT_TYPE_CREATURE,"gibberingmouthc",lLoc3);
        SetLocalInt(oCritter,"55%",1);
      }
      else if((nHP<=45) && (GetLocalInt(oCritter,"45%")!=1))
      {
        AssignCommand(oCritter, SpeakString( "<c¥  >**Wet, moist, gurgling noises erupt again as more smaller mouther's begin to split off!**</c>" ) );
        CreateObject(OBJECT_TYPE_CREATURE,"gibberingmouthc",lLoc2);
        CreateObject(OBJECT_TYPE_CREATURE,"gibberingmouthc",lLoc3);
        SetLocalInt(oCritter,"45%",1);
      }
      else if((nHP<=30) && (GetLocalInt(oCritter,"30%")!=1))
      {
        AssignCommand(oCritter, SpeakString( "<c¥  >**Wet, moist, gurgling noises erupt again as more smaller mouther's begin to split off!**</c>" ) );
        CreateObject(OBJECT_TYPE_CREATURE,"gibberingmouthc",lLoc2);
        CreateObject(OBJECT_TYPE_CREATURE,"gibberingmouthc",lLoc3);
        SetLocalInt(oCritter,"30%",1);
      }
      else if((nHP<=15) && (GetLocalInt(oCritter,"15%")!=1))
      {
        AssignCommand(oCritter, SpeakString( "<c¥  >**Wet, moist, gurgling noises erupt again as more smaller mouther's begin to split off!**</c>" ) );
        CreateObject(OBJECT_TYPE_CREATURE,"gibberingmouthc",lLoc2);
        CreateObject(OBJECT_TYPE_CREATURE,"gibberingmouthc",lLoc3);
        SetLocalInt(oCritter,"15%",1);
      }
    }
    else if(sResRef=="gib_boss_2")
    {
      if((nHP<=95) && (GetLocalInt(oCritter,"95%")!=1))
      {
        AssignCommand(oCritter, SpeakString( "<c¥  >**Gibberlings clinging to the larger Gibberling's hide leap off!**</c>" ) );
        CreateObject(OBJECT_TYPE_CREATURE,"gibberling_norml",lLoc1);
        SetLocalInt(oCritter,"95%",1);
      }
      else if((nHP<=50) && (GetLocalInt(oCritter,"50%")!=1))
      {
        AssignCommand(oCritter, SpeakString( "<c¥  >**More Gibberlings clinging to the larger Gibberling's hide leap off!**</c>" ) );
        CreateObject(OBJECT_TYPE_CREATURE,"gibberling_norml",lLoc2);
        CreateObject(OBJECT_TYPE_CREATURE,"gibberling_norml",lLoc3);
        SetLocalInt(oCritter,"50%",1);
      }
    }
    else if(sResRef=="gib_boss_3")
    {
      if((nHP<=95) && (GetLocalInt(oCritter,"80%")!=1))
      {
        AssignCommand(oCritter, SpeakString( "<c¥  >**Hisses, flails and then suddenly shimmers away!**</c>" ) );
        ApplyEffectAtLocation(DURATION_TYPE_INSTANT,EffectVisualEffect(VFX_FNF_SOUND_BURST),GetLocation(oCritter));
        CreateObject(OBJECT_TYPE_CREATURE,"gibberingmouther",lLoc1);
        AssignCommand( oCritter, ClearAllActions() );
        AssignCommand( oCritter, JumpToObject(GetWaypointByTag("gib_tele_"+IntToString(Random(4)+1))));
        AssignCommand( oCritter, ActionAttack(oDamager));
        SetLocalInt(oCritter,"95%",1);
      }
      else if((nHP<=75) && (GetLocalInt(oCritter,"60%")!=1))
      {
        AssignCommand(oCritter, SpeakString( "<c¥  >**Staggers and then suddenly howls!**</c>" ) );
        ApplyEffectAtLocation(DURATION_TYPE_INSTANT,EffectVisualEffect(VFX_FNF_HOWL_MIND),GetLocation(oCritter));
        CreateObject(OBJECT_TYPE_CREATURE,"gibberling_brood",lLoc2);
        CreateObject(OBJECT_TYPE_CREATURE,"gibberling_brood",lLoc3);
        SetLocalInt(oCritter,"75%",1);
      }
      else if((nHP<=50) && (GetLocalInt(oCritter,"30%")!=1))
      {
        AssignCommand(oCritter, SpeakString( "<c¥  >**Hisses, flails and then suddenly shimmers away again!**</c>" ) );
        ApplyEffectAtLocation(DURATION_TYPE_INSTANT,EffectVisualEffect(VFX_FNF_SOUND_BURST),GetLocation(oCritter));
        CreateObject(OBJECT_TYPE_CREATURE,"gibberingmouther",lLoc1);
        CreateObject(OBJECT_TYPE_CREATURE,"gibberling_brood",lLoc2);
        CreateObject(OBJECT_TYPE_CREATURE,"gibberling_brood",lLoc3);
        AssignCommand( oCritter, ClearAllActions() );
        AssignCommand( oCritter, JumpToObject(GetWaypointByTag("gib_tele_"+IntToString(Random(4)+1))));
        AssignCommand( oCritter, ActionAttack(oDamager));
        SetLocalInt(oCritter,"50%",1);
      }
    }


}
