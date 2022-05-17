using Server.Gumps;
using Server.Mobiles;

namespace Server.Engines.Quests
{
    public enum Buttons
    {
        Close,
        CloseQuest,
        RefuseQuest,
        ResignQuest,
        AcceptQuest,
        AcceptReward,
        Complete,
        CompleteQuest,
        RefuseReward,
        AcceptQuestion,
        ContinueQuestion
    }

    public sealed class MondainQuestGump : BaseQuestGump
    {
        private const int ButtonOffset = 13;
        private readonly object m_Quester;
        private readonly PlayerMobile m_From;
        private readonly BaseQuest m_Quest;
        private readonly bool m_Offer;
        private readonly bool m_Completed;
        private readonly bool m_IsQuestionQuest;
        private Section m_Section;
        public int Page;

        public MondainQuestGump(PlayerMobile from)
            : this(from, null, Section.Main, false, false)
        {
        }

        public MondainQuestGump(BaseQuest quest)
            : this(quest, Section.Description, true)
        {
        }

        public MondainQuestGump(BaseQuest quest, Section section, bool offer)
            : this(null, quest, section, offer, false)
        {
        }

        public MondainQuestGump(BaseQuest quest, Section section, bool offer, bool completed)
            : this(null, quest, section, offer, completed)
        {
        }

        public MondainQuestGump(PlayerMobile owner, BaseQuest quest, Section section, bool offer, bool completed)
            : this(owner, quest, section, offer, completed, null)
        {
        }

        public MondainQuestGump(PlayerMobile owner, BaseQuest quest, Section section, bool offer, bool completed, object quester)
            : base(75, 25)
        {
            m_Quester = quester;
            m_Quest = quest;
            m_Section = section;
            m_Offer = offer;
            m_Completed = completed;

            if (quest != null)
            {
                m_From = quest.Owner;
                m_IsQuestionQuest = quest.IsQuestionQuest;
            }
            else
            {
                m_From = owner;
            }

            AddPage(++Page);

            if (m_IsQuestionQuest)
            {
                Disposable = false;
                Closable = false;

                AddImageTiled(50, 20, 400, 400, 0x1404);
                AddImageTiled(83, 15, 350, 15, 0x280A);
                AddImageTiled(50, 29, 30, 390, 0x28DC);
                AddImageTiled(34, 140, 17, 279, 0x242F);
                AddImage(48, 135, 0x28AB);
                AddImage(-16, 285, 0x28A2);
                AddImage(0, 10, 0x28B5);
                AddImage(25, 0, 0x28B4);
                AddImageTiled(415, 29, 44, 390, 0xA2D);
                AddImageTiled(415, 29, 30, 390, 0x28DC);
                AddLabel(100, 50, 0x481, "");
                AddImage(370, 50, 0x589);
                AddImage(379, 60, 0x15E8);
                AddImage(425, 0, 0x28C9);
                AddImage(34, 419, 0x2842);
                AddImage(442, 419, 0x2840);
                AddImageTiled(51, 419, 392, 17, 0x2775);
                AddImage(90, 33, 0x232D);
                AddImageTiled(130, 65, 175, 1, 0x238D);

                SecQuestionHeader();

                switch (m_Section)
                {
                    case Section.Description:
                        SecQuestionDescription();
                        break;
                    case Section.Objectives:
                        SecQuestionObjectives();
                        break;
                    case Section.Refuse:
                        SecQuestionRefuse();
                        break;
                    case Section.Failed:
                        SecQuestionFailed();
                        break;
                    case Section.Complete:
                        SecQuestionComplete();
                        break;
                }
            }
            else
            {
                switch (m_Section)
                {
                    case Section.Main:
                        SecMain();
                        break;
                    case Section.Description:
                        SecDescription();
                        SecObjectives();
                        SecRewards();
                        break;                    
                    case Section.Refuse:
                        SecRefuse();
                        break;
                    case Section.Complete:
                        SecComplete();
                        break;
                    case Section.Rewards:
                        SecCompleteRewards();
                        break;
                    case Section.InProgress:
                        SecInProgress();
                        break;
                }
            }           
        }

