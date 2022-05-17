using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Engines.MyrmidexInvasion
{
    public enum Allegiance
    {
        None = 0,
        Myrmidex = 1156634,
        Tribes = 1156635
    }

    public class MyrmidexInvasionSystem
    {
        public static readonly bool Active = true;

        public static string FilePath = Path.Combine("Saves", "MyrmidexInvasion.bin");
        public static MyrmidexInvasionSystem System { get; set; }

        public static List<AllianceEntry> AllianceEntries { get; set; }

        public MyrmidexInvasionSystem()
        {
            AllianceEntries = new List<AllianceEntry>();
        }

        public void Join(PlayerMobile pm, Allegiance type)
        {
            AllianceEntry entry = GetEntry(pm);

            if (entry != null)
                AllianceEntries.Remove(entry);

            pm.SendLocalizedMessage(1156636, string.Format("#{0}", ((int)type).ToString())); // You have declared allegiance to the ~1_SIDE~!  You may only change your allegiance once every 2 hours.

            AllianceEntries.Add(new AllianceEntry(pm, type));
        }

        public static bool IsAlliedWith(Mobile a, Mobile b)
        {
            return IsAlliedWithMyrmidex(a) && IsAlliedWithMyrmidex(b) || IsAlliedWithEodonTribes(a) && IsAlliedWithEodonTribes(b);
        }

        public static bool AreEnemies(Mobile a, Mobile b)
        {
            if (IsAlliedWithEodonTribes(a) && !IsAlliedWithMyrmidex(b) || IsAlliedWithEodonTribes(b) && !IsAlliedWithMyrmidex(a) || IsAlliedWithMyrmidex(a) && !IsAlliedWithEodonTribes(b))
            {
                return false;
            }

            return !IsAlliedWith(a, b);
        }

        public static bool IsAlliedWith(Mobile m, Allegiance allegiance)
        {
            return allegiance == Allegiance.Myrmidex ? IsAlliedWithMyrmidex(m) : IsAlliedWithEodonTribes(m);
        }

        public static bool IsAlliedWithMyrmidex(Mobile m)
        {
            while (true)
            {
                if (m is BaseCreature bc)
                {
                    if (bc.GetMaster() != null)
                    {
                        m = bc.GetMaster();
                        continue;
                    }

                    return bc is MyrmidexLarvae || bc is MyrmidexWarrior || bc is MyrmidexQueen || bc is MyrmidexDrone || bc is BaseEodonTribesman man && man.TribeType == EodonTribe.Barrab;
                }

                AllianceEntry entry = GetEntry(m as PlayerMobile);

                return entry != null && entry.Allegiance == Allegiance.Myrmidex;
            }
        }

        public static bool IsAlliedWithEodonTribes(Mobile m)
        {
            while (true)
            {
                if (m is BaseCreature bc)
                {
                    if (bc.GetMaster() != null)
                    {
                        m = bc.GetMaster();
                        continue;
                    }

                    return bc is BaseEodonTribesman tribesman && tribesman.TribeType != EodonTribe.Barrab || bc is BritannianInfantry;
                }

                AllianceEntry entry = GetEntry(m as PlayerMobile);

                return entry != null && entry.Allegiance == Allegiance.Tribes;
            }
        }

        public static bool CanRecieveQuest(PlayerMobile pm, Allegiance allegiance)
        {
            AllianceEntry entry = GetEntry(pm);

            return entry != null && entry.Allegiance == allegiance && entry.CanRecieveQuest;
        }

        public static AllianceEntry GetEntry(PlayerMobile pm)
        {
            if (pm == null)
            {
                return null;
            }

            for (var index = 0; index < AllianceEntries.Count; index++)
            {
                var e = AllianceEntries[index];

                if (e.Player == pm)
                {
                    return e;
                }
            }

            return null;
        }

        public static void Configure()
        {
            System = new MyrmidexInvasionSystem();

            EventSink.WorldSave += OnSave;
            EventSink.WorldLoad += OnLoad;

            CommandSystem.Register("GetAllianceEntry", AccessLevel.GameMaster, e =>
            {
                e.Mobile.BeginTarget(10, false, TargetFlags.None, (from, targeted) =>
                    {
                        if (targeted is PlayerMobile mobile)
                        {
                            AllianceEntry entry = GetEntry(mobile);

                            if (entry != null)
                            {
                                mobile.SendGump(new PropertiesGump(mobile, entry));
                            }
                            else
                                e.Mobile.SendMessage("They don't belong to an alliance.");
                        }
                    });
            });
        }

        public static void OnSave(WorldSaveEventArgs e)
        {
            Persistence.Serialize(
                FilePath,
                writer =>
                {
                    writer.Write(0);

                    writer.Write(AllianceEntries.Count);
                    for (var index = 0; index < AllianceEntries.Count; index++)
                    {
                        var entry = AllianceEntries[index];
                        entry.Serialize(writer);
                    }

                    writer.Write(MoonstonePowerGeneratorAddon.Boss);
                });
        }

        public static void OnLoad()
        {
            Persistence.Deserialize(
               FilePath,
               reader =>
               {
                   int version = reader.ReadInt();

                   int count = reader.ReadInt();
                   for (int i = 0; i < count; i++)
                   {
                       AllianceEntry entry = new AllianceEntry(reader);

                       if (entry.Player != null)
                           AllianceEntries.Add(entry);
                   }

                   MoonstonePowerGeneratorAddon.Boss = reader.ReadMobile() as Zipactriotl;
               });
        }
    }

    [PropertyObject]
    public class AllianceEntry
    {
        public PlayerMobile Player { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Allegiance Allegiance { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime JoinTime { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanRecieveQuest { get; set; }

        public AllianceEntry(PlayerMobile pm, Allegiance allegiance)
        {
            Player = pm;
            Allegiance = allegiance;
            JoinTime = DateTime.UtcNow;
        }

        public AllianceEntry(GenericReader reader)
        {
            int version = reader.ReadInt();

            Player = reader.ReadMobile() as PlayerMobile;
            Allegiance = (Allegiance)reader.ReadInt();
            JoinTime = reader.ReadDateTime();
            CanRecieveQuest = reader.ReadBool();
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(0);

            writer.Write(Player);
            writer.Write((int)Allegiance);
            writer.Write(JoinTime);
            writer.Write(CanRecieveQuest);
        }
    }
}
