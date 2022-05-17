#region References
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Spells.Bushido;
using Server.Spells.Chivalry;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Mysticism;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;
using Server.Spells.Second;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Reflection;
#endregion

namespace Server.Spells
{
    public abstract class Spell : ISpell
    {
        private readonly Mobile m_Caster;
        private readonly Item m_Scroll;
        private readonly SpellInfo m_Info;
        private SpellState m_State;
        private long m_CastTime;
        private IDamageable m_InstantTarget;

        public int ID => SpellRegistry.GetRegistryNumber(this);

        public SpellState State { get => m_State; set => m_State = value; }

        public Mobile Caster => m_Caster;
        public SpellInfo Info => m_Info;
        public string Name => m_Info.Name;
        public string Mantra => m_Info.Mantra;
        public Type[] Reagents => m_Info.Reagents;
        public Item Scroll => m_Scroll;
        public long CastTime => m_CastTime;

        public IDamageable InstantTarget { get => m_InstantTarget; set => m_InstantTarget = value; }

        public bool Disturbed { get; set; }

        private static TimeSpan AnimateDelay = TimeSpan.FromSeconds(1.5);

        public virtual SkillName CastSkill => SkillName.Magery;
        public virtual SkillName DamageSkill => SkillName.EvalInt;

        public virtual bool RevealOnCast => true;
        public virtual bool ClearHandsOnCast => true;
        public virtual bool ShowHandMovement => true;

        public virtual bool CheckManaBeforeCast => true;

        public virtual bool DelayedDamage => false;
        public virtual Type[] DelayDamageFamily => null;
        // DelayDamageFamily can define spells so they don't stack, even though they are different spells
        // Right now, magic arrow and nether bolt are the only ones that have this functionality

        public virtual bool DelayedDamageStacking => true;
        //In reality, it's ANY delayed Damage spell that can't stack, but, only 
        //Expo & Magic Arrow have enough delay and a short enough cast time to bring up 
        //the possibility of stacking 'em.  Note that a MA & an Explosion will stack, but
        //of course, two MA's won't.

        public virtual DamageType SpellDamageType => DamageType.Spell;

        private static readonly Dictionary<Type, DelayedDamageContextWrapper> m_ContextTable =
            new Dictionary<Type, DelayedDamageContextWrapper>();

        private class DelayedDamageContextWrapper
        {
            private readonly Dictionary<IDamageable, Timer> m_Contexts = new Dictionary<IDamageable, Timer>();

            public void Add(IDamageable d, Timer t)
            {
                Timer oldTimer;

                if (m_Contexts.TryGetValue(d, out oldTimer))
                {
                    oldTimer.Stop();
                    m_Contexts.Remove(d);
                }

                m_Contexts.Add(d, t);
            }

            public bool Contains(IDamageable d)
            {
                return m_Contexts.ContainsKey(d);
            }

            public void Remove(IDamageable d)
            {
                m_Contexts.Remove(d);
            }
        }

        public void StartDelayedDamageContext(IDamageable d, Timer t)
        {
            if (DelayedDamageStacking)
            {
                return; //Sanity
            }

            DelayedDamageContextWrapper contexts;

            if (!m_ContextTable.TryGetValue(GetType(), out contexts))
            {
                contexts = new DelayedDamageContextWrapper();
                Type type = GetType();

                m_ContextTable.Add(type, contexts);

                if (DelayDamageFamily != null)
                {
                    for (var index = 0; index < DelayDamageFamily.Length; index++)
                    {
                        Type familyType = DelayDamageFamily[index];

                        m_ContextTable.Add(familyType, contexts);
                    }
                }
            }

            contexts.Add(d, t);
        }

        public bool HasDelayContext(IDamageable d)
        {
            if (DelayedDamageStacking)
            {
                return false; //Sanity
            }

            Type t = GetType();

            if (m_ContextTable.ContainsKey(t))
            {
                return m_ContextTable[t].Contains(d);
            }

            return false;
        }

        public void RemoveDelayedDamageContext(IDamageable d)
        {
            DelayedDamageContextWrapper contexts;
            Type type = GetType();

            if (!m_ContextTable.TryGetValue(type, out contexts))
            {
                return;
            }

            contexts.Remove(d);

            if (DelayDamageFamily != null)
            {
                for (var index = 0; index < DelayDamageFamily.Length; index++)
                {
                    Type t = DelayDamageFamily[index];

                    if (m_ContextTable.TryGetValue(t, out contexts))
                    {
                        contexts.Remove(d);
                    }
                }
            }
        }

