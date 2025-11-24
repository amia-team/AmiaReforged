// Necro Boss Mechanics
// 5/26/2020 - Maverick00053
#include "ds_ai2_include"
#include "X0_I0_SPELLS"
#include "x2_inc_spellhook"
#include "inc_ds_ondeath"

void OnHitTeleport(object oDamager);  // Teleport on hit
void Invulnerability(object oCaster, float fTime); // Invuln spell
void SummonArmy(object oCaster, int nArmyType); // Summon skeletal army
void DeathHealSpell(object oCaster); // Super sized wail that drains life that will heal caster to full
void EvocationFireSpell(object oCaster); // Super sized combust/flamestrike spell
void NegativeDeathSpell(object oCaster); // Negative energy damage / Death spell
void LootDrop(object oArea, object oWayPoint1, object oWaypoint2, object oDamager); // Loot Drop
int  SpellDiceRoll(int nSpell, int nHalf); // Damage calculation for spells.
void CreateLoot(string sResRef, object Chest);


void main()
{

    object oArea = GetArea(OBJECT_SELF);
    object oDamager = GetLastDamager();
    object oFirst;
    object oTarget      = GetLocalObject( OBJECT_SELF, L_CURRENTTARGET );
    object oWayPoint1 = GetWaypointByTag("necrochest");
    object oWayPoint2 = GetWaypointByTag("necroescape");
    int nReaction       = GetReaction( OBJECT_SELF, oDamager );
    int nTalked = GetLocalInt(OBJECT_SELF, "talkedrecently");
    int n90PercentHP = GetLocalInt(OBJECT_SELF,"90%AbilityFired");
    int n80PercentHP = GetLocalInt(OBJECT_SELF,"80%AbilityFired");
    int n70PercentHP = GetLocalInt(OBJECT_SELF,"70%AbilityFired");
    int n60PercentHP = GetLocalInt(OBJECT_SELF,"60%AbilityFired");
    int n50PercentHP = GetLocalInt(OBJECT_SELF,"50%AbilityFired");
    int n40PercentHP = GetLocalInt(OBJECT_SELF,"40%AbilityFired");
    int n30PercentHP = GetLocalInt(OBJECT_SELF,"30%AbilityFired");
    int n20PercentHP = GetLocalInt(OBJECT_SELF,"20%AbilityFired");
    int n10PercentHP = GetLocalInt(OBJECT_SELF,"10%AbilityFired");
    int n1PercentHP = GetLocalInt(OBJECT_SELF,"1%AbilityFired");
    int nRandom = Random(12);
    string sArchetype   = GetLocalString( OBJECT_SELF, L_ARCHETYPE );
    string sMob = GetResRef(OBJECT_SELF);
    string sMobTag = GetTag(OBJECT_SELF);

    location lCreature = GetLocation(OBJECT_SELF);
    location lAhead = GetAheadLocation(OBJECT_SELF);
    location lBehind = GetBehindLocation(OBJECT_SELF);
    location lLeft  = GetStepLeftLocation(OBJECT_SELF);
    location lRight = GetStepRightLocation(OBJECT_SELF);
    location lAheadL = GetAheadLeftLocation(OBJECT_SELF);
    location lAheadR = GetAheadRightLocation(OBJECT_SELF);

    effect eVisualAura = EffectVisualEffect(VFX_DUR_AURA_COLD);
    effect eVisualSummon = EffectVisualEffect(VFX_FNF_SUMMON_MONSTER_1);
    effect eFullHeal = EffectHeal(10000);


    // HP based reactions for the boss
    if((sMob == "necrobossmob5"))
    {
      if((GetPercentageHPLoss(OBJECT_SELF) <= 90) && (n90PercentHP == 0))
      {  // 90% HP and the boss invulns for a moment

          // Invuln, Summon Army
          Invulnerability(OBJECT_SELF, 10.0);
          SpeakString("How foolish are you? You are but an insect compared to me.");
          DelayCommand(5.0,SummonArmy(OBJECT_SELF,0));

          SetLocalInt(OBJECT_SELF,"90%AbilityFired",1);

      }
      else if((GetPercentageHPLoss(OBJECT_SELF) <= 80) && (n80PercentHP == 0))
      {  // 80% HP and the boss invulns for a moment


          // Invuln, Evocation Fire Spell
          Invulnerability(OBJECT_SELF, 10.0);
          SpeakString("Burn! *The lich conjures forth an epic evocation spell of fire*");
          DelayCommand(5.0,EvocationFireSpell(OBJECT_SELF));

          SetLocalInt(OBJECT_SELF,"80%AbilityFired",1);

      }
      else if((GetPercentageHPLoss(OBJECT_SELF) <= 70) && (n70PercentHP == 0))
      {   // 70% HP and the boss invulns for a moment

          // Invuln, Summon Army
          Invulnerability(OBJECT_SELF, 10.0);
          SpeakString("Let my pets tear you apart!");
          DelayCommand(5.0,SummonArmy(OBJECT_SELF,1));

          SetLocalInt(OBJECT_SELF,"70%AbilityFired",1);

      }
      else if((GetPercentageHPLoss(OBJECT_SELF) <= 60) && (n60PercentHP == 0))
      {   // 60% HP and the boss invulns for a moment


          // Invuln, Summon Army
          Invulnerability(OBJECT_SELF, 10.0);
          DelayCommand(5.0,SummonArmy(OBJECT_SELF,1));

          SetLocalInt(OBJECT_SELF,"60%AbilityFired",1);


      }
      else if((GetPercentageHPLoss(OBJECT_SELF) <= 50) && (n50PercentHP == 0))
      {   // 50% HP and the boss invulns for a moment


          // Lock outside doors so noone else can come in
          object oDoor1 = GetObjectByTag ("ost_msc_3");
          AssignCommand(oDoor1,ActionCloseDoor(oDoor1));
          SetLocked(oDoor1,TRUE);
          SetLockKeyRequired(oDoor1, TRUE);

          // Invuln, Cast Uber Death Spell, Heal
          Invulnerability(OBJECT_SELF, 10.0);
          SpeakString("I will tear your souls from your chests! *Summons forth an epic spell of death filling the arena");
          DelayCommand(5.0,DeathHealSpell(OBJECT_SELF));
          DelayCommand(6.0,ApplyEffectToObject(DURATION_TYPE_INSTANT, eFullHeal, OBJECT_SELF));

          SetLocalInt(OBJECT_SELF,"50%AbilityFired",1);

      }
      else if((GetPercentageHPLoss(OBJECT_SELF) <= 40) && (n40PercentHP == 0))
      {   // 40% HP and the boss invulns for a moment

          // Invuln, Summon Army w/ Dragons
          Invulnerability(OBJECT_SELF, 10.0);
          DelayCommand(5.0,SummonArmy(OBJECT_SELF,3));

          SetLocalInt(OBJECT_SELF,"40%AbilityFired",1);

      }
      else if((GetPercentageHPLoss(OBJECT_SELF) <= 30) && (n30PercentHP == 0))
      {   // 30% HP and the boss invulns for a moment

          // Invuln, Negative Death Spell
          Invulnerability(OBJECT_SELF, 10.0);
          SpeakString("Impressive, still alive? Hmm. Not for long. *Summons forth an epic negative energy spell");
          DelayCommand(5.0,NegativeDeathSpell(OBJECT_SELF));

          SetLocalInt(OBJECT_SELF,"30%AbilityFired",1);
      }
      else if((GetPercentageHPLoss(OBJECT_SELF) <= 20) && (n20PercentHP == 0))
      {   // 20% HP and the boss invulns for a moment

          // Invuln, Summon Army w/ TOUGH mele
          Invulnerability(OBJECT_SELF, 10.0);
          DelayCommand(5.0,SummonArmy(OBJECT_SELF,2));

          SetLocalInt(OBJECT_SELF,"20%AbilityFired",1);

      }
      else if((GetPercentageHPLoss(OBJECT_SELF) <= 10) && (n10PercentHP == 0))  // 10 percent HP and last phase the boss runs
      {

          // Invuln, Evocation Fire Spell
          Invulnerability(OBJECT_SELF, 10.0);
          SpeakString("This is the end. *Once more summons forth a massive epic evocation spell of fire*");
          DelayCommand(5.0,EvocationFireSpell(OBJECT_SELF));

          SetLocalInt(OBJECT_SELF,"10%AbilityFired",1);
      }
      else if((GetPercentageHPLoss(OBJECT_SELF) <= 1) && (n1PercentHP == 0))  // 10 percent HP and last phase the boss runs
      {
          // Loot Drop and DEATH
          SpeakString("You think this is the end!? I will be reborn again and again... *The Lich's current body is destroyed*");
          ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_FNF_GAS_EXPLOSION_MIND), GetLocation(OBJECT_SELF));
          LootDrop(oArea,oWayPoint1,oWayPoint2,oDamager);
          DelayCommand(1.0,DestroyObject(OBJECT_SELF));
          SetLocalInt(OBJECT_SELF,"1%AbilityFired",1);
      }
      else
      {



          if( nRandom == 0 ) // 5 Percent chance on hit to summon a wight behind the caster
          {
            OnHitTeleport(oDamager);
          }




          // ds_ai2_damaged script

          if ( nReaction == 2 ){

           if ( GetDistanceBetween( OBJECT_SELF, oDamager ) < 5.0 ){

            ClearAllActions();
            ActionMoveAwayFromObject( oDamager, TRUE, 10.0 );
            SetLocalObject( OBJECT_SELF, L_CURRENTTARGET, oDamager );
           }
          }
          else if ( nReaction == 1 )
          {

           if ( oTarget != oDamager )
           {

             if ( GetObjectSeen( oDamager, OBJECT_SELF ) && ( d100() - 20 ) < 25 )
             {

                SetLocalObject( OBJECT_SELF, L_CURRENTTARGET, oDamager );
             }
            }
           }

            }

      }






}

