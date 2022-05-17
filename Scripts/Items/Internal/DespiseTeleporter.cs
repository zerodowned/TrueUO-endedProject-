using Server.Engines.Despise;
using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Items
{
    public class DespiseTeleporter : Teleporter
    {
        [Constructable]
        public DespiseTeleporter()
        {
        }

        public override bool CanTeleport(Mobile m)
        {
            if (m is DespiseCreature)
                return false;

            return base.CanTeleport(m);
        }

        public override void DoTeleport(Mobile m)
        {
            Map map = MapDest;

            if (map == null || map == Map.Internal)
                map = m.Map;

            Point3D p = PointDest;

            if (p == Point3D.Zero)
                p = m.Location;

            TeleportPets(m, p, map);

            bool sendEffect = !m.Hidden || m.AccessLevel == AccessLevel.Player;

            if (SourceEffect && sendEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            m.MoveToWorld(p, map);

            if (DestEffect && sendEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            if (SoundID > 0 && sendEffect)
                Effects.PlaySound(m.Location, m.Map, SoundID);
        }

        public static void TeleportPets(Mobile master, Point3D loc, Map map)
        {
            List<Mobile> move = new List<Mobile>();
            IPooledEnumerable eable = master.GetMobilesInRange(3);

            foreach (Mobile m in eable)
            {
                if (m is BaseCreature pet && !(pet is DespiseCreature) && pet.Controlled && pet.ControlMaster == master)
                {
                    if (pet.ControlOrder == OrderType.Guard || pet.ControlOrder == OrderType.Follow || pet.ControlOrder == OrderType.Come)
                    {
                        move.Add(pet);
                    }
                }
            }

            eable.Free();

            for (var index = 0; index < move.Count; index++)
            {
                Mobile m = move[index];

                m.MoveToWorld(loc, map);
            }

            move.Clear();
            move.TrimExcess();
        }

        public DespiseTeleporter(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }

    public class GateTeleporter : Item
    {
        private Point3D _Destination;
        private Map _DestinationMap;

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Destination
        {
            get => _Destination;
            set
            {
                if (_Destination != value)
                {
                    _Destination = value;
                    AssignDestination(value);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map DestinationMap
        {
            get => _DestinationMap;
            set
            {
                if (DestinationMap != value)
                {
                    _DestinationMap = value;
                    AssignMap(value);
                }
            }
        }

        public List<InternalTeleporter> Teleporters { get; set; }

        [Constructable]
        public GateTeleporter()
            : this(19343, 0, Point3D.Zero, null)
        {
        }

        public GateTeleporter(int id, int hue, Point3D destination, Map destinationMap)
            : base(id)
        {
            Hue = hue;

            Movable = false;

            _Destination = destination;
            _DestinationMap = destinationMap;

            AssignTeleporters();
        }

        private void AssignTeleporters()
        {
            if (Teleporters != null)
            {
                for (var index = 0; index < Teleporters.Count; index++)
                {
                    InternalTeleporter tele = Teleporters[index];

                    if (tele != null && !tele.Deleted)
                    {
                        tele.Delete();
                    }
                }

                ColUtility.Free(Teleporters);
            }

            Teleporters = new List<InternalTeleporter>();

            for (int i = 0; i <= 7; i++)
            {
                Direction offset = (Direction)i;

                InternalTeleporter tele = new InternalTeleporter(this, _Destination, _DestinationMap);

                int x = X;
                int y = Y;
                int z = Z;

                Movement.Movement.Offset(offset, ref x, ref y);
                tele.MoveToWorld(new Point3D(x, y, z), Map);

                Teleporters.Add(tele);
            }
        }

        public void AssignDestination(Point3D p)
        {
            if (Teleporters == null)
            {
                AssignTeleporters();
            }
            else
            {
                for (var index = 0; index < Teleporters.Count; index++)
                {
                    var t = Teleporters[index];

                    t.PointDest = p;
                }
            }
        }

        public void AssignMap(Map map)
        {
            if (Teleporters == null)
            {
                AssignTeleporters();
            }
            else
            {
                for (var index = 0; index < Teleporters.Count; index++)
                {
                    var t = Teleporters[index];

                    t.MapDest = map;
                }
            }
        }

        public override void OnMapChange()
        {
            if (Teleporters != null)
            {
                for (var index = 0; index < Teleporters.Count; index++)
                {
                    var t = Teleporters[index];

                    t.Map = Map;
                }
            }
        }

        public override void OnLocationChange(Point3D old)
        {
            if (Teleporters != null)
            {
                for (var index = 0; index < Teleporters.Count; index++)
                {
                    var t = Teleporters[index];

                    t.Location = new Point3D(X + (t.X - old.X), Y + (t.Y - old.Y), Z + (t.Z - old.Z));
                }
            }
        }

        public override void Delete()
        {
            base.Delete();

            if (Teleporters != null)
            {
                for (var index = 0; index < Teleporters.Count; index++)
                {
                    var t = Teleporters[index];

                    t.Delete();
                }

                ColUtility.Free(Teleporters);
            }
        }

        public GateTeleporter(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(_Destination);
            writer.Write(_DestinationMap);

            writer.Write(Teleporters == null ? 0 : Teleporters.Count);

            if (Teleporters != null)
            {
                for (var index = 0; index < Teleporters.Count; index++)
                {
                    var t = Teleporters[index];

                    writer.Write(t);
                }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();

            _Destination = reader.ReadPoint3D();
            _DestinationMap = reader.ReadMap();

            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                if (Teleporters == null)
                {
                    Teleporters = new List<InternalTeleporter>();
                }

                if (reader.ReadItem() is InternalTeleporter tele)
                {
                    Teleporters.Add(tele);
                    tele.Master = this;
                }
            }
        }

        public class InternalTeleporter : Teleporter
        {
            [CommandProperty(AccessLevel.GameMaster)]
            public GateTeleporter Master { get; set; }

            public InternalTeleporter(GateTeleporter master, Point3D dest, Map destMap)
                : base(dest, destMap, true)
            {
                Master = master;
            }

            public override bool OnMoveOver(Mobile m)
            {
                return true;
            }

            public override bool HandlesOnMovement => Master != null && Utility.InRange(Master.Location, Location, 1) && Map == Master.Map;

            public override void OnMovement(Mobile m, Point3D oldLocation)
            {
                if (Master == null || Master.Destination == Point3D.Zero || Master.Map == null || Master.Map == Map.Internal)
                    return;

                if (m.Location == Location)
                {
                    IPooledEnumerable<Item> eable = Map.GetItemsInRange(oldLocation, 0);

                    foreach (Item item in eable)
                    {
                        if (item is InternalTeleporter || item == Master)
                        {
                            eable.Free();
                            return;
                        }
                    }

                    base.OnMoveOver(m);
                }
            }

            public InternalTeleporter(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);
                writer.Write(0);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);
                reader.ReadInt();
            }
        }
    }
}