        public void HarmfulSpell(IDamageable d)
        {
            if (d is BaseCreature creature)
            {
                creature.OnHarmfulSpell(m_Caster);
            }
            else if (d is IDamageableItem item)
            {
                item.OnHarmfulSpell(m_Caster);
            }

            NegativeAttributes.OnCombatAction(Caster);

            if (d is Mobile mobile)
            {
                if (mobile != m_Caster)
                    NegativeAttributes.OnCombatAction(mobile);

                EvilOmenSpell.TryEndEffect(mobile);
            }
        }

        public Spell(Mobile caster, Item scroll, SpellInfo info)
        {
            m_Caster = caster;
            m_Scroll = scroll;
            m_Info = info;
        }

        public virtual int GetNewAosDamage(int bonus, int dice, int sides, IDamageable singleTarget)
        {
            if (singleTarget != null)
            {
                return GetNewAosDamage(bonus, dice, sides, (Caster.Player && singleTarget is PlayerMobile), GetDamageScalar(singleTarget as Mobile), singleTarget);
            }

            return GetNewAosDamage(bonus, dice, sides, false, null);
        }

        public virtual int GetNewAosDamage(int bonus, int dice, int sides, bool playerVsPlayer, IDamageable damageable)
        {
            return GetNewAosDamage(bonus, dice, sides, playerVsPlayer, 1.0, damageable);
        }

        public virtual int GetNewAosDamage(int bonus, int dice, int sides, bool playerVsPlayer, double scalar, IDamageable damageable)
        {
            Mobile target = damageable as Mobile;

            int damage = Utility.Dice(dice, sides, bonus) * 100;

            int inscribeSkill = GetInscribeFixed(m_Caster);
            int scribeBonus = inscribeSkill >= 1000 ? 10 : inscribeSkill / 200;

            int damageBonus = scribeBonus +
                              (Caster.Int / 10) +
                              SpellHelper.GetSpellDamageBonus(m_Caster, target, CastSkill, playerVsPlayer);

            int evalSkill = GetDamageFixed(m_Caster);
            int evalScale = 30 + ((9 * evalSkill) / 100);

            damage = AOS.Scale(damage, evalScale);
            damage = AOS.Scale(damage, 100 + damageBonus);
            damage = AOS.Scale(damage, (int)(scalar * 100));

            return damage / 100;
        }

        public virtual bool IsCasting => m_State == SpellState.Casting;

        public virtual void OnCasterHurt()
        {
            CheckCasterDisruption();
        }

        public virtual void CheckCasterDisruption(bool checkElem = false, int phys = 0, int fire = 0, int cold = 0, int pois = 0, int nrgy = 0)
        {
            if (!Caster.Player || Caster.AccessLevel > AccessLevel.Player)
            {
                return;
            }

            if (IsCasting)
            {
                object o = ProtectionSpell.Registry[m_Caster];

                bool disturb = !(o is double d && d > Utility.RandomDouble() * 100.0);

                #region Stygian Abyss
                int focus = SAAbsorptionAttributes.GetValue(Caster, SAAbsorptionAttribute.CastingFocus);

                if (BaseFishPie.IsUnderEffects(m_Caster, FishPieEffect.CastFocus))
                    focus += 2;

                if (focus > 12)
                    focus = 12;

                focus += m_Caster.Skills[SkillName.Inscribe].Value >= 50 ? GetInscribeFixed(m_Caster) / 200 : 0;

                if (focus > 0 && focus > Utility.Random(100))
                {
                    disturb = false;
                    Caster.SendLocalizedMessage(1113690); // You regain your focus and continue casting the spell.
                }
                else if (checkElem)
                {
                    int res = 0;

                    if (phys == 100)
                        res = Math.Min(40, SAAbsorptionAttributes.GetValue(m_Caster, SAAbsorptionAttribute.ResonanceKinetic));

                    else if (fire == 100)
                        res = Math.Min(40, SAAbsorptionAttributes.GetValue(m_Caster, SAAbsorptionAttribute.ResonanceFire));

                    else if (cold == 100)
                        res = Math.Min(40, SAAbsorptionAttributes.GetValue(m_Caster, SAAbsorptionAttribute.ResonanceCold));

                    else if (pois == 100)
                        res = Math.Min(40, SAAbsorptionAttributes.GetValue(m_Caster, SAAbsorptionAttribute.ResonancePoison));

                    else if (nrgy == 100)
                        res = Math.Min(40, SAAbsorptionAttributes.GetValue(m_Caster, SAAbsorptionAttribute.ResonanceEnergy));

                    if (res > Utility.Random(100))
                        disturb = false;
                }
                #endregion

                if (disturb)
                {
                    Disturb(DisturbType.Hurt, false, true);
                }
            }
        }

        public virtual void OnCasterKilled()
        {
            Disturb(DisturbType.Kill);
        }

