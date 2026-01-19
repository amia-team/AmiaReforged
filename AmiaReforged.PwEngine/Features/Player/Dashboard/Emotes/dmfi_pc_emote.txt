//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  dmfi_pc_emote
//group:   dmfi replacement
//used as: action script
//date:    2008-10-04
//author:  disco/dmfi team
#include "NW_I0_GENERIC"
#include "x0_i0_position"

//-------------------------------------------------------------------------------
// prototypes
//-------------------------------------------------------------------------------
void DoEmoteFunction( object oUser, object oTarget, object oAssociate, int nNode, location lLocation );

//Smoking Function by Jason Robinson
void SmokePipe(object oActivator);

//Smoking Function by Jason Robinson
location GetLocationAboveAndInFrontOf(object oPC, float fDist, float fHeight);

void SitInNearestChair(object oPC);

void EmoteDance(object oPC);


//-------------------------------------------------------------------------------
// main
//-------------------------------------------------------------------------------
void main(){

    object oUser       = OBJECT_SELF;
    object oTarget     = GetLocalObject( oUser, "ds_target" );
    int nNode          = GetLocalInt( oUser, "ds_node" );
    // check to make sure if the target is a PC and not within the node range, that the target is reset to herself.
    if(nNode < 37 && (!(GetIsDM(oTarget) || GetIsDMPossessed(oTarget)) && GetMaster(oTarget) != oUser))
    {
        oTarget = oUser;
    }
    location lLocation = GetLocalLocation( oUser, "ds_location" );
    object oAssociate  = GetLocalObject( oUser, "jj_associate" );
    DoEmoteFunction( oUser, oTarget, oAssociate, nNode, lLocation );
}

//-------------------------------------------------------------------------------
// functions
//-------------------------------------------------------------------------------

void EmoteDance(object oPC)
{
object oRightHand = GetItemInSlot(INVENTORY_SLOT_RIGHTHAND,oPC);
object oLeftHand =  GetItemInSlot(INVENTORY_SLOT_LEFTHAND,oPC);

AssignCommand(oPC,ActionUnequipItem(oRightHand));
AssignCommand(oPC,ActionUnequipItem(oLeftHand));

AssignCommand(oPC,ActionPlayAnimation( ANIMATION_FIREFORGET_VICTORY2,1.0));
AssignCommand(oPC,ActionDoCommand(PlayVoiceChat(VOICE_CHAT_LAUGH,oPC)));
AssignCommand(oPC,ActionPlayAnimation( ANIMATION_LOOPING_TALK_LAUGHING, 2.0, 2.0));
AssignCommand(oPC,ActionPlayAnimation( ANIMATION_FIREFORGET_VICTORY1,1.0));
AssignCommand(oPC,ActionPlayAnimation( ANIMATION_FIREFORGET_VICTORY3,2.0));
AssignCommand(oPC,ActionPlayAnimation( ANIMATION_LOOPING_GET_MID, 3.0, 1.0));
AssignCommand(oPC,ActionPlayAnimation( ANIMATION_LOOPING_TALK_FORCEFUL,1.0));
AssignCommand(oPC,ActionPlayAnimation( ANIMATION_FIREFORGET_VICTORY2,1.0));
AssignCommand(oPC,ActionDoCommand(PlayVoiceChat(VOICE_CHAT_LAUGH,oPC)));
AssignCommand(oPC,ActionPlayAnimation( ANIMATION_LOOPING_TALK_LAUGHING, 2.0, 2.0));
AssignCommand(oPC,ActionPlayAnimation( ANIMATION_FIREFORGET_VICTORY1,1.0));
AssignCommand(oPC,ActionPlayAnimation( ANIMATION_FIREFORGET_VICTORY3,2.0));
AssignCommand(oPC,ActionDoCommand(PlayVoiceChat(VOICE_CHAT_LAUGH,oPC)));
AssignCommand(oPC,ActionPlayAnimation( ANIMATION_LOOPING_GET_MID, 3.0, 1.0));
AssignCommand(oPC,ActionPlayAnimation( ANIMATION_FIREFORGET_VICTORY2,1.0));

AssignCommand(oPC,ActionDoCommand(ActionEquipItem(oLeftHand,INVENTORY_SLOT_LEFTHAND)));
AssignCommand(oPC,ActionDoCommand(ActionEquipItem(oRightHand,INVENTORY_SLOT_RIGHTHAND)));
}