void OnHitTeleport(object oDamager) // On Hit Teleport
{
   int nRandom = Random(4)+1;
   object oRandomWP = GetWaypointByTag("randomtele"+IntToString(nRandom));
   effect eVFX = EffectVisualEffect(VFX_FNF_SUMMON_MONSTER_1);

   ApplyEffectToObject(DURATION_TYPE_TEMPORARY,eVFX,oDamager);

   AssignCommand( oDamager, ClearAllActions() );
   AssignCommand( oDamager, JumpToObject(oRandomWP));
   DelayCommand(0.5, SummonArmy(oDamager,4));

}

void Invulnerability(object oCaster, float fTime) // Invuln + Teleport
{
   int nRandom = Random(4)+1;
   effect eVFX = EffectVisualEffect(VFX_DUR_PROT_EPIC_ARMOR_2);
   object oAltar = GetWaypointByTag("necroboss");
   object oTeleport = GetWaypointByTag("necromagic"+IntToString(nRandom));

   effect eVFX2 = EffectVisualEffect(VFX_FNF_SUMMON_MONSTER_1);
   ApplyEffectToObject(DURATION_TYPE_TEMPORARY,eVFX2,oCaster);

   AssignCommand( oCaster, ClearAllActions() );
   AssignCommand( oCaster, JumpToObject(oTeleport));

   SetPlotFlag(oCaster, 1);
   AssignCommand(oCaster,ActionWait(fTime));
   ApplyEffectToObject(DURATION_TYPE_TEMPORARY, eVFX, oCaster, fTime);
   DelayCommand(fTime,SetPlotFlag(oCaster,0));


}