        public virtual void OnConnectionChanged()
        {
            FinishSequence();
        }

        /// <summary>
        /// Pre-ML code where mobile can change directions, but doesn't move
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
		public virtual bool OnCasterMoving(Direction d)
        {
            if (IsCasting && BlocksMovement && (!(m_Caster is BaseCreature) || ((BaseCreature)m_Caster).FreezeOnCast))
            {
                m_Caster.SendLocalizedMessage(500111); // You are frozen and can not move.
                return false;
            }

            return true;
        }

        /// <summary>
        /// Post ML code where player is frozen in place while casting.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        public virtual bool CheckMovement(Mobile caster)
        {
            if (IsCasting && BlocksMovement && (!(m_Caster is BaseCreature) || ((BaseCreature)m_Caster).FreezeOnCast))
            {
                return false;
            }

            return true;
        }

        public virtual bool OnCasterEquiping(Item item)
        {
            if (IsCasting)
            {
                if ((item.Layer == Layer.OneHanded || item.Layer == Layer.TwoHanded) && item.AllowEquipedCast(Caster))
                {
                    return true;
                }

                Disturb(DisturbType.EquipRequest);
            }

            return true;
        }

        public virtual bool OnCasterUsingObject(object o)
        {
            if (m_State == SpellState.Sequencing)
            {
                Disturb(DisturbType.UseRequest);
            }

            return true;
        }

        public virtual bool OnCastInTown(Region r)
        {
            return m_Info.AllowTown;
        }

        public virtual bool ConsumeReagents()
        {
            if ((m_Scroll != null && !(m_Scroll is SpellStone)) || !m_Caster.Player)
            {
                return true;
            }

            if (AosAttributes.GetValue(m_Caster, AosAttribute.LowerRegCost) > Utility.Random(100))
            {
                return true;
            }

            Container pack = m_Caster.Backpack;

            if (pack == null)
            {
                return false;
            }

            if (pack.ConsumeTotal(m_Info.Reagents, m_Info.Amounts) == -1)
            {
                return true;
            }

            return false;
        }

        public virtual double GetInscribeSkill(Mobile m)
        {
            // There is no chance to gain
            // m.CheckSkill( SkillName.Inscribe, 0.0, 120.0 );
            return m.Skills[SkillName.Inscribe].Value;
        }

        public virtual int GetInscribeFixed(Mobile m)
        {
            // There is no chance to gain
            // m.CheckSkill( SkillName.Inscribe, 0.0, 120.0 );
            return m.Skills[SkillName.Inscribe].Fixed;
        }

        public virtual int GetDamageFixed(Mobile m)
        {
            //m.CheckSkill( DamageSkill, 0.0, m.Skills[DamageSkill].Cap );
            return m.Skills[DamageSkill].Fixed;
        }

        public virtual double GetDamageSkill(Mobile m)
        {
            return m.Skills[DamageSkill].Value;
        }

        public virtual double GetResistSkill(Mobile m)
        {
            return m.Skills[SkillName.MagicResist].Value - EvilOmenSpell.GetResistMalus(m);
        }

        public virtual double GetDamageScalar(Mobile target)
        {
            double scalar = 1.0;

            if (target == null)
                return scalar;

            if (target is BaseCreature targetCreature)
            {
                targetCreature.AlterDamageScalarFrom(m_Caster, ref scalar);
            }

            if (m_Caster is BaseCreature creature)
            {
                creature.AlterDamageScalarTo(target, ref scalar);
            }

            scalar *= GetSlayerDamageScalar(target);

            target.Region.SpellDamageScalar(m_Caster, target, ref scalar);

            if (Evasion.CheckSpellEvasion(target)) //Only single target spells an be evaded
            {
                scalar = 0;
            }

            return scalar;
        }

