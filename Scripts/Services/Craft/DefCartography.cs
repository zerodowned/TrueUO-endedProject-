using Server.Items;
using System;

namespace Server.Engines.Craft
{
    public class DefCartography : CraftSystem
    {
        private static CraftSystem m_CraftSystem;
        private DefCartography()
            : base(1, 1, 1.25)// base( 1, 1, 3.0 )
        {
        }

        public static CraftSystem CraftSystem
        {
            get
            {
                if (m_CraftSystem == null)
                    m_CraftSystem = new DefCartography();

                return m_CraftSystem;
            }
        }
        public override SkillName MainSkill => SkillName.Cartography;
        public override int GumpTitleNumber => 1044008;
        public override double GetChanceAtMin(CraftItem item)
        {
            return 0.0; // 0%
        }

        public override int CanCraft(Mobile from, ITool tool, Type itemType)
        {
            int num = 0;

            if (tool == null || tool.Deleted || tool.UsesRemaining <= 0)
                return 1044038; // You have worn out your tool!

            if (!tool.CheckAccessible(from, ref num))
                return num; // The tool must be on your person to use.

            return 0;
        }

        public override void PlayCraftEffect(Mobile from)
        {
            from.PlaySound(0x249);
        }

        public override int PlayEndingEffect(Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality, bool makersMark, CraftItem item)
        {
            if (toolBroken)
                from.SendLocalizedMessage(1044038); // You have worn out your tool

            if (failed)
            {
                if (lostMaterial)
                {
                    return 1044043; // You failed to create the item, and some of your materials are lost.
                }

                return 1044157; // You failed to create the item, but no materials were lost.
            }

            if (quality == 0)
                return 502785; // You were barely able to make this item.  It's quality is below average.

            if (makersMark && quality == 2)
                return 1044156; // You create an exceptional quality item and affix your maker's mark.

            if (quality == 2)
                return 1044155; // You create an exceptional quality item.

            if (item.ItemType == typeof(StarChart))
            {
                return 1158494; // Which telescope do you wish to create the star chart from?
            }

            return 1044154; // You create the item.
        }

        public override void InitCraftList()
        {
            AddCraft(typeof(LocalMap), 1044448, 1015230, 10.0, 70.0, typeof(BlankMap), 1044449, 1, 1044450);
            AddCraft(typeof(CityMap), 1044448, 1015231, 25.0, 85.0, typeof(BlankMap), 1044449, 1, 1044450);
            AddCraft(typeof(SeaChart), 1044448, 1015232, 35.0, 95.0, typeof(BlankMap), 1044449, 1, 1044450);
            AddCraft(typeof(WorldMap), 1044448, 1015233, 39.5, 99.5, typeof(BlankMap), 1044449, 1, 1044450);

            int index = AddMapCraft(typeof(TatteredWallMapSouth), 1044448, 1072891, 90.0, 150.0, typeof(TreasureMap), 0, 1073494, 10, 1073495);
            AddMapRes(index, typeof(TreasureMap), 2, 1073498, 5, 1073499);
            AddMapRes(index, typeof(TreasureMap), 3, 1073500, 3, 1073501);
            AddMapRes(index, typeof(TreasureMap), 4, 1073502, 1, 1073503);

            index = AddMapCraft(typeof(TatteredWallMapEast), 1044448, 1072892, 90.0, 150.0, typeof(TreasureMap), 0, 1073494, 10, 1073495);
            AddMapRes(index, typeof(TreasureMap), 2, 1073498, 5, 1073499);
            AddMapRes(index, typeof(TreasureMap), 3, 1073500, 3, 1073501);
            AddMapRes(index, typeof(TreasureMap), 4, 1073502, 1, 1073503);

            index = AddCraft(typeof(EodonianWallMap), 1044448, 1156690, 65.0, 125.0, typeof(BlankMap), 1044449, 50, 1044450);
            AddRes(index, typeof(UnabridgedAtlasOfEodon), 1156721, 1, 1156722);
            AddRecipe(index, (int)CraftRecipes.EodonianWallMap);

            index = AddCraft(typeof(StarChart), 1044448, 1158493, 0.0, 60.0, typeof(BlankMap), 1044449, 1, 1044450);
            SetForceSuccess(index, 75);
        }        
    }
}
