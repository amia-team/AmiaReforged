using SpellId = int;
using PolymorphId = int;

namespace AmiaReforged.Classes.EffectUtils.Polymorph;

/// <summary>
/// Where the spell IDs of the various polymorphs match the polymorph.2da IDs. Control level switches and stuff
/// like that in the actual spell scripts so as not to conflate this mapping.
/// </summary>
public static class PolymorphMapping
{
    public static class GreaterWildshape1
    {
        public const int EpicLevelRequirement = 20;

        public static readonly Dictionary<SpellId, PolymorphId> Standard = new()
        {
            { 658, Polymorph.RedWyrmling },
            { 659, Polymorph.BlueWyrmling },
            { 660, Polymorph.BlackWyrmling },
            { 661, Polymorph.WhiteWyrmling },
            { 662, Polymorph.GreenWyrmling }
        };

        public static readonly Dictionary<SpellId, PolymorphId> Epic = new()
        {
            { 658, Polymorph.EpicRedWyrmling },
            { 659, Polymorph.EpicBlueWyrmling },
            { 660, Polymorph.EpicBlackWyrmling },
            { 661, Polymorph.EpicWhiteWyrmling },
            { 662, Polymorph.EpicGreenWyrmling }
        };
    }

    public static class GreaterWildshape2
    {
        public const int EpicLevelRequirement = 21;

        public static readonly Dictionary<SpellId, PolymorphId> Standard = new()
        {
            { 672, Polymorph.Harpy },
            { 678, Polymorph.Gargoyle },
            { 680, Polymorph.Minotaur }
        };

        public static readonly Dictionary<SpellId, PolymorphId> Epic = new()
        {
            { 672, Polymorph.EpicHarpy },
            { 678, Polymorph.EpicGargoyle },
            { 680, Polymorph.EpicMinotaur }
        };
    }

    public static class GreaterWildshape3
    {
        public const int EpicLevelRequirement = 22;

        public static readonly Dictionary<SpellId, PolymorphId> Standard = new()
        {
            { 670, Polymorph.Basilisk },
            { 673, Polymorph.Drider },
            { 674, Polymorph.Manticore }
        };

        public static readonly Dictionary<SpellId, PolymorphId> Epic = new()
        {
            { 670, Polymorph.EpicBasilisk },
            { 673, Polymorph.EpicDrider },
            { 674, Polymorph.EpicManticore }
        };
    }

    public static class HumanoidShape1
    {
        public const int EpicLevelRequirement = 23;

        public static readonly Dictionary<SpellId, PolymorphId> StandardMale = new()
        {
            { 682, Polymorph.DrowMale },
            { 683, Polymorph.Lizardfolk },
            { 684, Polymorph.Kobold }
        };

        public static readonly Dictionary<SpellId, PolymorphId> StandardFemale = new()
        {
            { 682, Polymorph.DrowFemale },
            { 683, Polymorph.Lizardfolk },
            { 684, Polymorph.Kobold }
        };

        public static readonly Dictionary<SpellId, PolymorphId> EpicMale = new()
        {
            { 682, Polymorph.EpicDrowMale },
            { 683, Polymorph.EpicLizardfolk },
            { 684, Polymorph.EpicKobold }
        };

        public static readonly Dictionary<SpellId, PolymorphId> EpicFemale = new()
        {
            { 682, Polymorph.EpicDrowFemale },
            { 683, Polymorph.EpicLizardfolk },
            { 684, Polymorph.EpicKobold }
        };
    }

    public static class GreaterWildshape4
    {
        public const int EpicLevelRequirement = 24;

        public static readonly Dictionary<SpellId, PolymorphId> Standard = new()
        {
            { 679, Polymorph.Medusa },
            { 691, Polymorph.Mindflayer },
            { 694, Polymorph.DireTiger }
        };

        public static readonly Dictionary<SpellId, PolymorphId> Epic = new()
        {
            { 679, Polymorph.EpicMedusa },
            { 691, Polymorph.EpicMindflayer },
            { 694, Polymorph.EpicDireTiger }
        };
    }

    public static class HumanoidShape2
    {
        public const int EpicLevelRequirement = 25;