        public virtual double GetSlayerDamageScalar(Mobile defender)
        {
            Spellbook atkBook = Spellbook.FindEquippedSpellbook(m_Caster);

            double scalar = 1.0;
            if (atkBook != null)
            {
                SlayerEntry atkSlayer = SlayerGroup.GetEntryByName(atkBook.Slayer);
                SlayerEntry atkSlayer2 = SlayerGroup.GetEntryByName(atkBook.Slayer2);

                if (atkSlayer == null && atkSlayer2 == null)
                {
                    atkSlayer = SlayerGroup.GetEntryByName(SlayerSocket.GetSlayer(atkBook));
                }

                if (atkSlayer != null && atkSlayer.Slays(defender) || atkSlayer2 != null && atkSlayer2.Slays(defender))
                {
                    defender.FixedEffect(0x37B9, 10, 5);

                    bool isSuper = false;

                    if (atkSlayer != null && atkSlayer == atkSlayer.Group.Super && atkSlayer.Group != SlayerGroup.Groups[6]) //Fey Slayers give 300% damage
                        isSuper = true;
                    else if (atkSlayer2 != null && atkSlayer2 == atkSlayer2.Group.Super && atkSlayer2.Group != SlayerGroup.Groups[6]) 
                        isSuper = true;

                    scalar = isSuper ? 2.0 : 3.0;
                }


                TransformContext context = TransformationSpellHelper.GetContext(defender);

                if ((atkBook.Slayer == SlayerName.Silver || atkBook.Slayer2 == SlayerName.Silver) && context != null && context.Type != typeof(HorrificBeastSpell))
                    scalar += .25; // Every necromancer transformation other than horrific beast take an additional 25% damage

                if (scalar != 1.0)
                    return scalar;
            }

            ISlayer defISlayer = Spellbook.FindEquippedSpellbook(defender);

            if (defISlayer == null)
            {
                defISlayer = defender.Weapon as ISlayer;
            }

            if (defISlayer != null)
            {
                SlayerEntry defSlayer = SlayerGroup.GetEntryByName(defISlayer.Slayer);
                SlayerEntry defSlayer2 = SlayerGroup.GetEntryByName(defISlayer.Slayer2);

                if (defSlayer != null && defSlayer.Group.OppositionSuperSlays(m_Caster) ||
                    defSlayer2 != null && defSlayer2.Group.OppositionSuperSlays(m_Caster))
                {
                    scalar = 2.0;
                }
            }

            return scalar;
        }

        public virtual void DoFizzle()
        {
            m_Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502632); // The spell fizzles.

            if (m_Caster.Player)
            {
                m_Caster.FixedParticles(0x3735, 1, 30, 9503, EffectLayer.Waist);
                m_Caster.PlaySound(0x5C);
            }
        }

        private AnimTimer m_AnimTimer;

        public void Disturb(DisturbType type)
        {
            Disturb(type, true, false);
        }

        public virtual bool CheckDisturb(DisturbType type, bool firstCircle, bool resistable)
        {
            if (resistable && m_Scroll is BaseWand)
            {
                return false;
            }

            return true;
        }

        public void Disturb(DisturbType type, bool firstCircle, bool resistable)
        {
            if (!CheckDisturb(type, firstCircle, resistable))
            {
                return;
            }

            if (m_State == SpellState.Casting)
            {
                m_State = SpellState.None;
                m_Caster.Spell = null;
                Caster.Delta(MobileDelta.Flags);

                Disturbed = true;
                OnDisturb(type, true);

                m_AnimTimer?.Stop();

                if (m_Caster.Player && type == DisturbType.Hurt)
                {
                    DoHurtFizzle();
                }

                m_Caster.NextSpellTime = Core.TickCount + (int)GetDisturbRecovery().TotalMilliseconds;
            }
            else if (m_State == SpellState.Sequencing)
            {
                m_State = SpellState.None;
                m_Caster.Spell = null;
                Caster.Delta(MobileDelta.Flags);

                OnDisturb(type, false);

                Target.Cancel(m_Caster);

                if (m_Caster.Player && type == DisturbType.Hurt)
                {
                    DoHurtFizzle();
                }
            }
        }

        public virtual void DoHurtFizzle()
        {
            m_Caster.FixedEffect(0x3735, 6, 30);
            m_Caster.PlaySound(0x5C);
        }

        public virtual void OnDisturb(DisturbType type, bool message)
        {
            if (message)
            {
                m_Caster.SendLocalizedMessage(500641); // Your concentration is disturbed, thus ruining thy spell.
            }
        }

        public virtual bool CheckCast()
        {
            #region High Seas
            if (Multis.BaseBoat.IsDriving(m_Caster) && m_Caster.AccessLevel == AccessLevel.Player)
            {
                m_Caster.SendLocalizedMessage(1049616); // You are too busy to do that at the moment.
                return false;
            }
            #endregion

            return true;
        }

        public virtual void SayMantra()
        {
            if (m_Scroll is SpellStone)
            {
                return;
            }

            if (m_Scroll is BaseWand)
            {
                return;
            }

            if (!string.IsNullOrEmpty(m_Info.Mantra) && (m_Caster.Player || m_Caster is BaseCreature creature && creature.ShowSpellMantra))
            {
                m_Caster.PublicOverheadMessage(MessageType.Spell, m_Caster.SpeechHue, true, m_Info.Mantra, false);
            }
        }

        public virtual bool BlockedByHorrificBeast
        {
            get
            {
                if (TransformationSpellHelper.UnderTransformation(Caster, typeof(HorrificBeastSpell)) &&
                    SpellHelper.HasSpellFocus(Caster, CastSkill))
                    return false;

                return true;
            }
        }