        public enum Section
        {
            Main,
            Description,
            Objectives,
            Rewards,
            Refuse,
            Complete,
            InProgress,
            Failed
        }

        public void SecQuestionHeader()
        {
            AddHtmlLocalized(130, 45, 270, 16, 1049010, 0x7FFF, false, false); // Quest Offer
        }

        public void SecQuestionDescription()
        {
            if (m_Quest == null)
                return;

            if (m_Quest.Title != null)
            {
                AddHtmlObject(160, 108, 250, 16, m_Quest.Title, 0x2710, false, false);
                AddHtmlLocalized(98, 140, 312, 16, 1072202, 0x2710, false, false); // Description
            }

            AddHtmlObject(98, 156, 312, 180, m_Quest.Description, 0x15F90, false, true);

            if (m_Offer)
            {
                AddButton(95, 395, 0x2EE0, 0x2EE2, (int)Buttons.AcceptQuestion, GumpButtonType.Reply, 0);
                AddHtmlLocalized(0, 0, 0, 0, 1013076, false, false); // Accept

                AddButton(313, 395, 0x2EF2, 0x2EF4, (int)Buttons.RefuseQuest, GumpButtonType.Reply, 0);
                AddHtmlLocalized(0, 0, 0, 0, 1013077, false, false); // Refuse
            }
        }

        public void SecQuestionObjectives()
        {
            if (m_Quest == null)
                return;

            AddHtmlObject(98, 156, 312, 180, m_Quest.Uncomplete, 0x15F90, false, true);

            AddButton(95, 395, 0x2EE9, 0x2EEB, (int)Buttons.ContinueQuestion, GumpButtonType.Reply, 0);
            AddHtmlLocalized(0, 0, 0, 0, 1011036, false, false); // OKAY
        }

        public void SecQuestionRefuse()
        {
            if (m_Quest == null)
                return;

            if (m_Offer)
            {
                AddHtmlObject(98, 156, 312, 180, m_Quest.Refuse, 0x15F90, false, true);

                AddButton(95, 395, 0x2EE6, 0x2EE8, (int)Buttons.Close, GumpButtonType.Reply, 0);
                AddHtmlLocalized(0, 0, 0, 0, 1060675, false, false); // CLOSE
            }
        }

        public void SecQuestionFailed()
        {
            if (m_Quest == null)
                return;            

            object fail = m_Quest.FailedMsg;

            if (fail == null)
                fail = "You have failed to meet the conditions of the quest.";

            AddHtmlObject(98, 156, 312, 180, fail, 0x15F90, false, true);

            AddButton(95, 395, 0x2EE6, 0x2EE8, (int)Buttons.Close, GumpButtonType.Reply, 0);
            AddHtmlLocalized(0, 0, 0, 0, 1060675, false, false); // CLOSE
        }

        public void SecQuestionComplete()
        {
            if (m_Quest == null)
                return;

            if (m_Quest.Complete == null)
            {
                if (QuestHelper.TryDeleteItems(m_Quest))
                {
                    if (QuestHelper.AnyRewards(m_Quest))
                    {
                        m_Section = Section.Rewards;
                        SecCompleteRewards();
                    }
                    else
                        m_Quest.GiveRewards();
                }

                return;
            }

            AddHtmlObject(98, 156, 312, 180, m_Quest.Complete, 0x15F90, false, true);

            AddButton(95, 395, 0x2EE6, 0x2EE8, (int)Buttons.Complete, GumpButtonType.Reply, 0);
            AddHtmlLocalized(0, 0, 0, 0, 1060675, false, false); // CLOSE
        }