void SummonArmy(object oCaster, int nArmyType) // Summons a pack of undead to help the necro
{
   location lAhead = GetAheadLocation(oCaster);
   location lAheadL = GetAheadLeftLocation(oCaster);
   location lAheadR = GetAheadRightLocation(oCaster);
   location lBehind = GetBehindLocation(oCaster);
   location lBehindL = GetFlankingLeftLocation(oCaster);
   location lBehindR = GetFlankingRightLocation(oCaster);
   location lLeft = GetStepLeftLocation(oCaster);
   location lRight = GetStepRightLocation(oCaster);
   object oNecroArch1 = GetWaypointByTag("necroarch1");
   object oNecroArch2 = GetWaypointByTag("necroarch2");
   object oNecroArch3 = GetWaypointByTag("necroarch3");
   object oNecroArch4 = GetWaypointByTag("necroarch4");
   object oNecroArchP1 = GetWaypointByTag("necroarchp1");
   object oNecroArchP2 = GetWaypointByTag("necroarchp2");
   object oNecroArchP3 = GetWaypointByTag("necroarchp3");
   object oNecroArchP4 = GetWaypointByTag("necroarchp4");
   object oNecroSummon1 = GetWaypointByTag("necrosummon1");
   object oNecroSummon2 = GetWaypointByTag("necrosummon2");
   object oNecroSummon3 = GetWaypointByTag("necrosummon3");
   object oNecroSummon4 = GetWaypointByTag("necrosummon4");
   effect eVFX = EffectVisualEffect(VFX_IMP_RAISE_DEAD);

// Necrobossmob1 = Wight, Necrobossmob2 = Bone Golem, Necrobossmob3 = Skeletal Archer, Necrobossmob4 = Animated Skeletal Dragon
   if(nArmyType == 0) // First Summon Event - Basic
   {
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArchP1), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArchP1));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArchP2), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArchP2));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArchP3), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArchP3));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArchP4), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArchP4));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch1), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch1));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob1", GetLocation(oNecroArch1), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch1));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch1), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch1));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch2), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch2));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob1", GetLocation(oNecroArch2), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch2));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch2), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch2));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch3), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch3));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob1", GetLocation(oNecroArch3), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch3));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch3), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch3));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch4), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch4));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob1", GetLocation(oNecroArch4), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch4));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch4), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch4));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob1", GetLocation(oNecroSummon1), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroSummon1));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob1", GetLocation(oNecroSummon2), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroSummon2));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob1", GetLocation(oNecroSummon3), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroSummon3));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob1", GetLocation(oNecroSummon4), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroSummon4));

   }
   else if(nArmyType == 1) // Second Summon Event - Bone Golem
   {
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArchP1), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArchP1));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArchP2), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArchP2));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArchP3), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArchP3));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArchP4), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArchP4));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch1), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch1));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch2), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch2));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch3), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch3));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch4), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch4));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob2", GetLocation(oNecroSummon1), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroSummon1));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob2", GetLocation(oNecroSummon2), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroSummon2));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob2", GetLocation(oNecroSummon3), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroSummon3));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob2", GetLocation(oNecroSummon4), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroSummon4));
   }
   else if(nArmyType == 2) //
   {
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob2", GetLocation(oNecroArchP1), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArchP1));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob2", GetLocation(oNecroArchP2), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArchP2));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob2", GetLocation(oNecroArchP3), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArchP3));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob2", GetLocation(oNecroArchP4), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArchP4));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch1), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch1));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch2), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch2));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch3), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch3));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch4), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch4));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob1", GetLocation(oNecroSummon1), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroSummon1));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob1", GetLocation(oNecroSummon2), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroSummon2));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob1", GetLocation(oNecroSummon3), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroSummon3));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob1", GetLocation(oNecroSummon4), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroSummon4));

   }
   else if(nArmyType == 3) //
   {
     // Animated Dragon going to fly in
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob4", GetLocation(oNecroSummon1), TRUE, "raiseme2");
     // Animated Dragon going to fly in
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob4", GetLocation(oNecroSummon2), TRUE, "raiseme2");


     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch1), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch1));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch1), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch1));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch2), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch2));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch2), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch2));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch3), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch3));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch3), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch3));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch4), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch4));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob3", GetLocation(oNecroArch4), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroArch4));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob1", GetLocation(oNecroSummon3), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroSummon3));
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob1", GetLocation(oNecroSummon4), FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, GetLocation(oNecroSummon4));

   }
   else if(nArmyType == 4) // One skeleton behind
   {
     CreateObject(OBJECT_TYPE_CREATURE, "Necrobossmob1", lBehind, FALSE, "raiseme");
     ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVFX, lBehind);
   }


}