        public virtual bool BlockedByAnimalForm => true;
        public virtual bool BlocksMovement => true;

        public virtual bool CheckNextSpellTime => !(m_Scroll is BaseWand);

        public virtual bool Cast()
        {
            if (m_Caster.Spell is Spell spell && spell.State == SpellState.Sequencing)
            {
                spell.Disturb(DisturbType.NewCast);
            }

            if (!m_Caster.CheckAlive())
            {
                return false;
            }

            if (m_Caster is PlayerMobile pm && pm.Peaced)
            {
                pm.SendLocalizedMessage(1072060); // You cannot cast a spell while calmed.
            }
            else if (m_Scroll is BaseWand && m_Caster.Spell != null && m_Caster.Spell.IsCasting)
            {
                m_Caster.SendLocalizedMessage(502643); // You can not cast a spell while frozen.
            }
            else if (SkillHandlers.SpiritSpeak.IsInSpiritSpeak(m_Caster) || (m_Caster.Spell != null && m_Caster.Spell.IsCasting))
            {
                m_Caster.SendLocalizedMessage(502642); // You are already casting a spell.
            }
            else if (BlockedByHorrificBeast && TransformationSpellHelper.UnderTransformation(m_Caster, typeof(HorrificBeastSpell)) ||
                     (BlockedByAnimalForm && AnimalForm.UnderTransformation(m_Caster)))
            {
                m_Caster.SendLocalizedMessage(1061091); // You cannot cast that spell in this form.
            }
            else if (!(m_Scroll is BaseWand) && (m_Caster.Paralyzed || m_Caster.Frozen))
            {
                m_Caster.SendLocalizedMessage(502643); // You can not cast a spell while frozen.
            }
            else if (CheckNextSpellTime && Core.TickCount - m_Caster.NextSpellTime < 0)
            {
                m_Caster.SendLocalizedMessage(502644); // You have not yet recovered from casting a spell.
            }
            else if (!CheckManaBeforeCast || m_Caster.Mana >= ScaleMana(GetMana()))
            {
                #region Stygian Abyss
                if (m_Caster.Race == Race.Gargoyle && m_Caster.Flying)
                {
                    if (BaseMount.OnFlightPath(m_Caster))
                    {
                        if (m_Caster.IsPlayer())
                        {
                            m_Caster.SendLocalizedMessage(1113750); // You may not cast spells while flying over such precarious terrain.
                            return false;
                        }

                        m_Caster.SendMessage("Your staff level allows you to cast while flying over precarious terrain.");
                    }
                }
                #endregion

                if (m_Caster.Spell == null && m_Caster.CheckSpellCast(this) && CheckCast() &&
                    m_Caster.Region.OnBeginSpellCast(m_Caster, this))
                {
                    m_State = SpellState.Casting;
                    m_Caster.Spell = this;

                    Caster.Delta(MobileDelta.Flags);

                    if (!(m_Scroll is BaseWand) && RevealOnCast)
                    {
                        m_Caster.RevealingAction();
                    }

                    SayMantra();

                    TimeSpan castDelay = GetCastDelay();

                    if (ShowHandMovement && !(m_Scroll is SpellStone) && (m_Caster.Body.IsHuman || (m_Caster.Player && m_Caster.Body.IsMonster)))
                    {
                        int count = (int)Math.Ceiling(castDelay.TotalSeconds / AnimateDelay.TotalSeconds);

                        if (count != 0)
                        {
                            m_AnimTimer = new AnimTimer(this, count);
                            m_AnimTimer.Start();
                        }

                        if (m_Info.LeftHandEffect > 0)
                        {
                            Caster.FixedParticles(0, 10, 5, m_Info.LeftHandEffect, EffectLayer.LeftHand);
                        }

                        if (m_Info.RightHandEffect > 0)
                        {
                            Caster.FixedParticles(0, 10, 5, m_Info.RightHandEffect, EffectLayer.RightHand);
                        }
                    }

                    if (ClearHandsOnCast)
                    {
                        m_Caster.ClearHands();
                    }

                    WeaponAbility.ClearCurrentAbility(m_Caster);
                    m_CastTime = Core.TickCount + (long)castDelay.TotalMilliseconds;

                    CastTimer.AddTimer(this);
                    OnBeginCast();

                    return true;
                }

                return false;
            }
            else
            {
                m_Caster.LocalOverheadMessage(MessageType.Regular, 0x22, 502625, ScaleMana(GetMana()).ToString()); // Insufficient mana. You must have at least ~1_MANA_REQUIREMENT~ Mana to use this spell.
            }

            return false;
        }

        public abstract void OnCast();

