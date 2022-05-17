using Server.Spells.Fourth;
using Server.Targeting;

namespace Server.Spells.Sixth
{
    public class MassCurseSpell : MagerySpell
    {
        private static readonly SpellInfo m_Info = new SpellInfo(
            "Mass Curse", "Vas Des Sanct",
            218,
            9031,
            false,
            Reagent.Garlic,
            Reagent.Nightshade,
            Reagent.MandrakeRoot,
            Reagent.SulfurousAsh);
        public MassCurseSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Sixth;

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        public void Target(IPoint3D p)
        {
            if (!Caster.CanSee(p))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                SpellHelper.Turn(Caster, p);
                SpellHelper.GetSurfaceTop(ref p);

                foreach (IDamageable target in AcquireIndirectTargets(p, 2))
                {
                    if (target is Mobile m)
                    {
                        CurseSpell.DoCurse(Caster, m, true);
                    }
                }
            }

            FinishSequence();
        }

        private class InternalTarget : Target
        {
            private readonly MassCurseSpell m_Owner;
            public InternalTarget(MassCurseSpell owner)
                : base(10, true, TargetFlags.None)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is IPoint3D p)
                {
                    m_Owner.Target(p);
                }
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}
