using Server.Gumps;
using System;

namespace Server.Mobiles
{
    public class WildfireOstardStatue : Item, ICreatureStatuette
    {
        public override int LabelNumber => 1159675;  // Wildfire Ostard

        public Type CreatureType => typeof(WildfireOstard);

        [Constructable]
        public WildfireOstardStatue()
            : base(0x2136)
        {
            Hue = 2758;
        }

        public WildfireOstardStatue(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1158954); // *Redeemable for a pet*<br>*Requires Grandmaster Taming to Claim Pet*
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                if (from.Skills[SkillName.AnimalTaming].Value >= 100)
                {
                    from.SendGump(new ConfirmMountStatuetteGump(this));
                }
                else
                {
                    from.SendLocalizedMessage(1158959, "100"); // ~1_SKILL~ Animal Taming skill is required to redeem this pet.
                }
            }
            else
            {
                SendLocalizedMessageTo(from, 1010095); // This must be on your person to use.
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }

    [CorpseName("a wildfire ostard corpse")]
    public class WildfireOstard : BaseMount
    {
        [Constructable]
        public WildfireOstard()
            : this("Wildfire Ostard")
        {
        }

        [Constructable]
        public WildfireOstard(string name)
            : base(name, 0xDA, 0x3EA4, AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Hue = 2758;

            BaseSoundID = 0x275;

            SetStr(540);
            SetDex(100);
            SetInt(150);

            SetHits(400);
            SetStam(100);
            SetMana(150);

            SetDamage(16, 22);

            SetResistance(ResistanceType.Physical, 65);
            SetResistance(ResistanceType.Fire, 45);
            SetResistance(ResistanceType.Cold, 40);
            SetResistance(ResistanceType.Poison, 55);
            SetResistance(ResistanceType.Energy, 35);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Cold, 25);
            SetDamageType(ResistanceType.Poison, 25);

            SetSkill(SkillName.MagicResist, 92.9, 100.0);
            SetSkill(SkillName.Tactics, 98.5, 100.0);
            SetSkill(SkillName.Wrestling, 96.3, 100.0);
            SetSkill(SkillName.Poisoning, 65.0, 100.0);
            SetSkill(SkillName.DetectHidden, 61.7, 100.0);
            SetSkill(SkillName.Magery, 30.9, 100.0);
            SetSkill(SkillName.EvalInt, 39.2, 100.0);

            Fame = 300;
            Karma = 300;

            Tamable = true;
            ControlSlots = 3;
            MinTameSkill = 96.0;
        }

        public WildfireOstard(Serial serial)
            : base(serial)
        {
        }

        public override bool DeleteOnRelease => true;
        public override int Meat => 3;
        public override FoodType FavoriteFood => FoodType.Meat;
        public override PackInstinct PackInstinct => PackInstinct.Ostard;

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }
}