        public void SecMain()
        {
            if (m_From == null)
                return;

            SecBackground();

            int offset = 140;

            for (int i = m_From.Quests.Count - 1; i >= 0; i--)
            {
                if (m_From.Quests[i].IsQuestionQuest)
                    continue;

                BaseQuest quest = m_From.Quests[i];

                AddHtmlObject(98, offset, 270, 21, quest.Title, quest.Failed ? 0x3C00 : White, false, false);
                AddButton(368, offset, 0x26B0, 0x26B1, ButtonOffset + i, GumpButtonType.Reply, 0);

                offset += 21;
            }

            AddButton(313, 455, 0x2EEC, 0x2EEE, (int)Buttons.Close, GumpButtonType.Reply, 0);
        }

        public void SecBackground(bool isconversation = false)
        {
            Closable = false;

            AddImageTiled(50, 20, 400, 460, 0x1404);
            AddImageTiled(50, 29, 30, 450, 0x28DC);
            AddImageTiled(34, 140, 17, 339, 0x242F);
            AddImage(48, 135, 0x28AB);
            AddImage(-16, 285, 0x28A2);
            AddImage(0, 10, 0x28B5);
            AddImage(25, 0, 0x28B4);
            AddImageTiled(83, 15, 350, 15, 0x280A);
            AddImage(34, 479, 0x2842);
            AddImage(442, 479, 0x2840);
            AddImageTiled(51, 479, 392, 17, 0x2775);
            AddImageTiled(415, 29, 44, 450, 0xA2D);
            AddImageTiled(415, 29, 30, 450, 0x28DC);
            AddLabel(100, 50, 0x481, "");
            AddImage(370, 50, 0x589);

            if ((int)m_From.AccessLevel > (int)AccessLevel.Counselor && m_Quest != null)
            {
                AddButton(379, 60, 0x15A9, 0x15A9, (int)Buttons.CompleteQuest, GumpButtonType.Reply, 0);
            }
            else
            {
                if (m_Quest == null)
                {
                    AddImage(379, 60, 0x15A9);
                }
                else
                {
                    AddImage(379, 60, 0x1580);
                }
            }

            AddImage(425, 0, 0x28C9);
            AddImage(90, 33, 0x232D);
            
            if (isconversation)
                AddHtmlLocalized(130, 45, 270, 16, 3006156, 0xFFFFFF, false, false); // Quest Conversation
            else if (m_Completed)
                AddHtmlLocalized(130, 45, 270, 16, 1072201, 0xFFFFFF, false, false); // Reward
            else if (m_Offer)
                AddHtmlLocalized(130, 45, 270, 16, 1049010, 0xFFFFFF, false, false); // Quest Offer
            else
                AddHtmlLocalized(130, 45, 270, 16, 1046026, 0xFFFFFF, false, false); // Quest Log

            AddImageTiled(130, 65, 175, 1, 0x238D);
        }

        public void SecHeader(bool isbutton = false)
        {
            if (isbutton)
            {
                if (m_Offer)
                {
                    AddButton(95, 455, 0x2EE0, 0x2EE2, (int)Buttons.AcceptQuest, GumpButtonType.Reply, 0);
                    AddButton(313, 455, 0x2EF2, 0x2EF4, (int)Buttons.RefuseQuest, GumpButtonType.Reply, 0);
                }
                else
                {
                    AddButton(95, 455, 0x2EF5, 0x2EF7, (int)Buttons.ResignQuest, GumpButtonType.Reply, 0);
                    AddButton(313, 455, 0x2EEC, 0x2EEE, (int)Buttons.CloseQuest, GumpButtonType.Reply, 0);
                }
            }

            if (m_Quest.Title is int)
            {
                AddHtmlLocalized(130, 68, 220, 48, 1114513, string.Format("@#{0}", m_Quest.Title), 0x2710, false, false); // <DIV ALIGN=CENTER>~1_TOKEN~</DIV>
            }
            else
            {
                AddHtml(130, 68, 220, 48, string.Format("<BASEFONT COLOR=#2AD580><DIV ALIGN=CENTER>{0}</DIV>", m_Quest.Title), false, false);
            }
        }