        private void SequenceSpell()
        {
            m_State = SpellState.Sequencing;
            m_Caster.OnSpellCast(this);

            Caster.Delta(MobileDelta.Flags);

            m_Caster.Region?.OnSpellCast(m_Caster, this);

            m_Caster.NextSpellTime = Core.TickCount + (int)GetCastRecovery().TotalMilliseconds;

            Target originalTarget = m_Caster.Target;

            if (InstantTarget == null || !OnCastInstantTarget())
            {
                OnCast();
            }

            if (m_Caster.Player && m_Caster.Target != originalTarget && Caster.Target != null)
            {
                m_Caster.Target.BeginTimeout(m_Caster, TimeSpan.FromSeconds(30.0));
            }
        }

        #region Enhanced Client
        public bool OnCastInstantTarget()
        {
            if (InstantTarget == null)
                return false;

            Type spellType = GetType();

            Type[] types = spellType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            Type targetType = null;

            for (var index = 0; index < types.Length; index++)
            {
                var t = types[index];

                if (t.IsSubclassOf(typeof(Target)))
                {
                    targetType = t;
                    break;
                }
            }

            if (targetType != null)
            {
                Target t = null;

                try
                {
                    t = Activator.CreateInstance(targetType, this) as Target;
                }
                catch
                {
                    LogBadConstructorForInstantTarget();
                }

                if (t != null)
                {
                    t.Invoke(Caster, InstantTarget);
                    return true;
                }
            }

            return false;
        }
        #endregion

        public virtual void OnBeginCast()
        {
            SendCastEffect();
        }

        public virtual void SendCastEffect()
        { }

        public virtual void GetCastSkills(out double min, out double max)
        {
            min = max = 0; //Intended but not required for overriding.
        }

        public virtual bool CheckFizzle()
        {
            if (m_Scroll is BaseWand)
            {
                return true;
            }

            double minSkill, maxSkill;

            GetCastSkills(out minSkill, out maxSkill);

            if (DamageSkill != CastSkill && DamageSkill != SkillName.Imbuing)
            {
                Caster.CheckSkill(DamageSkill, 0.0, Caster.Skills[DamageSkill].Cap);
            }

            bool skillCheck = Caster.CheckSkill(CastSkill, minSkill, maxSkill);

            return Caster is BaseCreature || skillCheck;
        }

        public abstract int GetMana();

        public virtual int ScaleMana(int mana)
        {
            double scalar = 1.0;

            if (ManaPhasingOrb.IsInManaPhase(Caster))
            {
                ManaPhasingOrb.RemoveFromTable(Caster);
                return 0;
            }

            if (!MindRotSpell.GetMindRotScalar(Caster, ref scalar))
            {
                scalar = 1.0;
            }

            if (PurgeMagicSpell.IsUnderCurseEffects(Caster))
            {
                scalar += .5;
            }

            int lmc = AosAttributes.GetValue(m_Caster, AosAttribute.LowerManaCost);

            scalar -= (double)lmc / 100;

            return (int)(mana * scalar);
        }

        public virtual TimeSpan GetDisturbRecovery()
        {
            return TimeSpan.Zero;
        }

        public virtual int CastRecoveryBase => 6;
        public virtual int CastRecoveryFastScalar => 1;
        public virtual int CastRecoveryPerSecond => 4;
        public virtual int CastRecoveryMinimum => 0;

        public virtual TimeSpan GetCastRecovery()
        {
            int fcr = AosAttributes.GetValue(m_Caster, AosAttribute.CastRecovery);

            int fcrDelay = -(CastRecoveryFastScalar * fcr);

            int delay = CastRecoveryBase + fcrDelay;

            if (delay < CastRecoveryMinimum)
            {
                delay = CastRecoveryMinimum;
            }

            return TimeSpan.FromSeconds((double)delay / CastRecoveryPerSecond);
        }

        public abstract TimeSpan CastDelayBase { get; }

        public virtual double CastDelayFastScalar => 1;
        public virtual double CastDelaySecondsPerTick => 0.25;
        public virtual TimeSpan CastDelayMinimum => TimeSpan.FromSeconds(0.25);