        public static readonly Dictionary<SpellId, PolymorphId> StandardMale = new()
        {
            { 964, Polymorph.DwarfMale },
            { 965, Polymorph.Ogre },
            { 966, Polymorph.YuanTi }
        };

        public static readonly Dictionary<SpellId, PolymorphId> StandardFemale = new()
        {
            { 964, Polymorph.DwarfFemale },
            { 965, Polymorph.Ogre },
            { 966, Polymorph.YuanTi }
        };

        public static readonly Dictionary<SpellId, PolymorphId> EpicMale = new()
        {
            { 964, Polymorph.EpicDwarfMale },
            { 965, Polymorph.EpicOgre },
            { 966, Polymorph.EpicYuanTi }
        };

        public static readonly Dictionary<SpellId, PolymorphId> EpicFemale = new()
        {
            { 964, Polymorph.EpicDwarfFemale },
            { 965, Polymorph.EpicOgre },
            { 966, Polymorph.YuanTi }
        };
    }

    public static class ElementalShape
    {
        public static readonly Dictionary<SpellId, PolymorphId> Large = new()
        {
            { 397, Polymorph.LargeFireElemental },
            { 398, Polymorph.LargeWaterElemental },
            { 399, Polymorph.LargeEarthElemental },
            { 400, Polymorph.LargeAirElemental }
        };

        public const int ImprovedLevelRequirement = 20;

        public static readonly Dictionary<SpellId, PolymorphId> Huge = new()
        {
            { 397, Polymorph.HugeFireElemental },
            { 398, Polymorph.HugeWaterElemental },
            { 399, Polymorph.HugeEarthElemental },
            { 400, Polymorph.HugeAirElemental }
        };

        public const int EpicLevelRequirement = 25;

        public static readonly Dictionary<SpellId, PolymorphId> Elder = new()
        {
            { 397, Polymorph.ElderFireElemental },
            { 398, Polymorph.ElderWaterElemental },
            { 399, Polymorph.ElderEarthElemental },
            { 400, Polymorph.ElderAirElemental }
        };
    }

    public static class WildShape
    {
        public static readonly Dictionary<SpellId, PolymorphId> Base = new()
        {
            { 401, Polymorph.BrownBear },
            { 402, Polymorph.Panther },
            { 403, Polymorph.Wolf },
            { 404, Polymorph.Boar },
            { 405, Polymorph.Badger }
        };

        public const int ImprovedLevelRequirement = 12;

        public static readonly Dictionary<SpellId, PolymorphId> Elder = new()
        {
            { 401, Polymorph.Grizzly },
            { 402, Polymorph.Cougar },
            { 403, Polymorph.ElderWolf },
            { 404, Polymorph.ElderBoar },
            { 405, Polymorph.ElderBadger }
        };

        public const int EpicLevelRequirement = 25;

        public static readonly Dictionary<SpellId, PolymorphId> Epic = new()
        {
            { 401, Polymorph.EpicDireBear },
            { 402, Polymorph.EpicDireCougar },
            { 403, Polymorph.EpicDireWolf },
            { 404, Polymorph.EpicDireBoar },
            { 405, Polymorph.EpicDireBadger }
        };
    }

    public static class Shapechange
    {
        public static readonly Dictionary<SpellId, PolymorphId> Standard = new()
        {
            { 392, Polymorph.ShapechangeRustDragon },
            { 393, Polymorph.ShapechangeFireGiant },
            { 394, Polymorph.ShapechangeJanni },
            { 395, Polymorph.ShapechangeDeathSlaad },
            { 396, Polymorph.ShapechangeIronGolem }
        };

        public static readonly Dictionary<SpellId, PolymorphId> Epic = new()
        {
            { 392, Polymorph.EpicShapechangeRustDragon },
            { 393, Polymorph.EpicShapechangeFireGiant },
            { 394, Polymorph.EpicShapechangeJanni },
            { 395, Polymorph.EpicShapechangeDeathSlaad },
            { 396, Polymorph.EpicShapechangeIronGolem }
        };
    }