void SitInNearestChair(object oPC)
{
object oSit,oRightHand,oLeftHand,oChair,oCouch,oBenchPew,oStool;
float fDistSit;int nth;
// get the closest chair, couch bench or stool
   nth = 1;oChair = GetNearestObjectByTag("Chair", oPC,nth);
   while(oChair != OBJECT_INVALID &&  GetSittingCreature(oChair) != OBJECT_INVALID)
   {nth++;oChair = GetNearestObjectByTag("Chair", oPC,nth);}

   nth = 1;oCouch = GetNearestObjectByTag("Couch", oPC,nth);
   while(oCouch != OBJECT_INVALID && GetSittingCreature(oCouch) != OBJECT_INVALID)
      {nth++;oChair = GetNearestObjectByTag("Couch", oPC,nth);}

   nth = 1;oBenchPew = GetNearestObjectByTag("BenchPew", oPC,nth);
   while(oBenchPew != OBJECT_INVALID && GetSittingCreature(oBenchPew) != OBJECT_INVALID)
      {nth++;oChair = GetNearestObjectByTag("BenchPew", oPC,nth);}
/* 1.27 bug
   nth = 1;oStool = GetNearestObjectByTag("Stool", oPC,nth);
   while(oStool != OBJECT_INVALID && GetSittingCreature(oStool) != OBJECT_INVALID)
      {nth++;oStool = GetNearestObjectByTag("Stool", oPC,nth);}
*/
// get the distance between the user and each object (-1.0 is the result if no
// object is found
float fDistanceChair = GetDistanceToObject(oChair);
float fDistanceBench = GetDistanceToObject(oBenchPew);
float fDistanceCouch = GetDistanceToObject(oCouch);
float fDistanceStool = GetDistanceToObject(oStool);

// if any of the objects are invalid (not there), change the return value
// to a high number so the distance math can work
if (fDistanceChair == -1.0)
{fDistanceChair =1000.0;}

if (fDistanceBench == -1.0)
{fDistanceBench = 1000.0;}

if (fDistanceCouch == -1.0)
{fDistanceCouch = 1000.0;}

if (fDistanceStool == -1.0)
{fDistanceStool = 1000.0;}

// find out which object is closest to the PC
if (fDistanceChair<fDistanceBench && fDistanceChair<fDistanceCouch && fDistanceChair<fDistanceStool)
{oSit=oChair;fDistSit=fDistanceChair;}
else
if (fDistanceBench<fDistanceChair && fDistanceBench<fDistanceCouch && fDistanceBench<fDistanceStool)
{oSit=oBenchPew;fDistSit=fDistanceBench;}
else
if (fDistanceCouch<fDistanceChair && fDistanceCouch<fDistanceBench && fDistanceCouch<fDistanceStool)
{oSit=oCouch;fDistSit=fDistanceCouch;}
else
//if (fDistanceStool<fDistanceChair && fDistanceStool<fDistanceBench && fDistanceStool<fDistanceCouch)
{oSit=oStool;fDistSit=fDistanceStool;}

 if(oSit !=  OBJECT_INVALID && fDistSit < 12.0)
    {
     // if no one is sitting in the object the PC is closest to, have him sit in it
     if (GetSittingCreature(oSit) == OBJECT_INVALID)
         {
           oRightHand = GetItemInSlot(INVENTORY_SLOT_RIGHTHAND,oPC);
           oLeftHand =  GetItemInSlot(INVENTORY_SLOT_LEFTHAND,oPC);
           AssignCommand(oPC,ActionMoveToObject(oSit,FALSE,2.0)); //:: Presumably this will be fixed in a patch so that Plares will not run to chair
           ActionUnequipItem(oRightHand); //:: Added to resolve clipping issues when seated
           ActionUnequipItem(oLeftHand);  //:: Added to resolve clipping issues when seated
           ActionDoCommand(AssignCommand(oPC,ActionSit(oSit)));

        }
      else
        {SendMessageToPC(oPC,"The nearest chair is already taken ");}
    }
  else
    {SendMessageToPC(oPC,"There are no chairs nearby");}
}