        public virtual TimeSpan GetCastDelay()
        {
            if (m_Scroll is SpellStone)
            {
                return TimeSpan.Zero;
            }

            if (m_Scroll is BaseWand)
            {
                return CastDelayBase; // TODO: Should FC apply to wands?
            }

            // Faster casting cap of 2 (if not using the protection spell) 
            // Faster casting cap of 0 (if using the protection spell) 
            // Paladin spells are subject to a faster casting cap of 4 
            // Paladins with magery of 70.0 or above are subject to a faster casting cap of 2 
            int fcMax = 4;

            if (CastSkill == SkillName.Magery || CastSkill == SkillName.Necromancy || CastSkill == SkillName.Mysticism ||
                (CastSkill == SkillName.Chivalry && (m_Caster.Skills[SkillName.Magery].Value >= 70.0 || m_Caster.Skills[SkillName.Mysticism].Value >= 70.0)))
            {
                fcMax = 2;
            }

            int fc = AosAttributes.GetValue(m_Caster, AosAttribute.CastSpeed);

            if (fc > fcMax)
            {
                fc = fcMax;
            }

            if (ProtectionSpell.Registry.ContainsKey(m_Caster) || EodonianPotion.IsUnderEffects(m_Caster, PotionEffect.Urali))
            {
                fc = Math.Min(fcMax - 2, fc - 2);
            }

            TimeSpan baseDelay = CastDelayBase;

            TimeSpan fcDelay = TimeSpan.FromSeconds(-(CastDelayFastScalar * fc * CastDelaySecondsPerTick));

            //int delay = CastDelayBase + circleDelay + fcDelay;
            TimeSpan delay = baseDelay + fcDelay;

            if (delay < CastDelayMinimum)
            {
                delay = CastDelayMinimum;
            }

            if (DreadHorn.IsUnderInfluence(m_Caster))
            {
                delay.Add(delay);
            }

            //return TimeSpan.FromSeconds( (double)delay / CastDelayPerSecond );
            return delay;
        }

        public virtual void FinishSequence()
        {
            SpellState oldState = m_State;

            m_State = SpellState.None;

            if (oldState == SpellState.Casting)
            {
                Caster.Delta(MobileDelta.Flags);
            }

            if (m_Caster.Spell == this)
            {
                m_Caster.Spell = null;
            }
        }

        public virtual int ComputeKarmaAward()
        {
            return 0;
        }

        public virtual bool CheckSequence()
        {
            int mana = ScaleMana(GetMana());

            if (m_Caster.Deleted || !m_Caster.Alive || m_Caster.Spell != this || m_State != SpellState.Sequencing)
            {
                DoFizzle();
            }
            else if (m_Scroll != null && !(m_Scroll is Runebook) &&
                     (m_Scroll.Amount <= 0 || m_Scroll.Deleted || m_Scroll.RootParent != m_Caster ||
                      m_Scroll is BaseWand wand && (wand.Charges <= 0 || wand.Parent != m_Caster)))
            {
                DoFizzle();
            }
            else if (!ConsumeReagents())
            {
                m_Caster.LocalOverheadMessage(MessageType.Regular, 0x22, 502630); // More reagents are needed for this spell.
            }
            else if (m_Caster.Mana < mana)
            {
                m_Caster.LocalOverheadMessage(MessageType.Regular, 0x22, 502625, mana.ToString()); // Insufficient mana for this spell.
            }
            else if (m_Caster.Frozen || m_Caster.Paralyzed)
            {
                m_Caster.SendLocalizedMessage(502646); // You cannot cast a spell while frozen.
                DoFizzle();
            }
            else if (m_Caster is PlayerMobile pm && pm.PeacedUntil > DateTime.UtcNow)
            {
                pm.SendLocalizedMessage(1072060); // You cannot cast a spell while calmed.
                DoFizzle();
            }
            else if (CheckFizzle())
            {
                m_Caster.Mana -= mana;

                CrazedMage.ManaCorruption(m_Caster, GetMana());

                if (m_Scroll is SpellStone stone)
                {
                    stone.Use(m_Caster);
                }

                if (m_Scroll is SpellScroll)
                {
                    m_Scroll.Consume();
                }

                else if (m_Scroll is BaseWand baseWand)
                {
                    baseWand.ConsumeCharge(m_Caster);
                    m_Caster.RevealingAction();
                }

                if (m_Scroll is BaseWand)
                {
                    bool m = m_Scroll.Movable;

                    m_Scroll.Movable = false;

                    if (ClearHandsOnCast)
                    {
                        m_Caster.ClearHands();
                    }

                    m_Scroll.Movable = m;
                }
                else
                {
                    if (ClearHandsOnCast)
                    {
                        m_Caster.ClearHands();
                    }
                }

                int karma = ComputeKarmaAward();

                if (karma != 0)
                {
                    Titles.AwardKarma(Caster, karma, true);
                }

                if (TransformationSpellHelper.UnderTransformation(m_Caster, typeof(VampiricEmbraceSpell)))
                {
                    bool garlic = false;

                    for (int i = 0; !garlic && i < m_Info.Reagents.Length; ++i)
                    {
                        garlic = (m_Info.Reagents[i] == Reagent.Garlic);
                    }

                    if (garlic)
                    {
                        m_Caster.SendLocalizedMessage(1061651); // The garlic burns you!
                        AOS.Damage(m_Caster, Utility.RandomMinMax(17, 23), 100, 0, 0, 0, 0);
                    }
                }

                return true;
            }
            else
            {
                DoFizzle();
            }

            return false;
        }