    public static class PolymorphSelf
    {
        public static readonly Dictionary<SpellId, PolymorphId> Shapes = new()
        {
            { 387, Polymorph.GiantSpider },
            { 388, Polymorph.Troll },
            { 389, Polymorph.UmberHulk },
            { 390, Polymorph.Pixie },
            { 391, Polymorph.Zombie }
        };
    }

    public static class DragonShape
    {
        public static readonly Dictionary<SpellId, PolymorphId> Standard = new()
        {
            { 707, Polymorph.AncientRedDragon },
            { 708, Polymorph.AncientGreenDragon },
            { 709, Polymorph.AncientBlueDragon }
        };
    }

    public static class UndeadShape
    {
        public static readonly Dictionary<SpellId, PolymorphId> Standard = new()
        {
            { 704, Polymorph.RotHarbinger },
            { 705, Polymorph.VampireMale },
            { 706, Polymorph.Spectre }
        };

        public static readonly Dictionary<SpellId, PolymorphId> StandardFemale = new()
        {
            { 704, Polymorph.RotHarbinger },
            { 705, Polymorph.VampireFemale },
            { 706, Polymorph.Spectre }
        };
    }

    public static class GiantShape
    {
        public static readonly Dictionary<SpellId, PolymorphId> Standard = new()
        {
            { 968, Polymorph.Cyclops },
            { 969, Polymorph.FireGiantMale },
            { 970, Polymorph.MountainGiant }
        };

        public static readonly Dictionary<SpellId, PolymorphId> StandardFemale = new()
        {
            { 968, Polymorph.Cyclops },
            { 969, Polymorph.FireGiantFemale },
            { 970, Polymorph.MountainGiant }
        };
    }

    public static class OutsiderShape
    {
        public static readonly Dictionary<SpellId, PolymorphId> Standard = new()
        {
            { 733, Polymorph.AzerMale },
            { 734, Polymorph.RakshasaMale },
            { 735, Polymorph.DeathSlaad }
        };

        public static readonly Dictionary<SpellId, PolymorphId> StandardFemale = new()
        {
            { 733, Polymorph.AzerFemale },
            { 734, Polymorph.RakshasaFemale },
            { 735, Polymorph.DeathSlaad }
        };
    }

    public static class ConstructShape
    {
        public static readonly Dictionary<SpellId, PolymorphId> Standard = new()
        {
            { 738, Polymorph.StoneGolem },
            { 739, Polymorph.DemonfleshGolem },
            { 740, Polymorph.IronGolem }
        };
    }

    public static class GargantuanShape
    {
        public static readonly Dictionary<SpellId, PolymorphId> Standard = new()
        {
            { 972, Polymorph.Grick },
            { 973, Polymorph.DemonOverlord },
            { 974, Polymorph.PyroclasticDragon }
        };
    }

    public static class LycanShape
    {
        public static class Feats
        {
            public const int Wolf = 1249;
            public const int Bear = 1250;
            public const int Cat = 1251;
            public const int Boar = 1252;
            public const int Bat = 1253;
            public const int Rat = 1254;
            public const int Chicken = 1276;
            public const int Owl = 1329;
            public const int Croc = 1330;
            public const int Shark = 1331;
            public const int Fox = 1332;
            public const int Raccoon = 1333;
        }