//Smoking Function by Jason Robinson
location GetLocationAboveAndInFrontOf(object oPC, float fDist, float fHeight)
{
    float fDistance = -fDist;
    object oTarget = (oPC);
    object oArea = GetArea(oTarget);
    vector vPosition = GetPosition(oTarget);
    vPosition.z += fHeight;
    float fOrientation = GetFacing(oTarget);
    vector vNewPos = AngleToVector(fOrientation);
    float vZ = vPosition.z;
    float vX = vPosition.x - fDistance * vNewPos.x;
    float vY = vPosition.y - fDistance * vNewPos.y;
    fOrientation = GetFacing(oTarget);
    vX = vPosition.x - fDistance * vNewPos.x;
    vY = vPosition.y - fDistance * vNewPos.y;
    vNewPos = AngleToVector(fOrientation);
    vZ = vPosition.z;
    vNewPos = Vector(vX, vY, vZ);
    return Location(oArea, vNewPos, fOrientation);
}

//Smoking Function by Jason Robinson
void SmokePipe(object oActivator)
{
    float fHeight = 1.7;
    float fDistance = 0.1;
    // Set height based on race and gender
    if (GetGender(oActivator) == GENDER_MALE)
    {
        switch (GetRacialType(oActivator))
        {
            case RACIAL_TYPE_HUMAN:
            case RACIAL_TYPE_HALFELF: fHeight = 1.7; fDistance = 0.12; break;
            case RACIAL_TYPE_ELF: fHeight = 1.55; fDistance = 0.08; break;
            case RACIAL_TYPE_GNOME:
            case RACIAL_TYPE_HALFLING: fHeight = 1.15; fDistance = 0.12; break;
            case RACIAL_TYPE_DWARF: fHeight = 1.2; fDistance = 0.12; break;
            case RACIAL_TYPE_HALFORC: fHeight = 1.9; fDistance = 0.2; break;
        }
    }
    else
    {
        // FEMALES
        switch (GetRacialType(oActivator))
        {
            case RACIAL_TYPE_HUMAN:
            case RACIAL_TYPE_HALFELF: fHeight = 1.6; fDistance = 0.12; break;
            case RACIAL_TYPE_ELF: fHeight = 1.45; fDistance = 0.12; break;
            case RACIAL_TYPE_GNOME:
            case RACIAL_TYPE_HALFLING: fHeight = 1.1; fDistance = 0.075; break;
            case RACIAL_TYPE_DWARF: fHeight = 1.2; fDistance = 0.1; break;
            case RACIAL_TYPE_HALFORC: fHeight = 1.8; fDistance = 0.13; break;
        }
    }
    location lAboveHead = GetLocationAboveAndInFrontOf(oActivator, fDistance, fHeight);
    // glow red
    AssignCommand(oActivator, ActionDoCommand(ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_LIGHT_RED_5), oActivator, 0.15)));
    // wait a moment
    AssignCommand(oActivator, ActionWait(3.0));
    // puff of smoke above and in front of head
    AssignCommand(oActivator, ActionDoCommand(ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_FNF_SMOKE_PUFF), lAboveHead)));
    // if female, turn head to left
    if ((GetGender(oActivator) == GENDER_FEMALE) && (GetRacialType(oActivator) != RACIAL_TYPE_DWARF))
        AssignCommand(oActivator, ActionPlayAnimation(ANIMATION_FIREFORGET_HEAD_TURN_LEFT, 1.0, 5.0));
}

//-----------------------------------------------------------------------
void sa2_control(object oHench, float fDuration=20.0, float wait=2.0)
{
  if (GetMaster(oHench) != OBJECT_INVALID)
  {
    SetAssociateState(NW_ASC_MODE_STAND_GROUND,TRUE,oHench);
    //DelayCommand(fDuration,ExecuteScript("sa2_follow_me",oHench));
  }
}

