using Server.Engines.Craft;
using System;

namespace Server.Items
{
    public class HammerOfHephaestus : SmithyHammer
    {
        public static readonly TimeSpan RechargDuration = TimeSpan.FromMinutes(5);
        public static readonly string TimerID = "HammerOfHephaestusTimer";

        [CommandProperty(AccessLevel.GameMaster)]
        public new int UsesRemaining
        {
            get => base.UsesRemaining;
            set
            {
                var uses = value;

                base.UsesRemaining = uses;

                if (uses < 20)
                {
                    if (!TimerRegistry.UpdateRegistry(TimerID, this, RechargDuration))
                    {
                        TimerRegistry.Register(TimerID, this, RechargDuration, false, Tick_Callback);
                    }
                }
                else
                {
                    TimerRegistry.RemoveFromRegistry(TimerID, this);
                }
            }
        }

        [Constructable]
        public HammerOfHephaestus()
        {
            SkillBonuses.SetValues(0, SkillName.Blacksmith, 10.0);
            LootType = LootType.Blessed;
            UsesRemaining = 20;
        }

        public HammerOfHephaestus(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1077740;// Hammer of Hephaestus

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack) || Parent == from)
            {
                if (UsesRemaining > 0)
                {
                    CraftSystem system = CraftSystem;

                    int num = system.CanCraft(from, this, null);

                    if (num > 0)
                    {
                        from.SendLocalizedMessage(num);
                    }
                    else
                    {
                        CraftContext context = system.GetContext(from);

                        from.SendGump(new CraftGump(from, system, this, null));
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1072306); // You must wait a moment for it to recharge.
                }
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public static void Tick_Callback(HammerOfHephaestus hammer)
        {
            hammer.UsesRemaining = Math.Min(20, hammer.UsesRemaining + 1);
            hammer.InvalidateProperties();
        }

        public override bool CanEquip(Mobile from)
        {
            if (UsesRemaining > 0)
                return base.CanEquip(from);

            from.SendLocalizedMessage(1072306); // You must wait a moment for it to recharge.
            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadEncodedInt();

            if (UsesRemaining < 20)
            {
                TimerRegistry.Register(TimerID, this, RechargDuration, false, Tick_Callback);
            }
        }
    }
}