void DeathHealSpell(object oCaster)  // Giant Wail spell
{

    //Declare major variables
    int nToAffect;
    object oTarget;
    float fTargetDistance;
    float fDelay;
    location lTarget;
    location nCasterLocation = GetLocation(oCaster);
    effect eVis = EffectVisualEffect(VFX_IMP_DEATH);
    effect eWail = EffectVisualEffect(VFX_FNF_WAIL_O_BANSHEES);
    effect eHorrid = EffectVisualEffect(VFX_FNF_HORRID_WILTING);
    effect eShake = EffectVisualEffect( VFX_FNF_SCREEN_SHAKE );
    int nCnt = 1;
    int nStop = 0;

    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eWail, nCasterLocation);
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eHorrid, nCasterLocation);

    object oMagicSpot1 = GetWaypointByTag("necromagic1");
    object oMagicSpot2 = GetWaypointByTag("necromagic2");
    object oMagicSpot3 = GetWaypointByTag("necromagic3");
    object oMagicSpot4 = GetWaypointByTag("necromagic4");
    object oMagicSpot5 = GetWaypointByTag("necromagic4");
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eWail, GetLocation(oMagicSpot1));
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eWail, GetLocation(oMagicSpot2));
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eWail, GetLocation(oMagicSpot3));
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eWail, GetLocation(oMagicSpot4));
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eWail, GetLocation(oMagicSpot5));

    oTarget = GetNearestObjectToLocation(OBJECT_TYPE_CREATURE, nCasterLocation, nCnt);

    while ((GetIsObjectValid(oTarget)) && (nStop == 0))
    {
        lTarget = GetLocation(oTarget);
        //Get the distance of the target from the center of the effect
        fDelay = GetRandomDelay(3.0, 4.0);//
        fTargetDistance = GetDistanceBetweenLocations(nCasterLocation, lTarget);
        //Check that the current target is valid and closer than 10.0m
        if(GetIsObjectValid(oTarget) && fTargetDistance <= 50.0)
        {
             // Make sure they dont have death immunity and they arent one of the necro's pet
             if(!GetIsImmune(oTarget, IMMUNITY_TYPE_DEATH) && (GetTag(oTarget) != "raiseme") && (GetTag(oTarget) != "raiseme2") && (GetTag(oTarget) != "necroboss"))
             {

                    //Make a fortitude save to avoid death
                    if(!MySavingThrow(SAVING_THROW_FORT, oTarget, 45, SAVING_THROW_TYPE_DEATH)) //, OBJECT_SELF, 3.0))
                    {
                        //Apply the delay VFX impact and death effect
                        DelayCommand(fDelay, ApplyEffectToObject(DURATION_TYPE_INSTANT, eVis, oTarget));
                        DelayCommand(fDelay, ApplyEffectToObject(DURATION_TYPE_INSTANT, eWail, oTarget));
                        effect eDeath = EffectDeath();
                        DelayCommand(fDelay, ApplyEffectToObject(DURATION_TYPE_INSTANT, eDeath, oTarget)); // no delay
                    }

              }


        }
        else
        {
            //Kick out of the loop
            nStop = 1;
        }
        //Increment the count of creatures targeted
        nCnt++;
        //Get the next closest target in the spell target location.
        oTarget = GetNearestObjectToLocation(OBJECT_TYPE_CREATURE, nCasterLocation, nCnt);
    }
}


