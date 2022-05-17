using Server.Engines.PartySystem;
using Server.Engines.Quests;
using Server.Mobiles;
using Server.Network;

namespace Server
{
    /* We need to add:
     * MondainQuesters when alive, so on MapChange
     * MondainQuesters are displayed on EA by subserver, so we'll have to do it by map
     * 
     * Corpse, when dead
     * Healers, when dead
     * 
     * Remove: healers when rezzed
     * Questers when you leave their map
     */

    public class Waypoints
    {
        public static void Create(Mobile m, Mobile mob, WaypointType type, bool ignoreObject = false)
        {
            NetState ns = m.NetState;

            if (ns != null && mob != null && !mob.Deleted)
            {
                ns.Send(new DisplayWaypoint(mob.Serial, mob.X, mob.Y, mob.Z, mob.Map.MapID, type, mob.Name + " " + mob.Title, ignoreObject));
            }
        }

        public static void Create(Mobile m, IEntity e, WaypointType type, string arg, bool ignoreObject = false)
        {
            NetState ns = m.NetState;

            if (ns != null && e != null && !e.Deleted && (!(e is Mobile) || ((Mobile)e).Alive))
            {
                ns.Send(new DisplayWaypoint(e.Serial, e.X, e.Y, e.Z, e.Map.MapID, type, arg, ignoreObject));
            }
        }

        public static void Remove(Mobile m, IEntity e)
        {
            NetState ns = m.NetState;

            ns?.Send(new RemoveWaypoint(e.Serial));
        }

        public static void OnMapChange(Mobile m, Map oldMap)
        {
            NetState ns = m.NetState;

            if (ns == null || !ns.IsEnhancedClient)
            {
                return;
            }

            if (m.Alive)
            {
                RemoveQuesters(m, ns, oldMap);
                AddQuesters(m);
            }
            else if (m.Corpse != null)
            {
                AddCorpse(m);
                RemoveHealers(m, oldMap);
                AddHealers(m);
            }
        }

        public static void OnDeath(Mobile m)
        {
            NetState ns = m.NetState;

            if (ns == null)
            {
                return;
            }

            AddHealers(m);
        }

        public static void AddCorpse(Mobile m)
        {
            if (m.Corpse != null)
            {
                Create(m, m.Corpse, WaypointType.Corpse, m.Name);
            }
        }

        public static void RemoveQuesters(Mobile m, NetState ns, Map oldMap)
        {
            if (m == null || oldMap == null)
            {
                return;
            }

            for (var index = 0; index < BaseVendor.AllVendors.Count; index++)
            {
                BaseVendor vendor = BaseVendor.AllVendors[index];

                if (vendor is MondainQuester && !vendor.Deleted && vendor.Map == oldMap)
                {
                    ns.Send(new RemoveWaypoint(vendor.Serial));
                }
            }
        }

        public static void AddQuesters(Mobile m)
        {
            if (m == null || m.Map == null || m.Deleted)
            {
                return;
            }

            for (var index = 0; index < BaseVendor.AllVendors.Count; index++)
            {
                BaseVendor vendor = BaseVendor.AllVendors[index];

                if (vendor is MondainQuester && !vendor.Deleted && vendor.Map == m.Map)
                {
                    Create(m, vendor, WaypointType.QuestGiver);
                }
            }
        }

        private static void AddHealers(Mobile m)
        {
            if (m == null || m.Map == null || m.Deleted)
            {
                return;
            }

            for (var index = 0; index < BaseVendor.AllVendors.Count; index++)
            {
                BaseVendor vendor = BaseVendor.AllVendors[index];

                if (vendor is BaseHealer healer && !healer.Deleted && healer.Map == m.Map)
                {
                    Create(m, healer, WaypointType.Resurrection);
                }
            }
        }

        public static void RemoveHealers(Mobile m, Map oldMap)
        {
            if (m == null || oldMap == null)
            {
                return;
            }

            NetState ns = m.NetState;

            if (ns == null)
            {
                return;
            }

            for (var index = 0; index < BaseVendor.AllVendors.Count; index++)
            {
                BaseVendor vendor = BaseVendor.AllVendors[index];

                if (vendor is BaseHealer healer && !healer.Deleted && healer.Map == oldMap)
                {
                    ns.Send(new RemoveWaypoint(healer.Serial));
                }
            }
        }

        public static void UpdateToParty(Mobile m)
        {
            Party p = Party.Get(m);

            if (p != null)
            {
                for (var index = 0; index < p.Members.Count; index++)
                {
                    var i = p.Members[index];

                    Mobile mob = i.Mobile;

                    if (mob != m && mob.NetState != null && mob.NetState.IsEnhancedClient)
                    {
                        Create(mob, m, WaypointType.PartyMember);
                    }
                }
            }
        }
    }

    public enum WaypointType : ushort
    {
        Corpse = 0x01,
        PartyMember = 0x02,
        RallyPoint = 0x03,
        QuestGiver = 0x04,
        QuestDestination = 0x05,
        Resurrection = 0x06,
        PointOfInterest = 0x07,
        Landmark = 0x08,
        Town = 0x09,
        Dungeon = 0x0A,
        Moongate = 0x0B,
        Shop = 0x0C,
        Player = 0x0D
    }

    public sealed class DisplayWaypoint : Packet
    {
        public DisplayWaypoint(Serial serial, int x, int y, int z, int mapID, WaypointType type, string name)
            : this(serial, x, y, z, mapID, type, name, false)
        {
        }

        public DisplayWaypoint(Serial serial, int x, int y, int z, int mapID, WaypointType type, string name, bool ignoreObject)
            : base(0xE5)
        {
            EnsureCapacity(21 + (name.Length * 2));

            m_Stream.Write(serial);

            m_Stream.Write((ushort)x);
            m_Stream.Write((ushort)y);
            m_Stream.Write((sbyte)z);
            m_Stream.Write((byte)mapID); //map 

            m_Stream.Write((ushort)type);

            m_Stream.Write((ushort)(ignoreObject ? 1 : 0));

            if (type == WaypointType.Corpse)
            {
                m_Stream.Write(1046414);
            }
            else
            {
                m_Stream.Write(1062613);
            }

            m_Stream.WriteLittleUniNull(name);

            m_Stream.Write((short)0); // terminate 
        }
    }

    public class RemoveWaypoint : Packet
    {
        public RemoveWaypoint(Serial serial)
            : base(0xE6, 5)
        {
            m_Stream.Write(serial);
        }
    }
}