void sa2_kiss(object oKisser, object oKissee, float fduration=8.0, int kissAnim=TRUE, int hug=FALSE)
{
    float eGhostDur = 8.0;
    effect eGhost = EffectCutsceneGhost();
    ApplyEffectToObject(DURATION_TYPE_TEMPORARY,eGhost,oKisser,eGhostDur);
    ApplyEffectToObject(DURATION_TYPE_TEMPORARY,eGhost,oKissee,eGhostDur);

    float wait = 4.0;
    float fKisserDistance = 0.25;
    float fKisseeFacing = GetFacing(oKissee);
    float fKisserFacing = GetOppositeDirection(fKisseeFacing);
    location lSave   = GetLocation(oKisser);

    if (kissAnim)
    {
      fKisserDistance = 1.0;
      if (GetGender(oKisser) == GetGender(oKissee) &&
          GetGender(oKisser) == GENDER_MALE) fKisserDistance += 0.34;
      else if (GetGender(oKisser) == GetGender(oKissee) &&
          GetGender(oKisser) == GENDER_FEMALE) fKisserDistance -= 0.3;
    }
    else
    {
      sa2_control(oKisser,fduration);
      sa2_control(oKissee,fduration);
    }

    location lKisser = GenerateNewLocationFromLocation(GetLocation(oKissee),
      fKisserDistance,fKisseeFacing,fKisserFacing);
    MoveToNewLocation(lKisser,oKisser);
    DelayCommand(fduration,MoveToNewLocation(lSave,oKisser));

    if (kissAnim)
    {
      int animation = ANIMATION_LOOPING_CUSTOM11;      // Set to kiss animation
      if (hug) animation = ANIMATION_LOOPING_CUSTOM14; // Set to hug animation
      DelayCommand(wait-1.0,AssignCommand(oKisser,
        ActionPlayAnimation(animation, 1.0f, fduration)));
      DelayCommand(wait-1.0,AssignCommand(oKissee,
        ActionPlayAnimation(animation, 1.0f, fduration)));
    }
}

void sa2_PCsleep_with(object oUser, object oTarget, location loc, float fduration=10.0)
{
  float fKisserDistance = 0.0;
  object oKisser;
  object oKissee;
  int kisserAnim = ANIMATION_LOOPING_CUSTOM12;
  int kisseeAnim = ANIMATION_LOOPING_CUSTOM12;

  if (GetGender(oTarget) == GENDER_MALE)
  {
    oKissee = oUser;
    oKisser = oTarget;
  }
  else
  {
    oKisser = oUser;
    oKissee = oTarget;
  }

  if (GetGender(oKisser) == GENDER_FEMALE)
  {
    kisserAnim = ANIMATION_LOOPING_CUSTOM13;
  }
  if (GetGender(oKissee) == GENDER_MALE)
  {
    kisseeAnim = ANIMATION_LOOPING_CUSTOM13;
  }
  if ((GetRacialType(oKisser) == RACIAL_TYPE_HUMAN &&
       GetRacialType(oKissee) == RACIAL_TYPE_HALFELF) ||
      (GetRacialType(oKisser) == RACIAL_TYPE_HALFELF &&
       GetRacialType(oKissee) == RACIAL_TYPE_HUMAN) ||
      (GetRacialType(oKisser) == GetRacialType(oKissee)))
  {
    fKisserDistance = 0.25;
  }
  else if (GetRacialType(oKissee) == RACIAL_TYPE_ELF) fKisserDistance = 0.4;
  else if (GetRacialType(oKisser) == RACIAL_TYPE_ELF) fKisserDistance = 0.15;
  else fKisserDistance = 0.25;

  if (GetGender(oKisser) == GetGender(oKissee))
  {
    if (GetGender(oKisser) == GENDER_FEMALE) fKisserDistance += 0.1;
    else if (GetGender(oKisser) == GENDER_MALE) fKisserDistance -= 0.1;
  }

  float eGhostDur = 8.0;
  effect eGhost = EffectCutsceneGhost();
  ApplyEffectToObject(DURATION_TYPE_TEMPORARY,eGhost,oKisser,eGhostDur);
  ApplyEffectToObject(DURATION_TYPE_TEMPORARY,eGhost,oKissee,eGhostDur);

  float wait = 3.0;
  float fKisserFacing = GetFacingFromLocation(loc);
  float fKisseeFacing = GetOppositeDirection(fKisserFacing);

  location lKissee = GenerateNewLocationFromLocation(loc,fKisserDistance,
                        fKisserFacing,fKisseeFacing);
  MoveToNewLocation(lKissee,oKissee);
  MoveToNewLocation(loc,oKisser);
  DelayCommand(wait-1.0,AssignCommand(oKissee,
    ActionPlayAnimation(kisseeAnim, 1.0f, fduration)));
  DelayCommand(wait-1.0,AssignCommand(oKisser,
    ActionPlayAnimation(kisserAnim, 1.0f, fduration)));
}
//waltz script
void startWaltz(object oUser, object oTarget)
{
  float fduration = 5.0;

    effect eGhost = EffectCutsceneGhost();
    ApplyEffectToObject(DURATION_TYPE_TEMPORARY,eGhost,oUser,fduration);
    ApplyEffectToObject(DURATION_TYPE_TEMPORARY,eGhost,oTarget,fduration);

    float fTargetface = GetFacing(oTarget);
    float fPCFace = GetOppositeDirection(fTargetface);
    float wait = 3.0;

    object oArea = GetArea(oUser);
    float wDist = 0.3;
    location targetLoc = GetLocation(oTarget);
    location wp = GenerateNewLocationFromLocation(targetLoc, wDist,  fTargetface, fPCFace);
    MoveToNewLocation ( wp, oUser);

  DelayCommand(wait, AssignCommand(oUser,
    ActionPlayAnimation(ANIMATION_LOOPING_CUSTOM20, 1.0f, 9999.0f)));
  DelayCommand(wait, AssignCommand(oTarget,
    ActionPlayAnimation(ANIMATION_LOOPING_CUSTOM20, 1.0f, 9999.0f)));
  //camera functions
  DelayCommand(3.3, SetCommandable(FALSE, oUser));
  DelayCommand(5.0, SetCommandable(TRUE, oUser));
}