        public void SecDescription()
        {
            SecBackground();
            SecHeader(true);            

            if (!m_Quest.RenderDescription(this, m_Offer))
            {
                if (m_Quest.Failed)
                    AddHtmlLocalized(160, 80, 250, 16, 500039, 0x3C00, false, false); // Failed!                

                if (m_Quest.ChainID != QuestChain.None)
                    AddHtmlLocalized(98, 140, 312, 16, 1075024, 0x2710, false, false); // Description (quest chain)
                else
                    AddHtmlLocalized(98, 140, 312, 16, 1072202, 0x2710, false, false); // Description

                if (m_Quest.Description is int)
                {
                    AddHtmlObject(98, 156, 312, 240, m_Quest.Description, 0x15F90, false, true);
                }
                else
                {
                    AddHtml(98, 156, 312, 240, string.Format("<BASEFONT COLOR=#BFEA95>{0}</DIV>", m_Quest.Description), false, false);
                }
            }

            if (m_Quest.ShowDescription)
                AddButton(275, 430, 0x2EE9, 0x2EEB, 0, GumpButtonType.Page, Page + 1);
        }

        public void SecObjectivesButtons()
        {
            AddPage(++Page);
            SecBackground();
            SecHeader(true);

            if (m_Offer)
            {
                AddButton(95, 455, 0x2EE0, 0x2EE2, (int)Buttons.AcceptQuest, GumpButtonType.Reply, 0);
                AddButton(313, 455, 0x2EF2, 0x2EF4, (int)Buttons.RefuseQuest, GumpButtonType.Reply, 0);
            }
            else
            {
                AddButton(95, 455, 0x2EF5, 0x2EF7, (int)Buttons.ResignQuest, GumpButtonType.Reply, 0);
                AddButton(313, 455, 0x2EEC, 0x2EEE, (int)Buttons.CloseQuest, GumpButtonType.Reply, 0);
            }

            AddButton(275, 430, 0x2EE9, 0x2EEB, 0, GumpButtonType.Page, Page + 1);
            AddButton(130, 430, 0x2EEF, 0x2EF1, 0, GumpButtonType.Page, Page - 1);

            AddHtmlLocalized(98, 140, 312, 16, 1049073, 0x2710, false, false); // Objective:

            if (m_Quest.AllObjectives)
                AddHtmlLocalized(98, 156, 312, 16, 1072208, 0x2710, false, false); // All of the following	
            else
                AddHtmlLocalized(98, 156, 312, 16, 1072209, 0x2710, false, false); // Only one of the following
        }

