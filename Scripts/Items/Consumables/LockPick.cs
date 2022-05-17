using Server.Targeting;
using System;
using System.Linq;

namespace Server.Items
{
    public interface ILockpickable : IPoint2D
    {
        int LockLevel { get; set; }
        bool Locked { get; set; }
        Mobile Picker { get; set; }
        int MaxLockLevel { get; set; }
        int RequiredSkill { get; set; }

        void LockPick(Mobile from);
    }

    [Flipable(0x14fc, 0x14fb)]
    public class Lockpick : Item
    {
        public virtual bool IsSkeletonKey => false;
        public virtual int SkillBonus => 0;

        [Constructable]
        public Lockpick()
            : this(1)
        {
        }

        [Constructable]
        public Lockpick(int amount)
            : base(0x14FC)
        {
            Stackable = true;
            Amount = amount;
        }

        public Lockpick(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.SendLocalizedMessage(502068); // What do you want to pick?
            from.Target = new InternalTarget(this);
        }

        public virtual void OnUse()
        {
        }

        protected virtual void BeginLockpick(Mobile from, ILockpickable item)
        {
            if (item.Locked)
            {
                if (item is TreasureMapChest chest && !chest.Guardians.All(g => g.Deleted))
                {
                    from.SendLocalizedMessage(1115991); // You must destroy all the guardians before you can unlock the chest.
                }
                else
                {
                    from.PlaySound(0x241);
                    Timer.DelayCall(TimeSpan.FromMilliseconds(200.0), EndLockpick, new object[] { item, from });
                }
            }
            else
            {
                // The door is not locked
                from.SendLocalizedMessage(502069); // This does not appear to be locked
            }
        }

        protected virtual void BrokeLockPickTest(Mobile from)
        {
            // When failed, a 25% chance to break the lockpick
            if (!IsSkeletonKey && Utility.Random(4) == 0)
            {
                // You broke the lockpick.
                SendLocalizedMessageTo(from, 502074);

                from.PlaySound(0x3A4);
                Consume();
            }
        }

        protected virtual void EndLockpick(object state)
        {
            object[] objs = (object[])state;
            ILockpickable lockpickable = objs[0] as ILockpickable;
            Mobile from = objs[1] as Mobile;

            Item item = (Item)lockpickable;

            if (item != null && from != null && !from.InRange(item.GetWorldLocation(), 1))
            {
                return;
            }

            if (lockpickable != null && (lockpickable.LockLevel == 0 || lockpickable.LockLevel == -255))
            {
                // LockLevel of 0 means that the door can't be picklocked
                // LockLevel of -255 means it's magic locked
                item.SendLocalizedMessageTo(from, 502073); // This lock cannot be picked by normal means
                return;
            }

            if (lockpickable != null && from != null && from.Skills[SkillName.Lockpicking].Value < lockpickable.RequiredSkill - SkillBonus)
            {
                /*
                // Do some training to gain skills
                from.CheckSkill( SkillName.Lockpicking, 0, lockpickable.LockLevel );*/
                // The LockLevel is higher thant the LockPicking of the player
                item.SendLocalizedMessageTo(from, 502072); // You don't see how that lock can be manipulated.
                return;
            }

            if (lockpickable != null)
            {
                int maxlevel = lockpickable.MaxLockLevel;
                int minLevel = lockpickable.LockLevel;

                if (lockpickable is Skeletonkey)
                {
                    minLevel -= SkillBonus;
                    maxlevel -= SkillBonus; //regulars subtract the bonus from the max level
                }

                if (from != null && (this is MasterSkeletonKey || from.CheckTargetSkill(SkillName.Lockpicking, lockpickable, minLevel, maxlevel)))
                {
                    // Success! Pick the lock!
                    OnUse();

                    item.SendLocalizedMessageTo(from, 502076); // The lock quickly yields to your skill.
                    from.PlaySound(0x4A);
                    lockpickable.LockPick(from);
                }
                else
                {
                    // The player failed to pick the lock
                    BrokeLockPickTest(from);
                    item.SendLocalizedMessageTo(from, 502075); // You are unable to pick the lock.

                    if (item is TreasureMapChest chest)
                    {
                        if (!chest.FailedLockpick)
                        {
                            chest.FailedLockpick = true;
                        }
                        else if (chest.Items.Count > 0 && 0.25 > Utility.RandomDouble())
                        {
                            Item toBreak = chest.Items[Utility.Random(chest.Items.Count)];

                            if (!(toBreak is Container))
                            {
                                toBreak.Delete();

                                Effects.PlaySound(chest.Location, chest.Map, 0x1DE);

                                if (from != null)
                                {
                                    from.SendMessage(0x20, "The sound of gas escaping is heard from the chest.");
                                }
                            }
                        }
                    }
                }
            }
        }

        private class InternalTarget : Target
        {
            private readonly Lockpick m_Item;

            public InternalTarget(Lockpick item)
                : base(1, false, TargetFlags.None)
            {
                m_Item = item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted)
                    return;

                if (targeted is ILockpickable lockpickable)
                {
                    m_Item.BeginLockpick(from, lockpickable);
                }
                else
                {
                    from.SendLocalizedMessage(501666); // You can't unlock that!
                }
            }
        }
    }
}