// lap sitting
void lapSit(object oUser, object oTarget, int iEmote)
{
    DelayCommand( 0.0, AssignCommand( oTarget, ActionPlayAnimation(ANIMATION_LOOPING_SIT_CROSS, 1.0, 9999.0)));
    float fduration = 15.0;

    effect eGhost = EffectCutsceneGhost();
    ApplyEffectToObject(DURATION_TYPE_TEMPORARY,eGhost,oUser,fduration);
    ApplyEffectToObject(DURATION_TYPE_TEMPORARY,eGhost,oTarget,fduration);

    float fTargetface = GetFacing(oTarget);
    float fPCFace;
    vector posDest;

    if(iEmote == 44)
    {
        fPCFace = GetOppositeDirection(fTargetface);
        fTargetface = GetNormalizedDirection(fTargetface + 20.0);
        fPCFace = GetNormalizedDirection(fPCFace - 10.0);
        posDest = GetPosition(oTarget) - AngleToVector(fTargetface)*0.25;
    }
    else if(iEmote == 45)
    {
        float fTargetface = GetFacing(oTarget);
        fPCFace = fTargetface;
        posDest = GetPosition(oTarget) + AngleToVector(fTargetface)*0.25;
    }
    object oArea = GetArea(oUser);

    location lDest = Location(oArea, posDest, fPCFace);
    MoveToNewLocation ( lDest, oUser);
    DelayCommand( 1.0, AssignCommand( oUser, ActionPlayAnimation(ANIMATION_LOOPING_SIT_CROSS, 1.0, 9999.0)));
}

void getCloseTo(object oUser, object oTarget, int iEmote)
{
    float fTargetface;
    float fPCFace;
    float fduration = 15.0;
    float vectorMod;
    effect eGhost = EffectCutsceneGhost();
    ApplyEffectToObject(DURATION_TYPE_TEMPORARY,eGhost,oUser,fduration);
    ApplyEffectToObject(DURATION_TYPE_TEMPORARY,eGhost,oTarget,fduration);

    if(iEmote == 41)
    {
        fTargetface = GetOppositeDirection (GetFacing(oTarget));
        fPCFace = GetFacing(oTarget);
        vectorMod = 0.25;
    }
    else if (iEmote = 42)
    {
        fTargetface = GetFacing(oTarget);
        fPCFace = fTargetface;
        vectorMod = 0.30;
    }

    object oArea = GetArea(oUser);
    vector posDest = GetPosition(oTarget) + AngleToVector(fTargetface)*vectorMod;
    location lDest = Location(oArea, posDest, fPCFace);
    MoveToNewLocation ( lDest, oUser);
}

