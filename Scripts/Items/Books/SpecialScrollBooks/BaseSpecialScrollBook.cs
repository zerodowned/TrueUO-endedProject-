using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    [Flipable(0x9A95, 0x9AA7)]
    public abstract class BaseSpecialScrollBook : Container, ISecurable
    {
        public const int MaxScrolls = 300;

        private int _Capacity;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Capacity
        {
            get => _Capacity <= 0 ? MaxScrolls : _Capacity;
            set
            {
                _Capacity = value;

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level { get; set; }

        public override bool DisplaysContent => false;
        public override double DefaultWeight => 1.0;

        public abstract Type ScrollType { get; }

        public abstract int BadDropMessage { get; }
        public abstract int DropMessage { get; }
        public abstract int RemoveMessage { get; }
        public abstract int GumpTitle { get; }

        public BaseSpecialScrollBook(int id)
            : base(id)
        {
            LootType = LootType.Blessed;
        }

        public override int GetTotal(TotalType type)
        {
            return 0;
        }

        public override void OnDoubleClick(Mobile m)
        {
            var parent = RootParent;

            if (parent != null && parent != m)
            {
                m.SendLocalizedMessage(502405, "", 21); // That is inaccessible.
                m.SendLocalizedMessage(1061637); // You are not allowed to access this.
            }
            else if (m is PlayerMobile mobile && mobile.InRange(GetWorldLocation(), 2))
            {
                BaseGump.SendGump(new SpecialScrollBookGump(mobile, this));
            }
            else if (m.AccessLevel > AccessLevel.Player)
            {
                base.OnDoubleClick(m);
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1151797, string.Format("{0}\t{1}", Items.Count, Capacity)); // Scrolls in book: ~1_val~/~2_val~
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            SetSecureLevelEntry.AddTo(from, this, list);
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            from.SendMessage("HACKER! GET YOUR STEAM OUT OF HERE!!!");

            return false;
        }

        public override bool OnDragDrop(Mobile m, Item dropped)
        {
            if (m.InRange(GetWorldLocation(), 2))
            {
                BaseHouse house = BaseHouse.FindHouseAt(this);

                if (dropped.GetType() != ScrollType)
                {
                    m.SendLocalizedMessage(BadDropMessage);
                }
                else if (house == null || !IsLockedDown)
                {
                    m.SendLocalizedMessage(1151765); // You must lock this book down in a house to add scrolls to it.
                }
                else if (!house.CheckAccessibility(this, m))
                {
                    m.SendLocalizedMessage(1155693); // This item is impermissible and can not be added to the book.
                }
                else if (Items.Count < Capacity) // TODO: Message for overfilled?
                {
                    DropItem(dropped);

                    m.SendLocalizedMessage(DropMessage);

                    dropped.Movable = false;

                    m.CloseGump(typeof(SpecialScrollBookGump));

                    return true;
                }
            }

            return false;
        }

        public virtual void Construct(Mobile m, SkillName sk, double value)
        {
            SpecialScroll scroll = null;

            for (var index = 0; index < Items.Count; index++)
            {
                Item item = Items[index];

                if (item is SpecialScroll s && s.Skill == sk && s.Value == value)
                {
                    scroll = s;
                    break;
                }
            }

            if (scroll != null)
            {
                if (m.Backpack == null || !m.Backpack.TryDropItem(m, scroll, false))
                {
                    m.SendLocalizedMessage(502868); // Your backpack is too full.
                }
                else
                {
                    BaseHouse house = BaseHouse.FindHouseAt(this);

                    if (house != null && house.LockDowns.ContainsKey(scroll))
                    {
                        house.LockDowns.Remove(scroll);
                    }

                    if (!scroll.Movable)
                    {
                        scroll.Movable = true;
                    }

                    if (scroll.IsLockedDown)
                    {
                        scroll.IsLockedDown = false;
                    }

                    InvalidateContainers(Parent);

                    m.UpdateTotals();
                    m.SendLocalizedMessage(RemoveMessage);                             
                }
            }
        }

        public BaseSpecialScrollBook(Serial serial)
            : base(serial)
        {
        }

        private void InvalidateContainers(object parent)
        {
            while (true)
            {
                if (parent is Container c)
                {
                    c.InvalidateProperties();
                    parent = c.Parent;
                    continue;
                }

                break;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(3); // version

            writer.Write((int)Level);
            writer.Write(_Capacity);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();

            Level = (SecureLevel)reader.ReadInt();
            _Capacity = reader.ReadInt();

            Timer.DelayCall(
                () =>
                {
                    for (var index = 0; index < Items.Count; index++)
                    {
                        Item item = Items[index];

                        if (item.Movable)
                        {
                            item.Movable = false;
                        }
                    }
                });
        }

        public virtual Dictionary<SkillCat, List<SkillName>> SkillInfo => null;
        public virtual Dictionary<int, double> ValueInfo => null;

        public static int GetCategoryLocalization(SkillCat category)
        {
            switch (category)
            {
                default:
                    //case SkillCat.None:
                    return 0;
                case SkillCat.Miscellaneous:
                    return 1078596;
                case SkillCat.Combat:
                    return 1078592;
                case SkillCat.TradeSkills:
                    return 1078591;
                case SkillCat.Magic:
                    return 1078593;
                case SkillCat.Wilderness:
                    return 1078595;
                case SkillCat.Thievery:
                    return 1078594;
                case SkillCat.Bard:
                    return 1078590;
            }
        }
    }
}
