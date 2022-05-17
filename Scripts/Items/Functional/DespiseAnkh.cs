using Server.Engines.Quests;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Despise
{
    public class DespiseAnkh : BaseAddon
    {
        private Alignment m_Alignment;

        [CommandProperty(AccessLevel.GameMaster)]
        public Alignment Alignment { get => m_Alignment; set => m_Alignment = value; }

        public override bool HandlesOnMovement => true;

        public DespiseAnkh(Alignment alignment)
        {
            m_Alignment = alignment;

            switch (alignment)
            {
                default:
                case Alignment.Good:
                    AddComponent(new AddonComponent(4), 0, 0, 0);
                    AddComponent(new AddonComponent(5), +1, 0, 0);
                    break;
                case Alignment.Evil:
                    AddComponent(new AddonComponent(2), 0, 0, 0);
                    AddComponent(new AddonComponent(3), 0, -1, 0);
                    break;
            }
        }

        public override void OnComponentUsed(AddonComponent c, Mobile from)
        {
            if (from.InRange(c.Location, 3) && from.Backpack != null)
            {
                for (var index = 0; index < WispOrb.Orbs.Count; index++)
                {
                    var x = WispOrb.Orbs[index];

                    if (x.Owner == from)
                    {
                        LabelTo(from, 1153357); // Thou can guide but one of us.
                        return;
                    }
                }

                Alignment alignment = Alignment.Neutral;

                if (from.Karma > 0 && m_Alignment == Alignment.Good)
                {
                    alignment = Alignment.Good;
                }
                else if (from.Karma < 0 && m_Alignment == Alignment.Evil)
                {
                    alignment = Alignment.Evil;
                }

                if (alignment != Alignment.Neutral)
                {
                    WispOrb orb = new WispOrb(from, alignment);
                    from.Backpack.DropItem(orb);
                    from.SendLocalizedMessage(1153355); // I will follow thy guidance.

                    if (from is PlayerMobile mobile && QuestHelper.HasQuest<WhisperingWithWispsQuest>(mobile))
                    {
                        mobile.SendLocalizedMessage(1158304); // The Ankh pulses with energy in front of you! You are drawn to it! As you
                                                            // place your hand on the ankh an inner voice speaks to you as you are joined to your Wisp companion...
                        mobile.SendLocalizedMessage(1158320, null, 0x23); // You've completed a quest objective!
                        mobile.PlaySound(0x5B5);

                        Services.TownCryer.TownCryerSystem.CompleteQuest(mobile, 1158303, 1158308, 0x65C);
                    }
                }
                else
                {
                    LabelTo(from, 1153350); // Thy spirit be not compatible with our goals!
                }
            }
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            bool any = false;

            for (var index = 0; index < WispOrb.Orbs.Count; index++)
            {
                var x = WispOrb.Orbs[index];

                if (x.Owner == m)
                {
                    any = true;
                    break;
                }
            }

            if (m is PlayerMobile pm && !any && QuestHelper.HasQuest<WhisperingWithWispsQuest>(pm) && InRange(pm.Location, 5) && !InRange(oldLocation, 5))
            {
                pm.SendLocalizedMessage(1158311); // You have found an ankh. Use the ankh to continue your journey.
            }
        }

        public DespiseAnkh(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write((int)m_Alignment);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();

            m_Alignment = (Alignment)reader.ReadInt();
        }
    }
}