void EvocationFireSpell(object oCaster)    // Super fire spell
{

//Declare major variables
    int nToAffect;
    object oTarget;
    float fTargetDistance;
    float fDelay;
    location lTarget;
    location nCasterLocation = GetLocation(oCaster);
    effect eVis = EffectVisualEffect(VFX_FNF_FIRESTORM);
    effect eFlamestrike =  EffectVisualEffect(VFX_IMP_DIVINE_STRIKE_FIRE);
    effect eShake = EffectVisualEffect( VFX_FNF_SCREEN_SHAKE );
    effect eDam;

    int nCnt = 1;
    int nStop = 0;

    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVis, nCasterLocation);


    object oMagicSpot1 = GetWaypointByTag("necromagic1");
    object oMagicSpot2 = GetWaypointByTag("necromagic2");
    object oMagicSpot3 = GetWaypointByTag("necromagic3");
    object oMagicSpot4 = GetWaypointByTag("necromagic4");
    object oMagicSpot5 = GetWaypointByTag("necromagic4");
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVis, GetLocation(oMagicSpot1));
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVis, GetLocation(oMagicSpot2));
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVis, GetLocation(oMagicSpot3));
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVis, GetLocation(oMagicSpot4));
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eVis, GetLocation(oMagicSpot5));

    oTarget = GetNearestObjectToLocation(OBJECT_TYPE_CREATURE, nCasterLocation, nCnt);

    while ((GetIsObjectValid(oTarget)) && (nStop == 0))
    {
        lTarget = GetLocation(oTarget);
        //Get the distance of the target from the center of the effect
        fDelay = GetRandomDelay(3.0, 4.0);//
        fTargetDistance = GetDistanceBetweenLocations(nCasterLocation, lTarget);
        //Check that the current target is valid and closer than 10.0m
        if(GetIsObjectValid(oTarget) && fTargetDistance <= 50.0)
        {
             // Make sure they arent one of the necro's pet
             if((GetTag(oTarget) != "raiseme") && (GetTag(oTarget) != "raiseme2") && (GetTag(oTarget) != "necroboss"))
             {

                    //Make a relex save
                    if(!MySavingThrow(SAVING_THROW_REFLEX, oTarget, 45, SAVING_THROW_TYPE_FIRE)) //, OBJECT_SELF, 3.0))
                    {


                        if((GetHasFeat(FEAT_IMPROVED_EVASION,oTarget)))   // Half damage if improved evasion
                        {
                          //Apply the VFX and damage
                          eDam = EffectDamage(SpellDiceRoll(0,1), DAMAGE_TYPE_FIRE);
                          ApplyEffectToObject(DURATION_TYPE_INSTANT, eFlamestrike, oTarget);
                          ApplyEffectToObject(DURATION_TYPE_INSTANT, eDam, oTarget);
                          ApplyEffectToObject(DURATION_TYPE_INSTANT, eShake, oTarget);

                        }
                        else   // Full damage
                        {
                          //Apply the VFX and damage
                          eDam = EffectDamage(SpellDiceRoll(0,0), DAMAGE_TYPE_FIRE);
                          ApplyEffectToObject(DURATION_TYPE_INSTANT, eFlamestrike, oTarget);
                          ApplyEffectToObject(DURATION_TYPE_INSTANT, eDam, oTarget);
                          ApplyEffectToObject(DURATION_TYPE_INSTANT, eShake, oTarget);
                        }


                    }
                    else  // Half damage or no damage with evasion
                    {

                        if((GetHasFeat(FEAT_EVASION,oTarget)) || (GetHasFeat(FEAT_IMPROVED_EVASION,oTarget))) // no damage
                        {
                          ApplyEffectToObject(DURATION_TYPE_INSTANT, eShake, oTarget);
                          FloatingTextStringOnCreature( "<c þ >You completely dodged the attack!</c>", oTarget, FALSE );
                        }
                        else      // half damage
                        {
                          eDam = EffectDamage(SpellDiceRoll(0,1), DAMAGE_TYPE_FIRE);
                          ApplyEffectToObject(DURATION_TYPE_INSTANT, eFlamestrike, oTarget);
                          ApplyEffectToObject(DURATION_TYPE_INSTANT, eDam, oTarget);
                          ApplyEffectToObject(DURATION_TYPE_INSTANT, eShake, oTarget);

                        }

                    }


              }


        }
        else
        {
            //Kick out of the loop
            nStop = 1;
        }
        //Increment the count of creatures targeted
        nCnt++;
        //Get the next closest target in the spell target location.
        oTarget = GetNearestObjectToLocation(OBJECT_TYPE_CREATURE, nCasterLocation, nCnt);
    }

}


