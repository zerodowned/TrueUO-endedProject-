using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Misc
{
    public class Aggression
    {
        private static readonly TimeSpan Delay = TimeSpan.FromMinutes(2.0);
        private const string AggressorFormat = "You are attacking {0}!";
        private const string AggressedFormat = "{0} is attacking you!";
        private const int Hue = 0x22;

        public static TimeSpan CombatHeatDelay => Delay;

        public static void Initialize()
        {
            EventSink.AggressiveAction += EventSink_AggressiveAction;
            EventSink.PlayerDeath += EventSink_PlayerDeath;
            EventSink.CreatureDeath += EventSink_CreatureDeath;
        }

        public static void EventSink_AggressiveAction(AggressiveActionEventArgs e)
        {
            Mobile aggressor = e.Aggressor;
            Mobile aggressed = e.Aggressed;

            if (!aggressor.Player || !aggressed.Player)
                return;

            if (!CheckAggressions(aggressor, aggressed))
            {
                aggressor.LocalOverheadMessage(MessageType.Regular, Hue, true, string.Format(AggressorFormat, aggressed.Name));
                aggressed.LocalOverheadMessage(MessageType.Regular, Hue, true, string.Format(AggressedFormat, aggressor.Name));
            }

            BuffInfo.AddBuff(aggressor, new BuffInfo(BuffIcon.HeatOfBattleStatus, 1153801, 1153827, Delay, aggressor, true));
            BuffInfo.AddBuff(aggressed, new BuffInfo(BuffIcon.HeatOfBattleStatus, 1153801, 1153827, Delay, aggressed, true));
        }

        public static void EventSink_PlayerDeath(PlayerDeathEventArgs e)
        {
            Mobile killed = e.Mobile;

            for (var index = 0; index < killed.Aggressed.Count; index++)
            {
                var m1 = killed.Aggressed[index];

                Mobile m = m1.Defender;
                CheckCombat(m);
            }

            for (var index = 0; index < killed.Aggressors.Count; index++)
            {
                var x = killed.Aggressors[index];

                Mobile m = x.Attacker;
                CheckCombat(m);
            }

            BuffInfo.RemoveBuff(killed, BuffIcon.HeatOfBattleStatus);
        }

        public static void EventSink_CreatureDeath(CreatureDeathEventArgs e)
        {
            Mobile killed = e.Creature;

            for (var index = 0; index < killed.Aggressed.Count; index++)
            {
                var x = killed.Aggressed[index];

                Mobile m = x.Defender;
                CheckCombat(m);
            }

            for (var index = 0; index < killed.Aggressors.Count; index++)
            {
                var x = killed.Aggressors[index];

                Mobile m = x.Attacker;
                CheckCombat(m);
            }
        }

        private static void CheckCombat(Mobile m)
        {
            if (m is PlayerMobile && !CheckHasAggression(m))
            {
                BuffInfo.RemoveBuff(m, BuffIcon.HeatOfBattleStatus);
            }
        }

        public static bool CheckAggressions(Mobile m1, Mobile m2)
        {
            List<AggressorInfo> list = m1.Aggressors;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = list[i];

                if (info.Attacker == m2 && DateTime.UtcNow < info.LastCombatTime + Delay)
                    return true;
            }

            list = m2.Aggressors;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = list[i];

                if (info.Attacker == m1 && DateTime.UtcNow < info.LastCombatTime + Delay)
                    return true;
            }

            return false;
        }

        public static bool CheckHasAggression(Mobile m, bool aggressedOnly = false)
        {
            if (Engines.VvV.ViceVsVirtueSystem.HasBattleAggression(m))
            {
                return true;
            }

            List<AggressorInfo> list = m.Aggressed;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = list[i];
                Mobile defender = info.Defender;

                if ((defender is PlayerMobile || defender is BaseCreature bc && !bc.IsMonster) && DateTime.UtcNow < info.LastCombatTime + Delay && defender.LastKilled < info.LastCombatTime)
                {
                    return true;
                }
            }

            if (aggressedOnly)
            {
                return false;
            }

            list = m.Aggressors;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = list[i];
                Mobile attacker = info.Attacker;

                if ((attacker is PlayerMobile || attacker is BaseCreature creature && !creature.IsMonster) && DateTime.UtcNow < info.LastCombatTime + Delay && attacker.LastKilled < info.LastCombatTime)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
