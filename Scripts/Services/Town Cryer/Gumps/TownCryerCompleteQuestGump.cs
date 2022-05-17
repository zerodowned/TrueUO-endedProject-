using Server.Engines.Quests;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Services.TownCryer
{
    public class TownCrierQuestCompleteGump : BaseGump
    {
        public object Title { get; set; }
        public object Body { get; set; }
        public int GumpID { get; set; }

        public TownCrierQuestCompleteGump(PlayerMobile pm, object title, object body, int id)
            : base(pm, 10, 100)
        {
            Title = title;
            Body = body;
            GumpID = id;
        }

        public TownCrierQuestCompleteGump(PlayerMobile pm, BaseQuest quest)
            : base(pm, 10, 100)
        {
            Title = quest.Title;
            Body = quest.Complete;

            TownCryerNewsEntry entry = null;

            for (var index = 0; index < TownCryerSystem.NewsEntries.Count; index++)
            {
                var e = TownCryerSystem.NewsEntries[index];

                if (e.QuestType == quest.GetType())
                {
                    entry = e;
                    break;
                }
            }

            if (entry != null)
            {
                GumpID = entry.GumpImage;
            }
        }

        public override void AddGumpLayout()
        {
            AddBackground(0, 0, 454, 540, 9380);

            AddImage(62, 42, GumpID);

            if (Title is int intTitle)
            {
                AddHtmlLocalized(0, 392, 454, 20, CenterLoc, string.Format("#{0}", intTitle), 0, false, false);
            }
            else if (Title is string stringTitle)
            {
                AddHtml(0, 392, 454, 20, Center(stringTitle), false, false);
            }

            if (Body is int intBody)
            {
                AddHtmlLocalized(27, 417, 390, 73, intBody, C32216(0x080808), false, true);
            }
            else if (Body is string stringBody)
            {
                AddHtml(27, 417, 390, 73, stringBody, false, true);
            }
        }
    }
}