void DestroyLap(object oLapTarget)
{
    if (!GetIsObjectValid(GetSittingCreature(oLapTarget)))
    {
        DestroyObject(oLapTarget);
    }
    else
    {
        DelayCommand(10.0, DestroyLap(oLapTarget));
    }
}

void chairLapSit(object oUser, object oTarget, int iEmote)
{
    location lTarget;
    object oPlaceable;
    object oSitter;
    object oSitTarget;
    location lSitTarget;
    float fSitTargetFacing;
    object oSitTargetArea;
    vector vSitTarget;
    location lLapTarget;
    vector vLapTarget;
    object oLapTarget;
    float fDistance;
    object oLapCheck;

    lTarget = GetLocation( oTarget);
    oPlaceable = GetNearestObjectToLocation( OBJECT_TYPE_PLACEABLE, lTarget);

    fSitTargetFacing = GetFacing( oTarget);
    oSitTargetArea = GetArea( oTarget);
    vSitTarget = GetPositionFromLocation( lTarget);

    if(iEmote == 46)
    {
        fDistance = 0.2;
        vLapTarget = GetChangedPosition( vSitTarget, fDistance, fSitTargetFacing);
        lLapTarget = Location( oSitTargetArea, vLapTarget, fSitTargetFacing);
    }
    else if(iEmote == 47)
    {
        fDistance = 0.25;
        vLapTarget = GetChangedPosition( vSitTarget, fDistance, fSitTargetFacing);
        fSitTargetFacing = GetOppositeDirection( fSitTargetFacing);
        lLapTarget = Location( oSitTargetArea, vLapTarget, fSitTargetFacing);
    }
    oLapTarget = CreateObject( OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLapTarget);
    AssignCommand( oUser, ActionSit(oLapTarget));
    DelayCommand(10.0, DestroyLap(oLapTarget));

}

void PerformMutualEmote(object oUser, object oTarget, int iEmote)
{
   switch(iEmote)
   {
        case 38: sa2_kiss(oTarget,oUser,9999.0,TRUE,FALSE); break;
        case 39: sa2_PCsleep_with(oUser, oTarget, GetLocation(oTarget), 9999.0); break;
        case 40: sa2_kiss(oTarget,oUser,9999.0,TRUE,TRUE); break;
        case 41: getCloseTo(oTarget, oUser, iEmote); break;
        case 42: getCloseTo(oTarget, oUser, iEmote); break;
        case 43: startWaltz(oTarget, oUser); break;
        case 44: lapSit(oTarget, oUser, iEmote); break;
        case 45: lapSit(oTarget, oUser, iEmote); break;
        case 46: AssignCommand(oUser, SitInNearestChair(oUser)); DelayCommand(3.0, chairLapSit(oTarget, oUser, iEmote)); break;
        case 47: AssignCommand(oUser, SitInNearestChair(oUser)); DelayCommand(3.0, chairLapSit(oTarget, oUser, iEmote)); break;
        default: break;
   }
}

