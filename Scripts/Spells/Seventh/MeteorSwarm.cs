using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Spells.Seventh
{
    public class MeteorSwarmSpell : MagerySpell
    {
        public override DamageType SpellDamageType => DamageType.SpellAOE;
        public Item Item { get; }

        private static readonly SpellInfo m_Info = new SpellInfo(
            "Meteor Swarm", "Flam Kal Des Ylem",
            233,
            9042,
            false,
            Reagent.Bloodmoss,
            Reagent.MandrakeRoot,
            Reagent.SulfurousAsh,
            Reagent.SpidersSilk);

        public MeteorSwarmSpell(Mobile caster, Item scroll, Item item)
            : base(caster, scroll, m_Info)
        {
            Item = item;
        }

        public MeteorSwarmSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override int GetMana()
        {
            if (Item != null)
            {
                return 0;
            }

            return base.GetMana();
        }

        public override SpellCircle Circle => SpellCircle.Seventh;
        public override bool DelayedDamage => true;

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this, Item);
        }

        public void Target(IPoint3D p, Item item)
        {
            if (!Caster.CanSee(p))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (SpellHelper.CheckTown(p, Caster) && (item != null || CheckSequence()))
            {
                if (item != null)
                {
                    if (item is MaskOfKhalAnkur mask)
                    {
                        mask.Charges--;
                    }

                    if (item is PendantOfKhalAnkur pendant)
                    {
                        pendant.Charges--;
                    }
                }

                SpellHelper.Turn(Caster, p);

                if (p is Item pItem)
                {
                    p = pItem.GetWorldLocation();
                }

                List<IDamageable> targets = new List<IDamageable>();

                foreach (var target in AcquireIndirectTargets(p, 2))
                {
                    targets.Add(target);
                }

                int count = Math.Max(1, targets.Count);

                Effects.PlaySound(p, Caster.Map, 0x160);

                for (var index = 0; index < targets.Count; index++)
                {
                    IDamageable id = targets[index];
                    Mobile m = id as Mobile;

                    double damage = GetNewAosDamage(51, 1, 5, id is PlayerMobile, id);

                    if (count > 2)
                    {
                        damage = (damage * 2) / count;
                    }

                    IDamageable source = Caster;
                    IDamageable target = id;

                    if (SpellHelper.CheckReflect(this, ref source, ref target))
                    {
                        Timer.DelayCall(TimeSpan.FromSeconds(.5),
                            () =>
                            {
                                source.MovingParticles(target, item != null ? 0xA1ED : 0x36D4, 7, 0, false, true, 9501,
                                    1, 0, 0x100);
                            });
                    }

                    if (m != null)
                    {
                        damage *= GetDamageScalar(m);
                    }

                    Caster.DoHarmful(id);
                    SpellHelper.Damage(this, target, damage, 0, 100, 0, 0, 0);

                    Caster.MovingParticles(id, item != null ? 0xA1ED : 0x36D4, 7, 0, false, true, 9501, 1, 0, 0x100);
                }

                ColUtility.Free(targets);
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private readonly MeteorSwarmSpell m_Owner;
            private readonly Item m_Item;

            public InternalTarget(MeteorSwarmSpell owner, Item item)
                : base(10, true, TargetFlags.None)
            {
                m_Owner = owner;
                m_Item = item;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is IPoint3D p)
                {
                    m_Owner.Target(p, m_Item);
                }
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}
