using Server.Items;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Engines.Quests
{
    public class Gareth : MondainQuester
    {
        private static readonly Dictionary<Mobile, HumilityQuestStatus> _Table = new Dictionary<Mobile, HumilityQuestStatus>();
        private static readonly string _FilePath = Path.Combine("Saves/Misc", "CloakOfHumility.bin");

        public static void Configure()
        {
            EventSink.WorldSave += OnSave;
            EventSink.WorldLoad += OnLoad;
        }

        [Constructable]
        public Gareth()
            : base("Gareth", "the Emissary of the RBC")
        {
        }

        public Gareth(Serial serial)
            : base(serial)
        {
        }

        public override Type[] Quests => new[] { typeof(TheQuestionsQuest) };

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            Female = false;
            Race = Race.Human;
            Body = 0x190;

            Hue = 0x83EA;
            HairItemID = 0x2049;
        }

        public override void InitOutfit()
        {
            AddItem(new Backpack());
            AddItem(new Boots());
            AddItem(new BodySash(902));
            AddItem(new FancyShirt(696));
            AddItem(new LongPants(546));
        }

        public static void OnSave(WorldSaveEventArgs e)
        {
            Persistence.Serialize(
                _FilePath,
                writer =>
                {
                    writer.Write(0);

                    writer.Write(_Table.Count);

                    foreach (var t in _Table)
                    {
                        writer.Write(t.Key);
                        writer.Write((int)t.Value);
                    }
                });
        }

        public static void OnLoad()
        {
            Persistence.Deserialize(
                _FilePath,
                reader =>
                {
                    int version = reader.ReadInt();

                    int count = reader.ReadInt();

                    for (int i = count; i > 0; i--)
                    {
                        var m = reader.ReadMobile();
                        var s = (HumilityQuestStatus)reader.ReadInt();

                        if (m != null)
                        {
                            _Table.Add(m, s);
                        }
                    }
                });
        }

        public static bool CheckQuestStatus(Mobile m, HumilityQuestStatus status)
        {
            return _Table.ContainsKey(m) && _Table[m] == status;
        }

        public static void AddQuestStatus(Mobile m, HumilityQuestStatus status)
        {
            if (_Table.ContainsKey(m))
            {
                _Table[m] = status;
            }
            else
            {
                _Table.Add(m, status);
            }
        }

        public override void OnTalk(PlayerMobile player)
        {
            if (TheQuestionsQuest.CooldownTable.ContainsKey(player))
            {
                SayTo(player, 1075787, 1150); // I feel that thou hast yet more to learn about Humility... Please ponder these things further, and visit me again on the 'morrow.
                return;
            }

            if (QuestHelper.GetQuest(player, typeof(TheQuestionsQuest)) is TheQuestionsQuest q && !q.Completed)
            {
                SayTo(player, 1080107, 0x3B2); // I'm sorry, I have nothing for you at this time.
                return;
            }

            if (CheckQuestStatus(player, HumilityQuestStatus.RewardPending))
            {
                return;
            }

            if (CheckQuestStatus(player, HumilityQuestStatus.RewardRefused))
            {
                var quest = new HumilityProofGump
                {
                    Owner = player,
                    Quester = this
                };

                player.SendGump(new MondainQuestGump(quest, MondainQuestGump.Section.Complete, false, true));

                return;
            }

            if (CheckQuestStatus(player, HumilityQuestStatus.RewardAccepted))
            {
                SayTo(player, 1075898, 0x3B2); // Worry not, noble one! We shall never forget thy deeds!
                return;
            }

            if (CheckQuestStatus(player, HumilityQuestStatus.Finished))
            {
                SayTo(player, 1075899, 0x3B2); // Hail, friend!
                return;
            }

            base.OnTalk(player);
        }

        public override void Advertise()
        {
            Say(1075674); // Hail! Care to join our efforts for the Rise of Britannia?
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }
}