        public void SecObjectives()
        {
            int limit = m_Offer ? 7 : 5;

            SecObjectivesButtons();

            if (!m_Quest.RenderObjective(this, m_Offer))
            {
                int offset = 172;

                for (int i = 0; i < m_Quest.Objectives.Count; i++)
                {
                    if (i != 0 && i % limit == 0)
                    {
                        SecObjectivesButtons();
                        offset = 172;
                    }

                    BaseObjective objective = m_Quest.Objectives[i];

                    if (objective is SlayObjective slay)
                    {
                        AddHtmlLocalized(98, offset, 312, 16, 1072204, 0x15F90, false, false); // Slay
                        AddLabel(133, offset, 0x481, slay.MaxProgress.ToString()); // Count
                        AddLabel(163, offset, 0x481, slay.Name); // Name 

                        offset += 16;

                        if (m_Offer)
                        {
                            if (slay.Timed)
                            {
                                AddHtmlLocalized(103, offset, 120, 16, 1062379, 0x15F90, false, false); // Est. time remaining:
                                AddLabel(223, offset, 0x481, FormatSeconds(slay.Seconds)); // %est. time remaining%

                                offset += 16;
                            }
                        }

                        if (slay.Region != null || slay.Label > 0)
                        {
                            AddHtmlLocalized(103, offset, 312, 20, 1018327, 0x15F90, false, false); // Location

                            if (slay.Label > 0)
                            {
                                AddHtmlLocalized(223, offset, 312, 20, slay.Label, 0xFFFFFF, false, false);
                            }
                            else
                            {
                                AddHtmlObject(223, offset, 312, 20, slay.Region, White, false, false); // %location%
                            }

                            offset += 16;
                        }

                        if (!m_Offer)
                        {
                            AddHtmlLocalized(103, offset, 120, 16, 3000087, 0x15F90, false, false); // Total			
                            AddLabel(223, offset, 0x481, slay.CurProgress.ToString());  // %current progress%

                            offset += 16;

                            if (ReturnTo() != null)
                            {
                                AddHtmlLocalized(103, offset, 120, 16, 1074782, 0x15F90, false, false); // Return to	
                                AddLabel(223, offset, 0x481, ReturnTo());  // %return to%		

                                offset += 16;
                            }

                            if (slay.Timed)
                            {
                                AddHtmlLocalized(103, offset, 120, 16, 1062379, 0x15F90, false, false); // Est. time remaining:
                                AddLabel(223, offset, 0x481, FormatSeconds(slay.Seconds)); // %est. time remaining%

                                offset += 16;
                            }
                        }
                    }
                    else if (objective is ObtainObjective obtain)
                    {
                        AddHtmlLocalized(98, offset, 350, 16, 1072205, 0x15F90, false, false); // Obtain						
                        AddLabel(143, offset, 0x481, obtain.MaxProgress.ToString()); // %count%
                        AddLabel(185, offset, 0x481, obtain.Name); // %name%

                        if (obtain.Image > 0)
                        {
                            AddItem(285, offset, obtain.Image, obtain.Hue); // Image
                        }

                        offset += 16;

                        if (m_Offer)
                        {
                            if (obtain.Timed)
                            {
                                AddHtmlLocalized(103, offset, 120, 16, 1062379, 0x15F90, false, false); // Est. time remaining:
                                AddLabel(223, offset, 0x481, FormatSeconds(obtain.Seconds)); // %est. time remaining%

                                offset += 16;
                            }
                            else if (obtain.Image > 0)
                            {
                                offset += 16;
                            }

                            continue;
                        }

                        AddHtmlLocalized(103, offset, 120, 16, 3000087, 0x15F90, false, false); // Total			
                        AddLabel(223, offset, 0x481, obtain.CurProgress.ToString());    // %current progress%

                        offset += 16;

                        if (ReturnTo() != null)
                        {
                            AddHtmlLocalized(103, offset, 120, 16, 1074782, 0x15F90, false, false); // Return to	
                            AddLabel(223, offset, 0x481, ReturnTo());  // %return to%

                            offset += 16;
                        }

                        if (obtain.Timed)
                        {
                            AddHtmlLocalized(103, offset, 120, 16, 1062379, 0x15F90, false, false); // Est. time remaining:
                            AddLabel(223, offset, 0x481, FormatSeconds(obtain.Seconds)); // %est. time remaining%

                            offset += 16;
                        }
                    }
                    else if (objective is DeliverObjective deliver)
                    {
                        AddHtmlLocalized(98, offset, 40, 16, 1072207, 0x15F90, false, false); // Deliver
                        AddLabel(143, offset, 0x481, deliver.MaxProgress.ToString()); // %count%
                        AddLabel(158, offset, 0x481, deliver.DeliveryName); // %name%

                        offset += 16;

                        AddHtmlLocalized(103, offset, 120, 16, 1072379, 0x15F90, false, false); // Deliver to						
                        AddLabel(223, offset, 0x481, deliver.DestName); // %deliver to%

                        offset += 16;

                        if (deliver.Timed)
                        {
                            AddHtmlLocalized(103, offset, 120, 16, 1062379, 0x15F90, false, false); // Est. time remaining:
                            AddLabel(223, offset, 0x481, FormatSeconds(deliver.Seconds)); // %est. time remaining%

                            offset += 16;
                        }
                    }
                    else if (objective is EscortObjective escort)
                    {
                        if (escort != null)
                        {
                            AddHtmlLocalized(98, offset, 312, 16, 1072206, 0x15F90, false, false); // Escort to

                            if (escort.Label == 0)
                            {
                                AddHtmlObject(173, offset, 200, 16, escort.Region, 0xFFFFFF, false, false);
                            }
                            else
                            {
                                AddHtmlLocalized(173, offset, 200, 16, escort.Label, 0xFFFFFF, false, false);
                            }

                            offset += 16;

                            if (escort.Timed)
                            {
                                AddHtmlLocalized(103, offset, 120, 16, 1062379, 0x15F90, false, false); // Est. time remaining:
                                AddLabel(223, offset, 0x481, FormatSeconds(escort.Seconds)); // %est. time remaining%

                                offset += 16;
                            }
                        }
                    }
                    else if (objective is ApprenticeObjective apprentice)
                    {
                        if (apprentice != null)
                        {
                            AddHtmlLocalized(98, offset, 200, 16, 1077485, "#" + (1044060 + (int)apprentice.Skill) + "\t" + apprentice.MaxProgress, 0x15F90, false, false); // Increase ~1_SKILL~ to ~2_VALUE~

                            offset += 16;
                        }
                    }
                    else if (objective is SimpleObjective obj && obj.Descriptions != null)
                    {
                        for (int j = 0; j < obj.Descriptions.Count; j++)
                        {
                            offset += 16;
                            AddLabel(98, offset, 0x481, obj.Descriptions[j]);
                        }

                        if (obj.Timed)
                        {
                            offset += 16;
                            AddHtmlLocalized(103, offset, 120, 16, 1062379, 0x15F90, false, false); // Est. time remaining:
                            AddLabel(223, offset, 0x481, FormatSeconds(obj.Seconds)); // %est. time remaining%
                        }
                    }
                    else if (objective.ObjectiveDescription != null)
                    {
                        if (objective.ObjectiveDescription is int iDesc)
                        {
                            AddHtmlLocalized(98, offset, 310, 300, iDesc, 0x15F90, false, false);
                        }
                        else if (objective.ObjectiveDescription is string sDesc)
                        {
                            AddHtmlObject(98, offset, 310, 300, sDesc, 0x15F90, false, false);
                        }
                    }
                }                
            }
        }

