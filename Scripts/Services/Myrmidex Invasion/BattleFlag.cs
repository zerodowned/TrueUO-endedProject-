using Server.Mobiles;
using System;

namespace Server.Engines.MyrmidexInvasion
{
    public class BattleFlag : Item
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public BattleSpawner BattleSpawner { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Allegiance Allegiance
        {
            get
            {
                if (BattleSpawner != null)
                {
                    if (this == BattleSpawner.MyrmidexFlag)
                        return Allegiance.Myrmidex;

                    return Allegiance.Tribes;
                }

                return Allegiance.None;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextSpawn { get; set; }

        [Constructable]
        public BattleFlag(int itemid, int hue)
            : base(itemid)
        {
            Hue = hue;
            Movable = false;
        }

        public override void OnDoubleClick(Mobile m)
        {
            if (m.InRange(Location, 3))
            {
                BattleSpawner spawner = BattleSpawner.Instance;

                if (spawner != null)
                {
                    DisplayWaveInfo(spawner, m);
                }
            }
            else
                m.SendLocalizedMessage(500618); // That is too far away!
        }

        public static void DisplayWaveInfo(BattleSpawner spawner, Mobile m)
        {
            int delay = 0;

            foreach (System.Collections.Generic.KeyValuePair<int, System.Collections.Generic.List<BaseCreature>> kvp in spawner.MyrmidexTeam)
            {
                if (kvp.Value.Count > 0)
                {
                    int wave = kvp.Key + 1;
                    int count = 0;

                    for (var index = 0; index < kvp.Value.Count; index++)
                    {
                        var bc = kvp.Value[index];

                        if (bc.Alive)
                        {
                            count++;
                        }
                    }

                    Timer.DelayCall(TimeSpan.FromSeconds(delay), () =>
                    {
                        m.SendLocalizedMessage(1156606, $"{(BattleSpawner.WaveCount - count).ToString()}\t{BattleSpawner.WaveCount.ToString()}\t{wave.ToString()}"); // Myrmidex have lost ~1_VAL~ of ~2_VAL~ from wave ~3_VAL~ of their front line.	
                    });
                }

                delay++;
            }

            delay = 0;

            foreach (System.Collections.Generic.KeyValuePair<int, System.Collections.Generic.List<BaseCreature>> kvp in spawner.TribeTeam)
            {
                if (kvp.Value.Count > 0)
                {
                    int wave = kvp.Key + 1;
                    int count = 0;

                    for (var index = 0; index < kvp.Value.Count; index++)
                    {
                        var bc = kvp.Value[index];

                        if (bc.Alive)
                        {
                            count++;
                        }
                    }

                    Timer.DelayCall(TimeSpan.FromSeconds(delay), () =>
                    {
                        m.SendLocalizedMessage(1156607, $"{(BattleSpawner.WaveCount - count).ToString()}\t{BattleSpawner.WaveCount.ToString()}\t{wave.ToString()}"); // Myrmidex have lost ~1_VAL~ of ~2_VAL~ from wave ~3_VAL~ of their front line.	
                    });
                }

                delay++;
            }
        }

        public override bool HandlesOnMovement => BattleSpawner != null && NextSpawn < DateTime.UtcNow;

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m is BaseCreature bc && NextSpawn < DateTime.UtcNow)
            {
                Point3D check = Allegiance == Allegiance.Myrmidex ? new Point3D(914, 1807, 0) : Location;

                if (Allegiance == Allegiance.Myrmidex && bc.InRange(check, 8))
                {
                    if (bc is BritannianInfantry || bc is BaseEodonTribesman tribesman && tribesman.TribeType != EodonTribe.Barrab)
                    {
                        Spawn(false, typeof(MyrmidexDrone), typeof(MyrmidexWarrior), typeof(TribeWarrior));
                    }
                }
                else if (Allegiance == Allegiance.Tribes && bc.InRange(check, 8) && (bc is MyrmidexDrone || bc is MyrmidexWarrior || bc is BaseEodonTribesman tribesman && tribesman.TribeType == EodonTribe.Barrab))
                {
                    Spawn(true, typeof(BritannianInfantry));
                }
            }
        }

        private void Spawn(bool tribe, params Type[] types)
        {
            if (Map == null || NextSpawn > DateTime.UtcNow)
            {
                return;
            }

            NextSpawn = DateTime.UtcNow + TimeSpan.FromMinutes(10);

            for (var i = 0; i < 5; i++)
            {
                var t = types[Utility.Random(types.Length)];
                BaseCreature bc = null;

                if (t.IsSubclassOf(typeof(BaseEodonTribesman)))
                {
                    bc = Activator.CreateInstance(t, EodonTribe.Barrab) as BaseCreature;
                }
                else
                {
                    bc = Activator.CreateInstance(t) as BaseCreature;
                }

                if (bc != null)
                {
                    var rec = new Rectangle2D(X - 5, Y - 5, 10, 10);
                    var p = Location;

                    bc.NoLootOnDeath = true;

                    for (var j = 0; j < 20; j++)
                    {
                        p = Map.GetRandomSpawnPoint(rec);

                        if (Map.CanSpawnMobile(p))
                        {
                            break;
                        }
                    }

                    if (p == Location)
                    {
                        bc.Delete();
                    }
                    else
                    {
                        bc.MoveToWorld(p, Map);

                        if (tribe)
                        {
                            bc.Home = new Point3D(914, 1872, 0);
                        }
                        else
                        {
                            bc.Home = new Point3D(913, 1792, 0);
                        }

                        bc.RangeHome = 15;
                    }
                }
            }
        }

        public BattleFlag(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);

            writer.Write(NextSpawn);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            if (version > 0)
                NextSpawn = reader.ReadDateTime();
            else
                NextSpawn = DateTime.UtcNow + TimeSpan.FromMinutes(1);
        }
    }
}