        public static readonly Dictionary<int, PolymorphId[]> Forms = new()
        {
            {
                Feats.Wolf,
                [
                    Polymorph.LycanWolf1, Polymorph.LycanWolf2, Polymorph.LycanWolf3, Polymorph.LycanWolf4,
                    Polymorph.LycanWolf5, Polymorph.LycanWolf6
                ]
            },
            {
                Feats.Bear,
                [
                    Polymorph.LycanBear1, Polymorph.LycanBear2, Polymorph.LycanBear3, Polymorph.LycanBear4,
                    Polymorph.LycanBear5, Polymorph.LycanBear6
                ]
            },
            {
                Feats.Cat,
                [
                    Polymorph.LycanCat1, Polymorph.LycanCat2, Polymorph.LycanCat3, Polymorph.LycanCat4,
                    Polymorph.LycanCat5, Polymorph.LycanCat6
                ]
            },
            {
                Feats.Boar,
                [
                    Polymorph.LycanBoar1, Polymorph.LycanBoar2, Polymorph.LycanBoar3, Polymorph.LycanBoar4,
                    Polymorph.LycanBoar5, Polymorph.LycanBoar6
                ]
            },
            {
                Feats.Bat,
                [
                    Polymorph.LycanBat1, Polymorph.LycanBat2, Polymorph.LycanBat3, Polymorph.LycanBat4,
                    Polymorph.LycanBat5, Polymorph.LycanBat6
                ]
            },
            {
                Feats.Rat,
                [
                    Polymorph.LycanRat1, Polymorph.LycanRat2, Polymorph.LycanRat3, Polymorph.LycanRat4,
                    Polymorph.LycanRat5, Polymorph.LycanRat6
                ]
            },
            {
                Feats.Chicken,
                [
                    Polymorph.LycanChicken1, Polymorph.LycanChicken2, Polymorph.LycanChicken3, Polymorph.LycanChicken4,
                    Polymorph.LycanChicken5, Polymorph.LycanChicken6
                ]
            },
            {
                Feats.Owl,
                [
                    Polymorph.LycanOwl1, Polymorph.LycanOwl2, Polymorph.LycanOwl3, Polymorph.LycanOwl4,
                    Polymorph.LycanOwl5, Polymorph.LycanOwl6
                ]
            },
            {
                Feats.Croc,
                [
                    Polymorph.LycanCroc1, Polymorph.LycanCroc2, Polymorph.LycanCroc3, Polymorph.LycanCroc4,
                    Polymorph.LycanCroc5, Polymorph.LycanCroc6
                ]
            },
            {
                Feats.Shark,
                [
                    Polymorph.LycanShark1, Polymorph.LycanShark2, Polymorph.LycanShark3, Polymorph.LycanShark4,
                    Polymorph.LycanShark5, Polymorph.LycanShark6
                ]
            },
            {
                Feats.Fox,
                [
                    Polymorph.LycanFox1, Polymorph.LycanFox2, Polymorph.LycanFox3, Polymorph.LycanFox4,
                    Polymorph.LycanFox5, Polymorph.LycanFox6
                ]
            },
            {
                Feats.Raccoon,
                [
                    Polymorph.LycanRaccoon1, Polymorph.LycanRaccoon2, Polymorph.LycanRaccoon3, Polymorph.LycanRaccoon4,
                    Polymorph.LycanRaccoon5, Polymorph.LycanRaccoon6
                ]
            },
        };
    }

    private static class Polymorph
    {
        public const PolymorphId RedWyrmling = 123;
        public const PolymorphId EpicRedWyrmling = 124;
        public const PolymorphId BlueWyrmling = 125;
        public const PolymorphId EpicBlueWyrmling = 126;
        public const PolymorphId BlackWyrmling = 127;
        public const PolymorphId EpicBlackWyrmling = 128;
        public const PolymorphId GreenWyrmling = 129;
        public const PolymorphId EpicGreenWyrmling = 130;
        public const PolymorphId WhiteWyrmling = 131;
        public const PolymorphId EpicWhiteWyrmling = 132;

        public const PolymorphId Harpy = 133;
        public const PolymorphId EpicHarpy = 134;
        public const PolymorphId Gargoyle = 135;
        public const PolymorphId EpicGargoyle = 136;
        public const PolymorphId Minotaur = 137;
        public const PolymorphId EpicMinotaur = 138;

        public const PolymorphId Basilisk = 139;
        public const PolymorphId EpicBasilisk = 140;
        public const PolymorphId Drider = 141;
        public const PolymorphId EpicDrider = 142;
        public const PolymorphId Manticore = 143;
        public const PolymorphId EpicManticore = 144;

        public const PolymorphId DrowFemale = 145;
        public const PolymorphId EpicDrowFemale = 146;
        public const PolymorphId DrowMale = 147;
        public const PolymorphId EpicDrowMale = 148;
        public const PolymorphId Lizardfolk = 149;
        public const PolymorphId EpicLizardfolk = 150;
        public const PolymorphId Kobold = 151;
        public const PolymorphId EpicKobold = 152;

