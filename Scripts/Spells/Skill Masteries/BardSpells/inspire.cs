/*Party Hit chance increase by up to 15%, Damage increase by up to 40%, 
SDI increased by up to 15% (PvP Cap 15)(Provocation Based)*/

namespace Server.Spells.SkillMasteries
{
    public class InspireSpell : BardSpell
    {
        private static readonly SpellInfo m_Info = new SpellInfo(
                "Inspire", "Unus Por",
                -1,
                9002
            );

        public override double RequiredSkill => 90;
        public override double UpKeep => 4;
        public override int RequiredMana => 16;
        public override SkillName CastSkill => SkillName.Provocation;
        public override bool PartyEffects => true;

        private int m_PropertyBonus;
        private int m_DamageBonus;
        private int m_DamageModifier;

        public InspireSpell(Mobile caster, Item scroll) : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            if (GetSpell(Caster, GetType()) is BardSpell spell)
            {
                spell.Expire();
                Caster.SendLocalizedMessage(1115774); //You halt your spellsong.
            }
            else if (CheckSequence())
            {
                m_PropertyBonus = (int)((BaseSkillBonus * 2) + CollectiveBonus);
                m_DamageBonus = (int)((BaseSkillBonus * 5) + (CollectiveBonus * 3));
                m_DamageModifier = (int)((BaseSkillBonus + 1) + CollectiveBonus);

                UpdateParty();
                BeginTimer();
            }

            FinishSequence();
        }

        public override void AddPartyEffects(Mobile m)
        {
            m.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
            m.SendLocalizedMessage(1115736); // You feel inspired by the bard's spellsong.

            string args = $"{m_PropertyBonus}\t{m_PropertyBonus}\t{m_DamageBonus}\t{m_DamageModifier}";
            BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.Inspire, 1115683, 1151951, args));
        }

        public override void RemovePartyEffects(Mobile m)
        {
            BuffInfo.RemoveBuff(m, BuffIcon.Inspire);
        }

        public override void EndEffects()
        {
            if (PartyList != null)
            {
                for (var index = 0; index < PartyList.Count; index++)
                {
                    Mobile m = PartyList[index];

                    RemovePartyEffects(m);
                }
            }

            RemovePartyEffects(Caster);
        }

        /// <summary>
        /// Called in AOS.cs - Hit Chance/Spell Damage Bonus
        /// </summary>
        /// <returns>HCI/SDI Bonus</returns>
        public override int PropertyBonus()
        {
            return m_PropertyBonus;
        }

        /// <summary>
        /// Called in AOS.cs - Weapon Damage Bonus
        /// </summary>
        /// <returns>DamInc Bonus</returns>
        public override int DamageBonus()
        {
            return m_DamageBonus;
        }

        public void DoDamage(ref int damageTaken)
        {
            damageTaken += AOS.Scale(damageTaken, m_DamageModifier);
        }
    }
}
