using Server.Commands;
using Server.ContextMenus;
using Server.Engines.Exodus;
using Server.Engines.PartySystem;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class ExodusTomeAltar : BaseDecayingItem
    {
        public override int LabelNumber => 1153602;  // Exodus Summoning Tome 
        public static ExodusTomeAltar Altar { get; set; }
        public TimeSpan DelayExit => TimeSpan.FromMinutes(10);
        private Point3D m_TeleportDest = new Point3D(764, 640, 0);
        public override int Lifespan => 420;
        public override bool UseSeconds => false;
        private readonly List<RitualArray> m_Rituals;
        private Mobile m_Owner;
        private Item m_ExodusAlterAddon;

        public List<RitualArray> Rituals => m_Rituals;

        public Mobile Owner { get => m_Owner; set => m_Owner = value; }

        [Constructable]
        public ExodusTomeAltar()
            : base(0x1C11)
        {
            Hue = 1943;
            Movable = false;
            LootType = LootType.Regular;
            Weight = 0.0;

            m_Rituals = new List<RitualArray>();

            m_ExodusAlterAddon = new ExodusAlterAddon
            {
                Movable = false
            };
        }

        public ExodusTomeAltar(Serial serial) : base(serial)
        {
        }

        private class BeginTheRitual : ContextMenuEntry
        {
            private readonly Mobile m_Mobile;
            private readonly ExodusTomeAltar m_altar;

            public BeginTheRitual(ExodusTomeAltar altar, Mobile from) : base(1153608, 2) // Begin the Ritual
            {
                m_Mobile = from;
                m_altar = altar;

                if (altar.Owner != from)
                    Flags |= CMEFlags.Disabled;
            }

            public override void OnClick()
            {
                if (m_altar.Owner == m_Mobile)
                {
                    m_altar.SendConfirmationsExodus(m_Mobile);
                }
                else
                {
                    m_Mobile.SendLocalizedMessage(1153610); // Only the altar owner can commence with the ritual. 
                }
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            list.Add(new BeginTheRitual(this, from));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.HasGump(typeof(AltarGump)))
            {
                from.SendGump(new AltarGump());
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            return false;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            m_ExodusAlterAddon?.Delete();

            if (Altar != null)
            {
                Altar = null;
            }
        }

        public override void OnMapChange()
        {
            if (Deleted)
            {
                return;
            }

            if (m_ExodusAlterAddon != null)
            {
                m_ExodusAlterAddon.Map = Map;
            }
        }

        public override void OnLocationChange(Point3D oldLoc)
        {
            if (Deleted)
            {
                return;
            }

            if (m_ExodusAlterAddon != null)
            {
                m_ExodusAlterAddon.Location = new Point3D(X - 1, Y - 1, Z - 18);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version

            writer.Write(m_ExodusAlterAddon);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();

            m_ExodusAlterAddon = reader.ReadItem();
        }

        public bool CheckParty(Mobile from, Mobile m)
        {
            Party party = Party.Get(from);

            if (party != null)
            {
                for (var index = 0; index < party.Members.Count; index++)
                {
                    PartyMemberInfo info = party.Members[index];

                    if (info.Mobile == m)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public virtual void SendConfirmationsExodus(Mobile from)
        {
            Party party = Party.Get(from);

            if (party != null)
            {
                int MemberRange = 0;

                for (var index = 0; index < party.Members.Count; index++)
                {
                    var x = party.Members[index];

                    if (!from.InRange(x.Mobile, 5))
                    {
                        MemberRange++;
                    }
                }

                if (MemberRange != 0)
                {
                    from.SendLocalizedMessage(1153611); // One or more members of your party are not close enough to you to perform the ritual.
                    return;
                }

                RobeofRite robe;

                for (var i = 0; i < party.Members.Count; i++)
                {
                    PartyMemberInfo info = party.Members[i];

                    robe = info.Mobile.FindItemOnLayer(Layer.OuterTorso) as RobeofRite;

                    bool any = false;

                    for (var index = 0; index < m_Rituals.Count; index++)
                    {
                        var completed = m_Rituals[index];

                        if (completed.RitualMobile == info.Mobile && completed.Ritual1 && completed.Ritual2)
                        {
                            any = true;
                            break;
                        }
                    }

                    if (!any || robe == null)
                    {
                        from.SendLocalizedMessage(1153609, info.Mobile.Name); // ~1_PLAYER~ has not fulfilled all the requirements of the Ritual! You cannot commence until they do.
                        return;
                    }
                }

                for (var index = 0; index < party.Members.Count; index++)
                {
                    PartyMemberInfo info = party.Members[index];

                    SendBattleground(info.Mobile);
                }
            }
            else
            {
                from.SendLocalizedMessage(1153596); // You must join a party with the players you wish to perform the ritual with. 
            }
        }

        public virtual void SendBattleground(Mobile from)
        {
            if (VerLorRegController.Active && VerLorRegController.Mobile != null && ExodusSummoningAlter.CheckExodus())
            {
                // teleport party member's pets
                if (from is PlayerMobile pm)
                {
                    for (var index = 0; index < pm.AllFollowers.Count; index++)
                    {
                        Mobile follower = pm.AllFollowers[index];

                        if (follower is BaseCreature pet && pet.Alive && pet.InRange(pm.Location, 5) && !(pet is BaseMount mount && mount.Rider != null))
                        {
                            pet.FixedParticles(0x376A, 9, 32, 0x13AF, EffectLayer.Waist);
                            pet.PlaySound(0x1FE);
                            pet.MoveToWorld(m_TeleportDest, Map.Ilshenar);
                        }
                    }
                }

                // teleport party member
                from.FixedParticles(0x376A, 9, 32, 0x13AF, EffectLayer.Waist);
                from.PlaySound(0x1FE);
                from.MoveToWorld(m_TeleportDest, Map.Ilshenar);

                // Robe of Rite Delete
                if (from.FindItemOnLayer(Layer.OuterTorso) is RobeofRite robe)
                {
                    robe.Delete();
                }

                // Altar Delete
                Timer.DelayCall(TimeSpan.FromSeconds(2), Delete);
            }
            else
            {
                from.SendLocalizedMessage(1075213); // The master of this realm has already been summoned and is engaged in combat.  Your opportunity will come after he has squashed the current batch of intruders!
            }
        }
    }

    public class RitualArray
    {
        public Mobile RitualMobile { get; set; }
        public bool Ritual1 { get; set; }
        public bool Ritual2 { get; set; }
    }

    public class AltarGump : Gump
    {
        public static void Initialize()
        {
            CommandSystem.Register("TomeAltarGump", AccessLevel.Administrator, TomeAltarGump_OnCommand);
        }

        [Usage("TomeAltarGump")]
        public static void TomeAltarGump_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (!from.HasGump(typeof(AltarGump)))
            {
                from.SendGump(new AltarGump());
            }
        }

        public AltarGump() : base(100, 100)
        {
            Closable = true;
            Disposable = true;
            Dragable = true;

            AddPage(0);
            AddBackground(0, 0, 447, 195, 5120);
            AddHtmlLocalized(17, 14, 412, 161, 1153607, 0x7FFF, false, false); // Contained within this Tome is the ritual by which Lord Exodus may once again be called upon Britannia in his physical form, summoned from deep within the Void.  Only when the Summoning Rite has been rejoined with the tome and only when the Robe of Rite covers the caster can the Sacrificial Dagger be used to seal thy fate.  Stab into this book the dagger and declare thy quest for Valor as thou stand to defend Britannia from this evil, or sacrifice thy blood unto this altar to declare thy quest for greed and wealth...only thou can judge thyself...
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            switch (info.ButtonID)
            {
                case 0:
                    {
                        //Cancel
                        break;
                    }
            }
        }
    }
}