        public const PolymorphId DwarfFemale = 253;
        public const PolymorphId EpicDwarfFemale = 254;
        public const PolymorphId DwarfMale = 255;
        public const PolymorphId EpicDwarfMale = 256;
        public const PolymorphId Ogre = 257;
        public const PolymorphId EpicOgre = 258;
        public const PolymorphId YuanTi = 259;
        public const PolymorphId EpicYuanTi = 260;

        public const PolymorphId Medusa = 155;
        public const PolymorphId EpicMedusa = 156;
        public const PolymorphId Mindflayer = 157;
        public const PolymorphId EpicMindflayer = 158;
        public const PolymorphId DireTiger = 153;
        public const PolymorphId EpicDireTiger = 154;

        public const PolymorphId LargeFireElemental = 199;
        public const PolymorphId HugeFireElemental = 200;
        public const PolymorphId ElderFireElemental = 201;
        public const PolymorphId LargeWaterElemental = 202;
        public const PolymorphId HugeWaterElemental = 203;
        public const PolymorphId ElderWaterElemental = 204;
        public const PolymorphId LargeEarthElemental = 205;
        public const PolymorphId HugeEarthElemental = 206;
        public const PolymorphId ElderEarthElemental = 207;
        public const PolymorphId LargeAirElemental = 208;
        public const PolymorphId HugeAirElemental = 209;
        public const PolymorphId ElderAirElemental = 210;

        public const PolymorphId BrownBear = 232;
        public const PolymorphId Panther = 233;
        public const PolymorphId Wolf = 234;
        public const PolymorphId Boar = 235;
        public const PolymorphId Badger = 236;
        public const PolymorphId Grizzly = 227;
        public const PolymorphId Cougar = 228;
        public const PolymorphId ElderWolf = 229;
        public const PolymorphId ElderBoar = 230;
        public const PolymorphId ElderBadger = 231;
        public const PolymorphId EpicDireBear = 222;
        public const PolymorphId EpicDireCougar = 223;
        public const PolymorphId EpicDireWolf = 224;
        public const PolymorphId EpicDireBoar = 225;
        public const PolymorphId EpicDireBadger = 226;

        public const PolymorphId GiantSpider = 3;
        public const PolymorphId Troll = 4;
        public const PolymorphId UmberHulk = 5;
        public const PolymorphId Pixie = 6;
        public const PolymorphId Zombie = 7;

        public const PolymorphId ShapechangeRustDragon = 237;
        public const PolymorphId ShapechangeFireGiant = 238;
        public const PolymorphId ShapechangeJanni = 239;
        public const PolymorphId ShapechangeDeathSlaad = 240;
        public const PolymorphId ShapechangeIronGolem = 241;

        public const PolymorphId EpicShapechangeRustDragon = 242;
        public const PolymorphId EpicShapechangeFireGiant = 243;
        public const PolymorphId EpicShapechangeJanni = 244;
        public const PolymorphId EpicShapechangeDeathSlaad = 245;
        public const PolymorphId EpicShapechangeIronGolem = 246;

        public const PolymorphId AncientBlueDragon = 71;
        public const PolymorphId AncientRedDragon = 72;
        public const PolymorphId AncientGreenDragon = 73;

        public const PolymorphId RotHarbinger = 159;
        public const PolymorphId VampireFemale = 160;
        public const PolymorphId VampireMale = 161;
        public const PolymorphId Spectre = 162;

        public const PolymorphId Cyclops = 261;
        public const PolymorphId FireGiantFemale = 262;
        public const PolymorphId FireGiantMale = 263;
        public const PolymorphId MountainGiant = 264;

        public const PolymorphId AzerFemale = 163;
        public const PolymorphId AzerMale = 164;
        public const PolymorphId RakshasaFemale = 165;
        public const PolymorphId RakshasaMale = 166;
        public const PolymorphId DeathSlaad = 167;

        public const PolymorphId StoneGolem = 168;
        public const PolymorphId IronGolem = 169;
        public const PolymorphId DemonfleshGolem = 170;

