namespace Server.Items
{
    public class Emerald : Item, IGem, ICommodity
    {
        [Constructable]
        public Emerald()
            : this(1)
        {
        }

        [Constructable]
        public Emerald(int amount)
            : base(0xF10)
        {
            Stackable = true;
            Amount = amount;
        }

        public Emerald(Serial serial)
            : base(serial)
        {
        }

        TextDefinition ICommodity.Description => LabelNumber;
        bool ICommodity.IsDeedable => true;

        public override double DefaultWeight => 0.1;

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