//This function is for the DMFI Emote Wand
void DoEmoteFunction( object oUser, object oTarget, object oAssociate, int nNode, location lLocation ){

    if ( !GetIsObjectValid( oTarget ) ){

        oTarget = oUser;
    }
    float fDur = 9999.0f; //Duration

    switch ( nNode ){

        case 01: AssignCommand(oTarget, PlayAnimation( ANIMATION_FIREFORGET_DODGE_SIDE, 1.0)); break;
        case 02: AssignCommand(oTarget, PlayAnimation( ANIMATION_FIREFORGET_DRINK, 1.0)); break;
        case 03: AssignCommand(oTarget, PlayAnimation( ANIMATION_FIREFORGET_DODGE_DUCK, 1.0)); break;
        case 04: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_DEAD_BACK, 1.0, fDur)); break;
        case 05: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_DEAD_FRONT, 1.0, fDur)); break;
        case 06: AssignCommand(oTarget, PlayAnimation( ANIMATION_FIREFORGET_READ, 1.0)); DelayCommand(3.0f, AssignCommand(oTarget, PlayAnimation( ANIMATION_FIREFORGET_READ, 1.0)));break;
        case 07: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_SIT_CROSS, 1.0, fDur)); break;
        case 11: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_TALK_PLEADING, 1.0, fDur)); break;
        case 12: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_CONJURE1, 1.0, fDur)); break;
        case 13: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_CONJURE2, 1.0, fDur)); break;
        case 14: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_GET_LOW, 1.0, fDur)); break;
        case 15: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_GET_MID, 1.0, fDur)); break;
        case 16: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_MEDITATE, 1.0, fDur)); break;
        case 17: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_TALK_FORCEFUL, 1.0, fDur)); break;
        case 18: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_WORSHIP, 1.0, fDur)); break;
        case 21: EmoteDance(oTarget); break;
        case 22: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_PAUSE_DRUNK, 1.0, fDur)); break;
        case 23: SendMessageToPC(oTarget, "This option has been removed."); break;
        case 24: SitInNearestChair(oTarget); break;
        case 25: AssignCommand(oTarget, ActionPlayAnimation( ANIMATION_LOOPING_SIT_CROSS, 1.0, fDur)); DelayCommand(1.0f, AssignCommand(oTarget, PlayAnimation( ANIMATION_FIREFORGET_DRINK, 1.0))); DelayCommand(3.0f, AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_SIT_CROSS, 1.0, fDur)));break;
        case 26: AssignCommand(oTarget, ActionPlayAnimation( ANIMATION_LOOPING_SIT_CROSS, 1.0, fDur)); DelayCommand(1.0f, AssignCommand(oTarget, PlayAnimation( ANIMATION_FIREFORGET_READ, 1.0))); DelayCommand(3.0f, AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_SIT_CROSS, 1.0, fDur)));break;
        case 27: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_SPASM, 1.0, fDur)); break;
        case 28: SmokePipe(oTarget); break;
        case 29: SetLocalObject(oUser, "jj_associate", GetAssociate( ASSOCIATE_TYPE_SUMMONED, oUser )) ; break;
        case 30: SetLocalObject(oUser, "jj_associate", GetAssociate( ASSOCIATE_TYPE_ANIMALCOMPANION, oUser )) ; break;
        case 31: SetLocalObject(oUser, "jj_associate", GetAssociate( ASSOCIATE_TYPE_FAMILIAR, oUser )) ; break;
        case 32: SetLocalObject(oUser, "jj_associate", GetAssociate( ASSOCIATE_TYPE_DOMINATED, oUser )) ; break;
        case 33: SetLocalObject(oUser, "jj_associate", GetAssociate( ASSOCIATE_TYPE_HENCHMAN, oUser )) ; break;
        case 34: AssignCommand(oAssociate, ActionMoveToLocation(lLocation, FALSE)); break;
        case 35: AssignCommand(oAssociate, ActionMoveToLocation(lLocation, TRUE)); break;
        case 36: AssignCommand(oAssociate, SetFacingPoint(GetPositionFromLocation(lLocation))); break;
        case 37: PerformMutualEmote(oUser, oTarget, GetLocalInt(oUser, "dmfi_pc_consent_emote")); break;
        case 38: SetLocalInt(oTarget, "dmfi_pc_consent_emote", 38); SetLocalObject(oTarget, "ds_target", oUser); SetCustomToken(54557, GetName(oUser)); SetCustomToken(54558, "kiss you standing up"); SetLocalString(oTarget,"ds_action","dmfi_pc_emote"); AssignCommand( oTarget, ActionStartConversation( oTarget, "dmfi_pc_consent", TRUE, FALSE ) ); break;
        case 39: SetLocalInt(oTarget, "dmfi_pc_consent_emote", 39); SetLocalObject(oTarget, "ds_target", oUser); SetCustomToken(54557, GetName(oUser)); SetCustomToken(54558, "kiss you lying down"); SetLocalString(oTarget,"ds_action","dmfi_pc_emote"); AssignCommand( oTarget, ActionStartConversation( oTarget, "dmfi_pc_consent", TRUE, FALSE ) ); break;
        case 40: SetLocalInt(oTarget, "dmfi_pc_consent_emote", 40); SetLocalObject(oTarget, "ds_target", oUser); SetCustomToken(54557, GetName(oUser)); SetCustomToken(54558, "hug you"); SetLocalString(oTarget,"ds_action","dmfi_pc_emote"); AssignCommand( oTarget, ActionStartConversation( oTarget, "dmfi_pc_consent", TRUE, FALSE ) ); break;
        case 41: SetLocalInt(oTarget, "dmfi_pc_consent_emote", 41); SetLocalObject(oTarget, "ds_target", oUser); SetCustomToken(54557, GetName(oUser)); SetCustomToken(54558, "hug you from behind"); SetLocalString(oTarget,"ds_action","dmfi_pc_emote"); AssignCommand( oTarget, ActionStartConversation( oTarget, "dmfi_pc_consent", TRUE, FALSE ) ); break;
        case 42: SetLocalInt(oTarget, "dmfi_pc_consent_emote", 42); SetLocalObject(oTarget, "ds_target", oUser); SetCustomToken(54557, GetName(oUser)); SetCustomToken(54558, "back into you closely"); SetLocalString(oTarget,"ds_action","dmfi_pc_emote"); AssignCommand( oTarget, ActionStartConversation( oTarget, "dmfi_pc_consent", TRUE, FALSE ) ); break;
        case 43: SetLocalInt(oTarget, "dmfi_pc_consent_emote", 43); SetLocalObject(oTarget, "ds_target", oUser); SetCustomToken(54557, GetName(oUser)); SetCustomToken(54558, "waltz with you"); SetLocalString(oTarget,"ds_action","dmfi_pc_emote"); AssignCommand( oTarget, ActionStartConversation( oTarget, "dmfi_pc_consent", TRUE, FALSE ) ); break;
        case 44: SetLocalInt(oTarget, "dmfi_pc_consent_emote", 44); SetLocalObject(oTarget, "ds_target", oUser); SetCustomToken(54557, GetName(oUser)); SetCustomToken(54558, "sit on your lap on the ground facing you"); SetLocalString(oTarget,"ds_action","dmfi_pc_emote"); AssignCommand( oTarget, ActionStartConversation( oTarget, "dmfi_pc_consent", TRUE, FALSE ) ); break;
        case 45: SetLocalInt(oTarget, "dmfi_pc_consent_emote", 45); SetLocalObject(oTarget, "ds_target", oUser); SetCustomToken(54557, GetName(oUser)); SetCustomToken(54558, "sit on your lap on the ground facing away from you"); SetLocalString(oTarget,"ds_action","dmfi_pc_emote"); AssignCommand( oTarget, ActionStartConversation( oTarget, "dmfi_pc_consent", TRUE, FALSE ) ); break;
        case 46: SetLocalInt(oTarget, "dmfi_pc_consent_emote", 46); SetLocalObject(oTarget, "ds_target", oUser); SetCustomToken(54557, GetName(oUser)); SetCustomToken(54558, "sit on your lap while seated in a chair facing you"); SetLocalString(oTarget,"ds_action","dmfi_pc_emote"); AssignCommand( oTarget, ActionStartConversation( oTarget, "dmfi_pc_consent", TRUE, FALSE ) ); break;
        case 47: SetLocalInt(oTarget, "dmfi_pc_consent_emote", 47); SetLocalObject(oTarget, "ds_target", oUser); SetCustomToken(54557, GetName(oUser)); SetCustomToken(54558, "sit on your lap while seated in a chair facing away from you"); SetLocalString(oTarget,"ds_action","dmfi_pc_emote"); AssignCommand( oTarget, ActionStartConversation( oTarget, "dmfi_pc_consent", TRUE, FALSE ) ); break;
        case 48: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_CUSTOM19, 1.0, fDur)); break;
        default: break;
    }
}