void NegativeDeathSpell(object oCaster)     // Negative energy does damage and kills on failed save
{

    //Declare major variables
    int nToAffect;
    object oTarget;
    float fTargetDistance;
    float fDelay;
    location lTarget;
    location nCasterLocation = GetLocation(oCaster);
    effect eNeg = EffectVisualEffect(81);
    effect eAura = EffectVisualEffect(144);
    effect eBlast = EffectLinkEffects(eNeg,eAura);
    effect eVisDam = EffectVisualEffect(VFX_IMP_REDUCE_ABILITY_SCORE);
    effect eShake = EffectVisualEffect( VFX_FNF_SCREEN_SHAKE );
    effect eDam;
    int nCnt = 1;
    int nStop = 0;

    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eBlast, nCasterLocation);

    object oMagicSpot1 = GetWaypointByTag("necromagic1");
    object oMagicSpot2 = GetWaypointByTag("necromagic2");
    object oMagicSpot3 = GetWaypointByTag("necromagic3");
    object oMagicSpot4 = GetWaypointByTag("necromagic4");
    object oMagicSpot5 = GetWaypointByTag("necromagic4");
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eBlast, GetLocation(oMagicSpot1));
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eBlast, GetLocation(oMagicSpot2));
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eBlast, GetLocation(oMagicSpot3));
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eBlast, GetLocation(oMagicSpot4));
    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, eBlast, GetLocation(oMagicSpot5));


    oTarget = GetNearestObjectToLocation(OBJECT_TYPE_CREATURE, nCasterLocation, nCnt);

    while ((GetIsObjectValid(oTarget)) && (nStop == 0))
    {
        lTarget = GetLocation(oTarget);
        //Get the distance of the target from the center of the effect
        fDelay = GetRandomDelay(3.0, 4.0);//
        fTargetDistance = GetDistanceBetweenLocations(nCasterLocation, lTarget);
        //Check that the current target is valid and closer than 10.0m
        if(GetIsObjectValid(oTarget) && fTargetDistance <= 50.0)
        {


           // Make sure they arent one of the necro's pet
           if((GetTag(oTarget) != "raiseme") && (GetTag(oTarget) != "raiseme2") && (GetTag(oTarget) != "necroboss"))
           {

             // Make sure they dont have death immunity
             if(!GetIsImmune(oTarget, IMMUNITY_TYPE_DEATH))
             {

                    //Make a fortitude save to avoid death
                    if(!MySavingThrow(SAVING_THROW_FORT, oTarget, 45, SAVING_THROW_TYPE_DEATH)) //, OBJECT_SELF, 3.0))
                    {
                        //Apply the delay VFX impact and death effect
                        ApplyEffectToObject(DURATION_TYPE_INSTANT, eBlast, oTarget);
                        ApplyEffectToObject(DURATION_TYPE_INSTANT, eVisDam, oTarget);
                        effect eDeath = EffectDeath();
                        ApplyEffectToObject(DURATION_TYPE_INSTANT, eDeath, oTarget); // no delay
                        ApplyEffectToObject(DURATION_TYPE_INSTANT, eShake, oTarget);
                    }
                    else // If the saving throw is made still deal damage
                    {

                          //Apply the VFX and damage
                          eDam = EffectDamage(SpellDiceRoll(1,0), DAMAGE_TYPE_NEGATIVE);
                          ApplyEffectToObject(DURATION_TYPE_INSTANT, eVisDam, oTarget);
                          ApplyEffectToObject(DURATION_TYPE_INSTANT, eBlast, oTarget);
                          ApplyEffectToObject(DURATION_TYPE_INSTANT, eDam, oTarget);
                          ApplyEffectToObject(DURATION_TYPE_INSTANT, eShake, oTarget);

                    }

              }
              else // It will still deal damage
              {

                   //Apply the VFX and damage
                   eDam = EffectDamage(SpellDiceRoll(1,0), DAMAGE_TYPE_NEGATIVE);
                   ApplyEffectToObject(DURATION_TYPE_INSTANT, eVisDam, oTarget);
                   ApplyEffectToObject(DURATION_TYPE_INSTANT, eBlast, oTarget);
                   ApplyEffectToObject(DURATION_TYPE_INSTANT, eDam, oTarget);
                   ApplyEffectToObject(DURATION_TYPE_INSTANT, eShake, oTarget);

              }
            }

        }
        else
        {
            //Kick out of the loop
            nStop = 1;
        }
        //Increment the count of creatures targeted
        nCnt++;
        //Get the next closest target in the spell target location.
        oTarget = GetNearestObjectToLocation(OBJECT_TYPE_CREATURE, nCasterLocation, nCnt);
    }


}