        public void SecRewards()
        {
            AddPage(++Page);

            SecBackground();
            SecHeader(true);

            AddHtmlLocalized(98, 140, 312, 16, 1072201, 0x2710, false, false); // Reward	

            int offset = 163;

            for (int i = 0; i < m_Quest.Rewards.Count; i++)
            {
                BaseReward reward = m_Quest.Rewards[i];

                if (reward != null)
                {
                    AddImage(105, offset, 0x4B9);
                    AddHtmlObject(133, offset, 280, m_Quest.Rewards.Count == 1 ? 100 : 16, reward.Name, 0x15F90, false, false);

                    if (reward.Image > 0)
                    {
                        AddItem(283, offset - 6, reward.Image);
                    }

                    offset += 16;
                }
            }

            if (m_Offer)
            {
                AddButton(95, 455, 0x2EE0, 0x2EE2, (int)Buttons.AcceptQuest, GumpButtonType.Reply, 0);
                AddButton(313, 455, 0x2EF2, 0x2EF4, (int)Buttons.RefuseQuest, GumpButtonType.Reply, 0);
                AddButton(130, 430, 0x2EEF, 0x2EF1, 0, GumpButtonType.Page, Page - 1);
            }
            else
            {
                AddButton(95, 455, 0x2EF5, 0x2EF7, (int)Buttons.ResignQuest, GumpButtonType.Reply, 0);
                AddButton(313, 455, 0x2EEC, 0x2EEE, (int)Buttons.CloseQuest, GumpButtonType.Reply, 0);
                AddButton(130, 430, 0x2EEF, 0x2EF1, 0, GumpButtonType.Page, Page - 1);
            }
        }

