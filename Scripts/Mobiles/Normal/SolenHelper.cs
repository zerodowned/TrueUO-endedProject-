using Server.Items;
using Server.Network;

namespace Server.Mobiles
{
    interface IBlackSolen
    {
    }

    interface IRedSolen
    {
    }

    public class SolenHelper
    {
        public static Item PackPicnicBasket(IEntity e)
        {
            var basket = new PicnicBasket();

            basket.DropItem(new BeverageBottle(BeverageType.Wine));
            basket.DropItem(new CheeseWedge());

            return basket;
        }

        public static bool CheckRedFriendship(Mobile m)
        {
            while (true)
            {
                if (m is BaseCreature bc)
                {
                    if (bc.Controlled && bc.ControlMaster is PlayerMobile)
                    {
                        m = bc.ControlMaster;
                        continue;
                    }

                    if (bc.Summoned && bc.SummonMaster is PlayerMobile)
                    {
                        m = bc.SummonMaster;
                        continue;
                    }
                }

                return m is PlayerMobile player && player.SolenFriendship == SolenFriendship.Red;
            }
        }

        public static bool CheckBlackFriendship(Mobile m)
        {
            while (true)
            {
                if (m is BaseCreature bc)
                {
                    if (bc.Controlled && bc.ControlMaster is PlayerMobile)
                    {
                        m = bc.ControlMaster;
                        continue;
                    }

                    if (bc.Summoned && bc.SummonMaster is PlayerMobile)
                    {
                        m = bc.SummonMaster;
                        continue;
                    }
                }

                return m is PlayerMobile player && player.SolenFriendship == SolenFriendship.Black;
            }
        }

        public static void OnRedDamage(Mobile from)
        {
            if (from is BaseCreature bc)
            {
                if (bc.Controlled && bc.ControlMaster is PlayerMobile)
                {
                    OnRedDamage(bc.ControlMaster);
                }
                else if (bc.Summoned && bc.SummonMaster is PlayerMobile)
                {
                    OnRedDamage(bc.SummonMaster);
                }
            }

            if (from is PlayerMobile player && player.SolenFriendship == SolenFriendship.Red)
            {
                player.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1054103); // The solen revoke their friendship. You will now be considered an intruder.

                player.SolenFriendship = SolenFriendship.None;
            }
        }

        public static void OnBlackDamage(Mobile from)
        {
            if (from is BaseCreature bc)
            {
                if (bc.Controlled && bc.ControlMaster is PlayerMobile)
                {
                    OnBlackDamage(bc.ControlMaster);
                }
                else if (bc.Summoned && bc.SummonMaster is PlayerMobile)
                {
                    OnBlackDamage(bc.SummonMaster);
                }
            }

            if (from is PlayerMobile player && player.SolenFriendship == SolenFriendship.Black)
            {
                player.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1054103); // The solen revoke their friendship. You will now be considered an intruder.

                player.SolenFriendship = SolenFriendship.None;
            }
        }
    }
}