        public const PolymorphId Grick = 265;
        public const PolymorphId DemonOverlord = 266;
        public const PolymorphId PyroclasticDragon = 267;

        public const PolymorphId LycanWolf1 = 268;
        public const PolymorphId LycanWolf2 = 269;
        public const PolymorphId LycanWolf3 = 270;
        public const PolymorphId LycanWolf4 = 271;
        public const PolymorphId LycanWolf5 = 272;
        public const PolymorphId LycanWolf6 = 273;

        public const PolymorphId LycanBear1 = 274;
        public const PolymorphId LycanBear2 = 275;
        public const PolymorphId LycanBear3 = 276;
        public const PolymorphId LycanBear4 = 277;
        public const PolymorphId LycanBear5 = 278;
        public const PolymorphId LycanBear6 = 279;

        public const PolymorphId LycanCat1 = 280;
        public const PolymorphId LycanCat2 = 281;
        public const PolymorphId LycanCat3 = 282;
        public const PolymorphId LycanCat4 = 283;
        public const PolymorphId LycanCat5 = 284;
        public const PolymorphId LycanCat6 = 285;

        public const PolymorphId LycanBoar1 = 286;
        public const PolymorphId LycanBoar2 = 287;
        public const PolymorphId LycanBoar3 = 288;
        public const PolymorphId LycanBoar4 = 289;
        public const PolymorphId LycanBoar5 = 290;
        public const PolymorphId LycanBoar6 = 291;

        public const PolymorphId LycanBat1 = 292;
        public const PolymorphId LycanBat2 = 293;
        public const PolymorphId LycanBat3 = 294;
        public const PolymorphId LycanBat4 = 295;
        public const PolymorphId LycanBat5 = 296;
        public const PolymorphId LycanBat6 = 297;

        public const PolymorphId LycanRat1 = 298;
        public const PolymorphId LycanRat2 = 299;
        public const PolymorphId LycanRat3 = 300;
        public const PolymorphId LycanRat4 = 301;
        public const PolymorphId LycanRat5 = 302;
        public const PolymorphId LycanRat6 = 303;

        public const PolymorphId LycanChicken1 = 304;
        public const PolymorphId LycanChicken2 = 305;
        public const PolymorphId LycanChicken3 = 306;
        public const PolymorphId LycanChicken4 = 307;
        public const PolymorphId LycanChicken5 = 308;
        public const PolymorphId LycanChicken6 = 309;

        public const PolymorphId LycanOwl1 = 311;
        public const PolymorphId LycanOwl2 = 312;
        public const PolymorphId LycanOwl3 = 313;
        public const PolymorphId LycanOwl4 = 314;
        public const PolymorphId LycanOwl5 = 315;
        public const PolymorphId LycanOwl6 = 316;

        public const PolymorphId LycanCroc1 = 317;
        public const PolymorphId LycanCroc2 = 318;
        public const PolymorphId LycanCroc3 = 319;
        public const PolymorphId LycanCroc4 = 320;
        public const PolymorphId LycanCroc5 = 321;
        public const PolymorphId LycanCroc6 = 322;

        public const PolymorphId LycanShark1 = 323;
        public const PolymorphId LycanShark2 = 324;
        public const PolymorphId LycanShark3 = 325;
        public const PolymorphId LycanShark4 = 326;
        public const PolymorphId LycanShark5 = 327;
        public const PolymorphId LycanShark6 = 328;

        public const PolymorphId LycanFox1 = 329;
        public const PolymorphId LycanFox2 = 330;
        public const PolymorphId LycanFox3 = 331;
        public const PolymorphId LycanFox4 = 332;
        public const PolymorphId LycanFox5 = 333;
        public const PolymorphId LycanFox6 = 334;

        public const PolymorphId LycanRaccoon1 = 335;
        public const PolymorphId LycanRaccoon2 = 336;
        public const PolymorphId LycanRaccoon3 = 337;
        public const PolymorphId LycanRaccoon4 = 338;
        public const PolymorphId LycanRaccoon5 = 339;
        public const PolymorphId LycanRaccoon6 = 340;
    }
}
