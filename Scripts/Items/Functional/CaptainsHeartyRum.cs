using System.Collections.Generic;
using Server.Multis;

namespace Server.Items
{
    public class CaptainsHeartyRum : Item
    {
        private string _ShipName;

        [CommandProperty(AccessLevel.GameMaster)]
        public string ShipName
        {
            get => _ShipName;
            set
            {
                _ShipName = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public CaptainsHeartyRum()
            : base(0xA40B)
        {
            Layer = Layer.OneHanded;
            ShipName = GetRandomShip();
        }

        public CaptainsHeartyRum(Serial serial)
            : base(serial)
        {
        }

        public override bool AllowEquipedCast(Mobile from)
        {
            return true;
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            list.Add(1159231, !string.IsNullOrEmpty(ShipName) ? ShipName : "An Unknown Ship"); // Captain of ~1_SHIP~'s Hearty Rum
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060482); // spell channeling
        }

        public static string GetRandomShip()
        {
            var list = new List<BaseBoat>();

            for (var index = 0; index < BaseBoat.Boats.Count; index++)
            {
                var b = BaseBoat.Boats[index];

                if (!string.IsNullOrEmpty(b.ShipName))
                {
                    list.Add(b);
                }
            }

            if (list.Count > 0)
            {
                return list[Utility.Random(list.Count)].ShipName;
            }

            return null;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(_ShipName);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();

            _ShipName = reader.ReadString();
        }
    }
}
