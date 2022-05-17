using Server.Mobiles;

namespace Server.Items
{
    [Flipable(0x19BC, 0x19BD)]
    public partial class BaseCostume : BaseShield
    {
        public bool m_Transformed;

        private int m_Body;
        private int m_Hue = -1;

        public virtual string CreatureName { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Transformed { get => m_Transformed; set => m_Transformed = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CostumeBody { get => m_Body; set => m_Body = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CostumeHue { get => m_Hue; set => m_Hue = value; }

        public BaseCostume()
            : base(0x19BC)
        {
            Resource = CraftResource.None;
            Attributes.SpellChanneling = 1;
            Layer = Layer.FirstValid;
            Weight = 4.0;
            StrRequirement = 10;
        }

        public BaseCostume(Serial serial)
            : base(serial)
        {
        }

        private bool MaskOn(Mobile from)
        {
            if (from.Mounted || from.Flying) // You cannot use this while mounted or flying. 
            {
                from.SendLocalizedMessage(1010097);
            }
            else if (from.IsBodyMod || from.HueMod > -1)
            {
                from.SendLocalizedMessage(1158010); // You cannot use that item in this form.
            }
            else if (from is Mannequin || from is Steward)
            {
                // On EA Mannequins and Stewards can hold costumes but doing so does not change their own BodyID.
                return true;
            }
            else
            {
                from.BodyMod = m_Body;
                from.HueMod = m_Hue;
                Transformed = true;

                return true;
            }

            return false;
        }

        private void MaskOff(Mobile from)
        {
            from.BodyMod = 0;
            from.HueMod = -1;
            Transformed = false;
        }

        public override bool OnEquip(Mobile from)
        {
            if (!Transformed)
            {
                if (MaskOn(from))
                {
                    return true;
                }

                return false;
            }         

            return base.OnEquip(from);
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile mobile && Transformed)
            {
                MaskOff(mobile);
            }

            base.OnRemoved(parent);
        }

        public static void OnDamaged(Mobile m)
        {
            if (m.FindItemOnLayer(Layer.FirstValid) is BaseCostume costume)
            {
                m.AddToBackpack(costume);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(m_Body);
            writer.Write(m_Hue);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();

            m_Body = reader.ReadInt();
            m_Hue = reader.ReadInt();

            if (RootParent is Mobile mobile && mobile.Items.Contains(this))
            {
                MaskOn(mobile);
            }
        }
    }
}