        public void SecRefuse()
        {
            if (m_Quest == null)
                return;

            SecBackground(true);
            SecHeader();

            if (m_Offer)
            {
                if (m_Quest.Refuse is int)
                {
                    AddHtmlObject(98, 140, 312, 180, m_Quest.Refuse, 0x15F90, false, true);
                }
                else
                {
                    AddHtml(98, 140, 312, 180, string.Format("<BASEFONT COLOR=#BFEA95>{0}", m_Quest.Refuse), false, true);
                }

                AddButton(313, 455, 0x2EE6, 0x2EE8, (int)Buttons.Close, GumpButtonType.Reply, 0);
            }
        }

        public void SecInProgress()
        {
            if (m_Quest == null)
                return;

            SecBackground(true);
            SecHeader();

            if (m_Quest.Uncomplete is int)
            {
                AddHtmlObject(98, 140, 312, 180, m_Quest.Uncomplete, 0x15F90, false, true);
            }
            else
            {
                AddHtml(98, 140, 312, 180, string.Format("<BASEFONT COLOR=#BFEA95>{0}", m_Quest.Uncomplete), false, true);
            }

            AddButton(313, 455, 0x2EE6, 0x2EE8, (int)Buttons.Close, GumpButtonType.Reply, 0);
        }

        public void SecComplete()
        {
            if (m_Quest == null)
                return;

            SecBackground(true);
            SecHeader();

            if (m_Quest.Complete == null)
            {
                if (QuestHelper.TryDeleteItems(m_Quest))
                {
                    if (QuestHelper.AnyRewards(m_Quest))
                    {
                        m_Section = Section.Rewards;
                        SecCompleteRewards();
                    }
                    else
                        m_Quest.GiveRewards();
                }

                return;
            }

            if (m_Quest.Complete is int)
            {
                AddHtmlObject(98, 140, 312, 180, m_Quest.Complete, 0x15F90, false, true);
            }
            else
            {
                AddHtml(98, 140, 312, 180, string.Format("<BASEFONT COLOR=#BFEA95>{0}", m_Quest.Complete), false, true);
            }

            AddButton(313, 455, 0x2EE6, 0x2EE8, (int)Buttons.Close, GumpButtonType.Reply, 0);
            AddButton(95, 455, 0x2EE9, 0x2EEB, (int)Buttons.Complete, GumpButtonType.Reply, 0);
        }

        public void SecCompleteRewards()
        {
            if (m_Quest == null)
                return;

            SecBackground();
            SecHeader();            

            int offset = 140;

            for (int i = 0; i < m_Quest.Rewards.Count; i++)
            {
                BaseReward reward = m_Quest.Rewards[i];

                if (reward != null)
                {
                    AddImage(107, offset + 7, 0x4B9);
                    AddHtmlObject(135, offset + 6, 280, m_Quest.Rewards.Count == 1 ? 100 : 16, reward.Name, 0x15F90, false, false);

                    if (reward.Image > 0)
                    {
                        AddItem(285, offset, reward.Image);
                    }

                    offset += 16;
                }
            }

            if (m_Completed)
            {
                AddButton(95, 455, 0x2EE0, 0x2EE2, (int)Buttons.AcceptReward, GumpButtonType.Reply, 0);

                if (m_Quest.CanRefuseReward)
                    AddButton(313, 430, 0x2EF2, 0x2EF4, (int)Buttons.RefuseReward, GumpButtonType.Reply, 0);
                else
                    AddButton(313, 455, 0x2EE6, 0x2EE8, (int)Buttons.Close, GumpButtonType.Reply, 0);
            }
        }

        public string FormatSeconds(int seconds)
        {
            int hours = seconds / 3600;

            seconds -= hours * 3600;

            int minutes = seconds / 60;

            seconds -= minutes * 60;

            if (hours > 0 && minutes > 0)
            {
                return hours + ":" + minutes + ":" + seconds;
            }

            if (minutes > 0)
            {
                return minutes + ":" + seconds;
            }

            return seconds.ToString();
        }