        public bool CheckBSequence(Mobile target)
        {
            return CheckBSequence(target, false);
        }

        public bool CheckBSequence(Mobile target, bool allowDead)
        {
            if (!target.Alive && !allowDead)
            {
                m_Caster.SendLocalizedMessage(501857); // This spell won't work on that!
                return false;
            }

            if (Caster.CanBeBeneficial(target, true, allowDead) && CheckSequence())
            {
                if (ValidateBeneficial(target))
                {
                    Caster.DoBeneficial(target);
                }

                return true;
            }

            return false;
        }

        public bool CheckHSequence(IDamageable target)
        {
            if (!target.Alive || target is IDamageableItem item && !item.CanDamage)
            {
                m_Caster.SendLocalizedMessage(501857); // This spell won't work on that!
                return false;
            }

            if (Caster.CanBeHarmful(target) && CheckSequence())
            {
                Caster.DoHarmful(target);
                return true;
            }

            return false;
        }

        public bool ValidateBeneficial(Mobile target)
        {
            if (target == null)
                return true;

            if (this is HealSpell || this is GreaterHealSpell || this is CloseWoundsSpell)
            {
                return target.Hits < target.HitsMax;
            }

            if (this is CureSpell || this is CleanseByFireSpell)
            {
                return target.Poisoned;
            }

            return true;
        }

        public virtual IEnumerable<IDamageable> AcquireIndirectTargets(IPoint3D pnt, int range)
        {
            return SpellHelper.AcquireIndirectTargets(Caster, pnt, Caster.Map, range);
        }

        private class AnimTimer : Timer
        {
            private readonly Spell m_Spell;

            public AnimTimer(Spell spell, int count)
                : base(TimeSpan.Zero, AnimateDelay, count)
            {
                m_Spell = spell;

                Priority = TimerPriority.FiftyMS;
            }

            protected override void OnTick()
            {
                if (m_Spell.State != SpellState.Casting || m_Spell.m_Caster.Spell != m_Spell)
                {
                    Stop();
                    return;
                }

                if (!m_Spell.Caster.Mounted && m_Spell.m_Info.Action >= 0)
                {
                    m_Spell.Caster.Animate(AnimationType.Spell, 0);
                }

                if (!Running)
                {
                    m_Spell.m_AnimTimer = null;
                }
            }
        }

        private class CastTimer : Timer
        {
            private static CastTimer _Instance;

            public static CastTimer Instance
            {
                get
                {
                    if (_Instance == null)
                    {
                        _Instance = new CastTimer();
                    }

                    return _Instance;
                }
            }

            public List<Spell> Registry { get; } = new List<Spell>();

            public CastTimer()
                : base(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100))
            {
                Priority = TimerPriority.FiftyMS;
            }

            public static void AddTimer(Spell spell)
            {
                Instance.Registry.Insert(0, spell);

                if (!Instance.Running)
                {
                    Instance.Start();
                }
            }

            protected override void OnTick()
            {
                var registry = Instance.Registry;

                if (registry.Count > 0)
                {
                    for (int i = registry.Count - 1; i >= 0; i--)
                    {
                        try
                        {
                            var spell = registry[i];

                            if (spell.Disturbed || !spell.Caster.Alive || spell.Caster.Deleted || spell.Caster.IsDeadBondedPet)
                            {
                                registry.RemoveAt(i);
                                spell.FinishSequence();
                            }
                            else if (spell.CastTime - 50 < Core.TickCount)
                            {
                                /*
                                 * EA seems to use some type of spell variation, of -50 ms to make up for timer resolution.
                                 * Using the below millisecond dropoff with a 50ms timer resolution seems to be exact
                                 * to EA.
                                 */

                                spell.SequenceSpell();
                                registry.RemoveAt(i);
                            }
                        }
                        catch (Exception e)
                        {
                            Diagnostics.ExceptionLogging.LogException(e, $"Count: {registry.Count}; Index: {i}");
                        }
                    }

                }
            }
        }

        public void LogBadConstructorForInstantTarget()
        {
            try
            {
                using (System.IO.StreamWriter op = new System.IO.StreamWriter("InstantTargetErr.log", true))
                {
                    op.WriteLine("# {0}", DateTime.UtcNow);
                    op.WriteLine("Target with bad contructor args:");
                    op.WriteLine("Offending Spell: {0}", ToString());
                    op.WriteLine("_____");
                }
            }
            catch (Exception e)
            {
                Diagnostics.ExceptionLogging.LogException(e);
            }
        }
    }
}
