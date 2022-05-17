using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Items
{
    public class ScouringToxin : Item, IUsesRemaining, ICommodity
    {
        public override int LabelNumber => 1112292;  // scouring toxin

        private int m_UsesRemaining;

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining { get => m_UsesRemaining; set { m_UsesRemaining = value; if (m_UsesRemaining <= 0) Delete(); else InvalidateProperties(); } }

        public bool ShowUsesRemaining { get => false; set { { } } }

        [Constructable]
        public ScouringToxin()
            : this(1)
        {
        }

        [Constructable]
        public ScouringToxin(int amount)
            : base(0x1848)
        {
            Stackable = true;
            m_UsesRemaining = amount;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1112348); // Which item do you wish to scour?
                from.BeginTarget(-1, false, Targeting.TargetFlags.None, OnTarget);
            }
        }

        public void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Item item)
            {
                if (item.Parent is Mobile)
                {
                    from.SendLocalizedMessage(1112350); // You cannot scour items that are being worn!
                }
                else if (item.IsLockedDown || item.IsSecure)
                {
                    from.SendLocalizedMessage(1112351); // You may not scour items which are locked down.
                }
                else if (item.QuestItem)
                {
                    from.SendLocalizedMessage(1151837); // You may not scour toggled quest items.
                }
                else if (item is DryReeds dryReeds)
                {
                    if (!(from is PlayerMobile) || !((PlayerMobile)from).BasketWeaving)
                    {
                        from.SendLocalizedMessage(1112253); //You haven't learned basket weaving. Perhaps studying a book would help!
                    }
                    else
                    {
                        Container cont = from.Backpack;

                        Engines.Plants.PlantHue hue = dryReeds.PlantHue;

                        if (!dryReeds.IsChildOf(from.Backpack))
                        {
                            from.SendLocalizedMessage(1116249); //That must be in your backpack for you to use it.
                        }
                        else if (cont != null)
                        {
                            Item[] items = cont.FindItemsByType(typeof(DryReeds));
                            List<Item> list = new List<Item>();

                            int total = 0;

                            for (var index = 0; index < items.Length; index++)
                            {
                                Item it = items[index];

                                if (it is DryReeds check)
                                {
                                    if (dryReeds.PlantHue == check.PlantHue)
                                    {
                                        total += check.Amount;
                                        list.Add(check);
                                    }
                                }
                            }

                            int toConsume = 2;

                            if (list.Count > 0 && total > 1)
                            {
                                for (var index = 0; index < list.Count; index++)
                                {
                                    Item it = list[index];

                                    if (it.Amount >= toConsume)
                                    {
                                        it.Consume(toConsume);
                                        toConsume = 0;
                                    }
                                    else if (it.Amount < toConsume)
                                    {
                                        it.Delete();
                                        toConsume -= it.Amount;
                                    }

                                    if (toConsume <= 0)
                                    {
                                        break;
                                    }
                                }

                                SoftenedReeds sReed = new SoftenedReeds(hue);

                                if (!from.Backpack.TryDropItem(from, sReed, false))
                                {
                                    sReed.MoveToWorld(from.Location, from.Map);
                                }

                                m_UsesRemaining--;

                                if (m_UsesRemaining <= 0)
                                {
                                    Delete();
                                }
                                else
                                {
                                    InvalidateProperties();
                                }

                                from.PlaySound(0x23E);
                            }
                            else
                            {
                                from.SendLocalizedMessage(1112250); //You don't have enough of this type of dry reeds to make that.
                            }
                        }
                    }
                }
                else if (BasePigmentsOfTokuno.IsValidItem(item))
                {
                    from.PlaySound(0x23E);

                    item.Hue = 0;

                    m_UsesRemaining--;

                    if (m_UsesRemaining <= 0)
                    {
                        Delete();
                    }
                    else
                    {
                        InvalidateProperties();
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1112349); // You cannot scour that!
                }
            }
            else
            {
                from.SendLocalizedMessage(1112349); // You cannot scour that!
            }
        }

        public ScouringToxin(Serial serial)
            : base(serial)
        {
        }

        TextDefinition ICommodity.Description => LabelNumber;
        bool ICommodity.IsDeedable => true;

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(2); // version

            writer.Write(m_UsesRemaining);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();

            m_UsesRemaining = reader.ReadInt();
        }
    }
}