        public string ReturnTo()
        {
            if (m_Quest == null)
                return null;

            if (m_Quest.StartingMobile != null)
            {
                string returnTo = m_Quest.StartingMobile.Name;

                if (m_Quest.StartingMobile.Region != null)
                    returnTo = string.Format("{0} ({1})", returnTo, m_Quest.StartingMobile.Region.Name);
                else
                    returnTo = string.Format("{0}", returnTo);

                return returnTo;
            }

            return null;
        }

        public override void OnResponse(Network.NetState state, RelayInfo info)
        {
            if (m_From != null)
                m_From.CloseGump(typeof(MondainQuestGump));

            switch (info.ButtonID)
            {
                case (int)Buttons.Close: // close quest list
                    break;
                case (int)Buttons.CloseQuest: // close quest
                    if (m_From != null)
                    {
                        m_From.SendGump(new MondainQuestGump(m_From));
                    }
                    break;
                case (int)Buttons.AcceptQuest: // accept quest
                    if (m_Offer)
                    {
                        m_Quest.OnAccept();
                    }
                    break;
                case (int)Buttons.ContinueQuestion:
                    if (m_Offer)
                    {
                        m_Quest.OnAccept();
                    }
                    break;
                case (int)Buttons.AcceptQuestion:
                    if (m_Offer && m_From != null)
                    {
                        m_From.SendGump(new MondainQuestGump(m_Quest, Section.Objectives, true));
                    }
                    break;
                case (int)Buttons.RefuseQuest: // refuse quest
                    if (m_Offer)
                    {
                        m_Quest.OnRefuse();
                        if (m_From != null)
                        {
                            m_From.SendGump(new MondainQuestGump(m_Quest, Section.Refuse, true));
                        }
                    }
                    break;
                case (int)Buttons.ResignQuest: // resign quest
                    if (!m_Offer && m_From != null)
                    {
                        m_From.SendGump(new MondainResignGump(m_Quest));
                    }
                    break;
                case (int)Buttons.AcceptReward: // accept reward
                    if (!m_Offer && m_Section == Section.Rewards && m_Completed)
                        m_Quest.GiveRewards();
                    break;
                case (int)Buttons.RefuseReward: // refuse reward
                    if (!m_Offer && m_Section == Section.Rewards && m_Completed)
                        m_Quest.RefuseRewards();
                    break;
                case (int)Buttons.Complete: // player complete quest
                    if (!m_Offer && m_Section == Section.Complete)
                    {
                        if (!m_Quest.Completed)
                        {
                            if (m_From != null)
                            {
                                m_From.SendLocalizedMessage(1074861); // You do not have everything you need!
                            }
                        }
                        else
                        {
                            if (QuestHelper.TryDeleteItems(m_Quest))
                            {
                                if (m_Quester != null)
                                    m_Quest.Quester = m_Quester;

                                if (!QuestHelper.AnyRewards(m_Quest))
                                    m_Quest.GiveRewards();
                                else if (m_From != null)
                                    m_From.SendGump(new MondainQuestGump(m_Quest, Section.Rewards, false, true));
                            }
                            else
                            {
                                if (m_From != null)
                                    m_From.SendLocalizedMessage(1074861); // You do not have everything you need!
                            }
                        }
                    }
                    break;
                case (int)Buttons.CompleteQuest: // admin complete quest
                    if (m_From != null && (int)m_From.AccessLevel > (int)AccessLevel.Counselor && m_Quest != null)
                        QuestHelper.CompleteQuest(m_From, m_Quest);
                    break;
                default: // show quest
                    if (m_From != null && (m_Section != Section.Main || info.ButtonID >= m_From.Quests.Count + ButtonOffset || info.ButtonID < ButtonOffset))
                        break;

                    if (m_From != null)
                        m_From.SendGump(new MondainQuestGump(m_From.Quests[info.ButtonID - ButtonOffset],
                            Section.Description, false));
                    break;
            }
        }
    }
}
