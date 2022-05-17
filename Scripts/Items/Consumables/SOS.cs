using Server.Gumps;
using System;

namespace Server.Items
{
    [Flipable(0x14ED, 0x14EE)]
    public class SOS : Item
    {
        public override int LabelNumber
        {
            get
            {
                if (IsAncient)
                {
                    return 1063450; // an ancient SOS
                }

                return 1041081; // a waterstained SOS
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRead { get; set; } = false;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime RetentionDate { get; set; }

        private int m_Level;
        private Map m_TargetMap;
        private Point3D m_TargetLocation;
        private int m_MessageIndex;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsAncient => m_Level >= 4;

        [CommandProperty(AccessLevel.GameMaster)]
        public string ShipwreckName { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level
        {
            get => m_Level;
            set
            {
                m_Level = Math.Max(1, Math.Min(value, 4));

                if (IsAncient && string.IsNullOrEmpty(ShipwreckName))
                {
                    ShipwreckName = _Names[Utility.Random(_Names.Length)];
                }

                UpdateHue();
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map TargetMap { get => m_TargetMap; set => m_TargetMap = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D TargetLocation { get => m_TargetLocation; set => m_TargetLocation = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MessageIndex { get => m_MessageIndex; set => m_MessageIndex = value; }

        public void UpdateHue()
        {
            Hue = IsAncient ? 0x481 : 0;
        }

        [Constructable]
        public SOS()
            : this(Map.Trammel)
        {
        }

        [Constructable]
        public SOS(Map map)
            : this(map, MessageInABottle.GetRandomLevel())
        {
        }

        [Constructable]
        public SOS(Map map, int level)
            : base(0x14EE)
        {
            Weight = 1.0;

            Level = level;
            m_MessageIndex = Utility.Random(MessageEntry.Entries.Length);
            m_TargetMap = map;
            m_TargetLocation = FindLocation(m_TargetMap);

            UpdateHue();
        }

        public SOS(Serial serial)
            : base(serial)
        {
        }

        private static string[] _Names =
        {
            "The Beast", "The Crown Jewel", "The Dragon's Breath", "The Excellencia", "The Golden Ankh", "The HMS Cape", "The Mustang", "The Nymphet", "The Spartan", "The Scaly Eel",
            "The Silver Hart", "The Vengeance Bay Ghost Ship", "The Poseidon's Fury", "The Lusty Wench", "The Empire", "The Ararat", "The Rogue", "The Arabella"
        };

        public void SOSReset()
        {
            if (!IsRead && RootParent is Container && RetentionDate < DateTime.UtcNow)
            {
                m_TargetLocation = FindLocation(m_TargetMap);
                m_MessageIndex = Utility.Random(MessageEntry.Entries.Length);

                RetentionDate = DateTime.UtcNow + TimeSpan.FromDays(30);
            }
        }

        protected override void OnParentChanged(object oldParent)
        {
            base.OnParentChanged(oldParent);            

            if (!IsRead)
            {
                if (RetentionDate != DateTime.MinValue)
                {
                    RetentionDate = DateTime.MinValue;
                }

                if (oldParent == null && RootParent is Container)
                {
                    RetentionDate = DateTime.UtcNow + TimeSpan.FromDays(30);
                }
            }            
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(6);

            writer.Write(RetentionDate);
            writer.Write(IsRead);
            writer.Write(ShipwreckName);
            writer.Write(m_Level);
            writer.Write(m_TargetMap);
            writer.Write(m_TargetLocation);
            writer.Write(m_MessageIndex);

            SOSReset();
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 6:
                    {
                        RetentionDate = reader.ReadDateTime();
                        IsRead = reader.ReadBool();
                        goto case 5;
                    }
                case 5:
                    {
                        ShipwreckName = reader.ReadString();
                        goto case 2;
                    }
                case 4:
                case 3:
                case 2:
                    {
                        m_Level = reader.ReadInt();
                        goto case 1;
                    }
                case 1:
                    {
                        m_TargetMap = reader.ReadMap();
                        m_TargetLocation = reader.ReadPoint3D();
                        m_MessageIndex = reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }

            if (version < 6 && RootParent is Container)
            {
                RetentionDate = DateTime.UtcNow + TimeSpan.FromDays(30);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                MessageEntry entry;

                if (m_MessageIndex >= 0 && m_MessageIndex < MessageEntry.Entries.Length)
                    entry = MessageEntry.Entries[m_MessageIndex];
                else
                    entry = MessageEntry.Entries[m_MessageIndex = Utility.Random(MessageEntry.Entries.Length)];

                IsRead = true;
                RetentionDate = DateTime.MinValue;

                from.CloseGump(typeof(MessageGump));
                from.SendGump(new MessageGump(entry, m_TargetMap, m_TargetLocation));
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public virtual void OnSOSComplete(Container chest)
        {
        }

        private static readonly int[] m_WaterTiles =
        {
            0x00A8, 0x00AB, 0x0136, 0x0137
        };

        private static readonly Rectangle2D[] m_BritRegions = { new Rectangle2D(8, 88, 88, 3927), new Rectangle2D(96, 1547, 192, 2463), new Rectangle2D(288, 1729, 262, 260), new Rectangle2D(550, 1873, 136, 159), new Rectangle2D(1584, 3617, 448, 464), new Rectangle2D(2360, 2497, 920, 728), new Rectangle2D(2640, 3705, 1384, 344), new Rectangle2D(3232, 3257, 760, 408), new Rectangle2D(2296, 1353, 1272, 528), new Rectangle2D(2952, 1041, 496, 312), new Rectangle2D(3568, 25, 256, 976), new Rectangle2D(3872, 1585, 584, 1344), new Rectangle2D(4464, 1585, 560, 664), new Rectangle2D(4464, 2433, 568, 496) };
        private static readonly Rectangle2D[] m_IlshRegions = { new Rectangle2D(1472, 272, 304, 240), new Rectangle2D(1240, 1000, 312, 160) };
        private static readonly Rectangle2D[] m_MalasRegions = { new Rectangle2D(1376, 1520, 464, 280) };
        private static readonly Rectangle2D[] m_TokunoRegions = { new Rectangle2D(10, 10, 1440, 1440) };

        public static Point3D FindLocation(Map map)
        {
            if (map == null || map == Map.Internal)
                return Point3D.Zero;

            Rectangle2D[] regions;

            if (map == Map.Felucca || map == Map.Trammel)
                regions = m_BritRegions;
            else if (map == Map.Ilshenar)
                regions = m_IlshRegions;
            else if (map == Map.Malas)
                regions = m_MalasRegions;
            else if (map == Map.Tokuno)
                regions = m_TokunoRegions;
            else
                regions = new[] { new Rectangle2D(0, 0, map.Width, map.Height) };

            if (regions.Length == 0)
                return Point3D.Zero;

            for (int i = 0; i < 50; ++i)
            {
                Rectangle2D reg = regions[Utility.Random(regions.Length)];
                int x = Utility.Random(reg.X, reg.Width);
                int y = Utility.Random(reg.Y, reg.Height);

                if (!ValidateDeepWater(map, x, y))
                    continue;

                bool valid = true;

                for (int j = 1, offset = 5; valid && j <= 5; ++j, offset += 5)
                {
                    if (!ValidateDeepWater(map, x + offset, y + offset))
                        valid = false;
                    else if (!ValidateDeepWater(map, x + offset, y - offset))
                        valid = false;
                    else if (!ValidateDeepWater(map, x - offset, y + offset))
                        valid = false;
                    else if (!ValidateDeepWater(map, x - offset, y - offset))
                        valid = false;
                }

                if (valid)
                    return new Point3D(x, y, 0);
            }

            return Point3D.Zero;
        }

        public static bool ValidateDeepWater(Map map, int x, int y)
        {
            int tileID = map.Tiles.GetLandTile(x, y).ID;
            bool water = false;

            for (int i = 0; !water && i < m_WaterTiles.Length; i += 2)
                water = tileID >= m_WaterTiles[i] && tileID <= m_WaterTiles[i + 1];

            return water;
        }

        private class MessageGump : Gump
        {
            public MessageGump(MessageEntry entry, Map map, Point3D loc)
                : base(150, 50)
            {
                int xLong = 0, yLat = 0;
                int xMins = 0, yMins = 0;
                bool xEast = false, ySouth = false;
                string fmt;

                if (Sextant.Format(loc, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
                {
                    fmt = $"{yLat}o {yMins}'{(ySouth ? "S" : "N")}, {xLong}o {xMins}'{(xEast ? "E" : "W")}";
                }
                else
                {
                    fmt = "?????";
                }

                AddPage(0);

                AddBackground(0, 0, 254, 400, 9390);

                AddHtmlLocalized(27, 50, 200, 300, entry.Message, fmt, 0, false, false);
            }
        }

        private class MessageEntry
        {
            private readonly int m_Width;
            private readonly int m_Height;
            private readonly int m_Message;

            public int Width => m_Width;
            public int Height => m_Height;
            public int Message => m_Message;

            public MessageEntry(int width, int height, int message)
            {
                m_Width = width;
                m_Height = height;
                m_Message = message;
            }

            private static readonly MessageEntry[] m_Entries =
            {
                new MessageEntry(280, 180, 1153540),
                new MessageEntry(280, 215, 1153546),
                new MessageEntry(280, 285, 1153547),
                new MessageEntry(280, 180, 1153537),
                new MessageEntry(280, 215, 1153539),
                new MessageEntry(280, 195, 1153543),
                new MessageEntry(280, 265, 1153548),
                new MessageEntry(280, 230, 1153544),
                new MessageEntry(280, 285, 1153545),
                new MessageEntry(280, 285, 1153549),
                new MessageEntry(280, 160, 1153541),
                new MessageEntry(280, 250, 1153538),
                new MessageEntry(280, 250, 1153542),
                new MessageEntry(280, 250, 1153550)
            };

            public static MessageEntry[] Entries => m_Entries;
        }
    }
}
