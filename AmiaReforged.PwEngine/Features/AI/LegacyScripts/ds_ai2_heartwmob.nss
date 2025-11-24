//-----------------------------------------------------------------------------
// header
//-----------------------------------------------------------------------------
//script:  ds_ai2_heartwmob
//author:  Mav


//-------------------------------------------------------------------------------
// includes
//-------------------------------------------------------------------------------
#include "ds_ai2_include"

// Hunts the enemy if inactive too long
void HuntEnemy(object oCritter);

void main(){

    object oCritter     = OBJECT_SELF;
    object oRaidSummoner = GetObjectByTag("wormraid");
    int nBossOut = GetLocalInt(oRaidSummoner,"bossOut");
    int nCount          = GetLocalInt( oCritter, L_INACTIVE );
    int nSunkill        = GetLocalInt( oCritter, F_SUNKILL );

    SetLocalInt( oCritter, L_INACTIVE, (nCount + 1) );

    if (nCount == 1)
    {
      if(nBossOut==0)
      {
        DestroyObject(oCritter);
      }
      HuntEnemy(oCritter);
    }
}

void HuntEnemy(object oCritter)
{

  object oArea = GetArea(oCritter);
  object oObject = GetFirstObjectInArea(oArea,OBJECT_TYPE_CREATURE);
  int nBreak;
  while(GetIsObjectValid(oObject))
  {
    if((GetIsEnemy(oObject,oCritter)==TRUE) && GetIsInsideTrigger(oObject,"purplewormzone"))
    {
     AssignCommand(oCritter, ActionAttack(oObject));
     nBreak=1;
    }
    oObject = GetNextObjectInArea(oArea, OBJECT_TYPE_CREATURE);
    if(nBreak==1)
    {
     break;
    }
  }

  if(nBreak==0)
  {
   AssignCommand(oCritter, ActionMoveToObject(GetWaypointByTag("purplewormstart"),TRUE));
  }



}
