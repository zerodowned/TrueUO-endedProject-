using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public enum VoidEvolution
    {
        None = 0,
        Killing = 1,
        Grouping = 2,
        Survival = 3
    }

    public class BaseVoidCreature : BaseCreature
    {
        public static int MutateCheck => Utility.RandomMinMax(30, 120);

        private DateTime m_NextMutate;
        private bool m_BuddyMutate;

        public virtual int GroupAmount => 2;

        public virtual VoidEvolution Evolution => VoidEvolution.None;
        public virtual int Stage => 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BuddyMutate { get => m_BuddyMutate; set => m_BuddyMutate = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextMutate { get => m_NextMutate; set => m_NextMutate = value; }

        public override bool PlayerRangeSensitive => Evolution != VoidEvolution.Killing && Stage < 3;
        public override bool AlwaysMurderer => true;

        public BaseVoidCreature(AIType aiType, int perception, int range, double passive, double active)
            : base(aiType, FightMode.Good, perception, range, passive, active)
        {
            m_NextMutate = DateTime.UtcNow + TimeSpan.FromMinutes(MutateCheck);
            m_BuddyMutate = true;
        }

        public override void OnThink()
        {
            base.OnThink();

            if (Stage >= 3 || m_NextMutate > DateTime.UtcNow)
                return;

            if (!MutateGrouped() && Alive && !Deleted)
            {
                Mutate(VoidEvolution.Survival);
            }
        }

        public bool MutateGrouped()
        {
            if (!m_BuddyMutate)
            {
                return false;
            }

            List<BaseVoidCreature> buddies = new List<BaseVoidCreature>();
            IPooledEnumerable eable = GetMobilesInRange(12);

            foreach (Mobile m in eable)
            {
                if (m != this && IsEvolutionType(m) && !m.Deleted && m.Alive && !buddies.Contains((BaseVoidCreature)m) && ((BaseVoidCreature)m).BuddyMutate)
                {
                    buddies.Add((BaseVoidCreature) m);
                }
            }

            eable.Free();

            if (buddies.Count >= GroupAmount)
            {
                Mutate(VoidEvolution.Grouping);

                for (var index = 0; index < buddies.Count; index++)
                {
                    BaseVoidCreature k = buddies[index];

                    k.Mutate(VoidEvolution.Grouping);
                }

                ColUtility.Free(buddies);

                return true;
            }

            ColUtility.Free(buddies);
            return false;
        }

        public bool IsEvolutionType(Mobile from)
        {
            if (Stage == 0 && from.GetType() != GetType())
            {
                return false;
            }

            return from is BaseVoidCreature;
        }

        public Type[][] m_EvolutionCycle =
        {
            new[] { typeof(Betballem),     typeof(Ballem),     typeof(UsagralemBallem) },
            new[] { typeof(Anlorzen),      typeof(Anlorlem),   typeof(Anlorvaglem) },
            new[] { typeof(Anzuanord),     typeof(Relanord),   typeof(Vasanord) }
        };

        private BaseCreature _MutateTo;

        public void Mutate(VoidEvolution evolution)
        {
            if (!Alive || Deleted || Stage == 3)
                return;

            VoidEvolution evo = evolution;

            if (Stage > 0)
                evo = Evolution;

            if (0.05 > Utility.RandomDouble())
            {
                SpawnOrtanords();
            }

            Type type = m_EvolutionCycle[(int)evo - 1][Stage];

            BaseCreature bc = (BaseCreature)Activator.CreateInstance(type);

            _MutateTo = bc;

            if (bc != null)
            {
                //TODO: Effents/message?

                bc.MoveToWorld(Location, Map);

                bc.Home = Home;
                bc.RangeHome = RangeHome;

                if (0.05 > Utility.RandomDouble())
                    SpawnOrtanords();

                if (bc is BaseVoidCreature creature)
                    creature.BuddyMutate = m_BuddyMutate;

                Delete();
            }
        }

        public void SpawnOrtanords()
        {
            BaseCreature ortanords = new Ortanord();

            Point3D spawnLoc = Location;

            for (int i = 0; i < 25; i++)
            {
                int x = Utility.RandomMinMax(X - 5, X + 5);
                int y = Utility.RandomMinMax(Y - 5, Y + 5);
                int z = Map.GetAverageZ(x, y);

                Point3D p = new Point3D(x, y, z);

                if (Map.CanSpawnMobile(p))
                {
                    spawnLoc = p;
                    break;
                }
            }

            ortanords.MoveToWorld(spawnLoc, Map);
            ortanords.BoltEffect(0);
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            double baseChance = 0.02;
            double chance = 0.0;

            if (Stage > 0)
                chance = baseChance * (Stage + 3);

            if (Stage > 0 && Utility.RandomDouble() < chance)
                c.DropItem(new VoidEssence());

            if (Stage == 3 && Utility.RandomDouble() < 0.12)
                c.DropItem(new VoidCore());
        }

        public override void Delete()
        {
            if (_MutateTo != null)
            {
                ISpawner s = Spawner;

                if (s is XmlSpawner xml)
                {
                    if (xml.SpawnObjects == null)
                        return;

                    for (var index = 0; index < xml.SpawnObjects.Length; index++)
                    {
                        XmlSpawner.SpawnObject so = xml.SpawnObjects[index];

                        for (int i = 0; i < so.SpawnedObjects.Count; ++i)
                        {
                            if (so.SpawnedObjects[i] == this)
                            {
                                so.SpawnedObjects[i] = _MutateTo;

                                Spawner = null;
                                base.Delete();
                                return;
                            }
                        }
                    }
                }
            }

            base.Delete();
        }

        public BaseVoidCreature(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);

            writer.Write(m_NextMutate);
            writer.Write(m_BuddyMutate);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();

            m_NextMutate = reader.ReadDateTime();
            m_BuddyMutate = reader.ReadBool();
        }
    }
}