void LootDrop(object oArea, object oWayPoint1, object oWaypoint2, object oDamager)
{
    int nRandom  = Random(100);
    int nRandom2 = Random(100);
    int nRandom3 = Random(5);
    int nPartySize = 0;
    object oHorde = CreateObject(OBJECT_TYPE_PLACEABLE,"lichlootchest",GetLocation(oWayPoint1));
    CreateObject(OBJECT_TYPE_PLACEABLE,"lbossexit",GetLocation(oWaypoint2));

    object oPartyMember  = GetFirstFactionMember( oDamager, TRUE );
    while(GetIsObjectValid(oPartyMember) == TRUE)
    {
     nPartySize++;
     oPartyMember = GetNextFactionMember(oDamager, TRUE);
    }

    // Removes the variable on the raid summoner when the boss "dies" so it can be summoned again
    object oRaidSpawner = GetObjectByTag("raidsummonerlich");
    DeleteLocalInt(oRaidSpawner,"bossOut");
    //

    if(nRandom <= (10+nPartySize))      //  Divine Mythal
    {
      DelayCommand(0.5,CreateLoot("mythal7",oHorde));
    }

    if(nRandom2 <= (20+nPartySize))  // Item drops
    {
      DelayCommand(0.5,CreateLoot("raid_base_lich",oHorde));
    }
    DelayCommand(0.5,GenerateEpicLoot(oHorde));
    DelayCommand(0.5,GenerateEpicLoot(oHorde));
    DelayCommand(0.5,GenerateEpicLoot(oHorde));

    // Every 3 party members past 5 will be an extra epic loot.
    int i;
    int p = (nPartySize - 5)/3;
    for( i=0; i<p; i++)
    {
     DelayCommand(0.5,GenerateEpicLoot(oHorde));
    }

}

int SpellDiceRoll(int nSpell, int nHalf) // nSpell = 0 is the Fire spell calc, 1 is the Negative energy spell calc | int nHalf = 0 the spell damage is not halved, and 1 is the spell damage is halved
{
   int nCount = 0;
   int nDamage = 0;

  if(nSpell == 0)// Fire Spell
  {

  while(nCount < 39)   // 40d10 Damage
  {
     nDamage = nDamage + (Random(10)+1);
     nCount++;
  }

  if(nHalf == 1)
  {
     nDamage = nDamage/2;
  }

  }
  else if(nSpell == 1) // Negative Energy Spell
  {

  while(nCount < 19)   // 20d10 Damage
  {
     nDamage = nDamage + (Random(10)+1);
     nCount++;
  }

  if(nHalf == 1)
  {
     nDamage = nDamage/2;
  }

  }

  return nDamage;
}

void CreateLoot(string sResRef, object Chest)
{
    CreateItemOnObject(sResRef,Chest);
}
