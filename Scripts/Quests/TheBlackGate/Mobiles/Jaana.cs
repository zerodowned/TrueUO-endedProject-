using Server.Items;
using System;

namespace Server.Engines.Quests
{
    public class Jaana : MondainQuester
    {
        [Constructable]
        public Jaana()
            : base("Jaana")
        {
        }

        public Jaana(Serial serial)
            : base(serial)
        {
        }

        public override Type[] Quests => new[] { typeof(InTheCourtOfTruthQuest) };

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            CantWalk = true;
            Female = true;
            Race = Race.Human;

            Hue = 33770;
            HairItemID = 0x203C;
            HairHue = 1126;
        }

        public override void InitOutfit()
        {
            AddItem(new Backpack());
            AddItem(new Robe(1367));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }

    public class InTheCourtOfTruthQuest : BaseQuest
    {
        public override bool DoneOnce => true;

        public InTheCourtOfTruthQuest()
        {
            AddObjective(new ObtainObjective(typeof(ThreateningNote), "Evidence", 1, 0x46B3, 0, 66));
            AddReward(new BaseReward(typeof(BarristersRobe), 1, "A Barrister's Robe", 0x1F03, 1367));
        }

        public override object Title => "In the Court of Truth";

        public override object Description => "There has been a break in at the Yew Winery. Those who stand accused cling to claims of innocence as mere Fellowship pawns. Jaana has asked for your help in clearing the names of the accused. You are to go to the Yew Winery and uncover evidence of the true nature of the crime and return to Jaana who may present the evidence to the court in an effort to clear the names of those whom she represents.";

        public override object Refuse => "Jaana shakes her head in disappointment. You decline her request. She whispers to you. \"I only hope in your time of need someone finds the will to stand in your defense.\"";

        public override object Uncomplete => "Jaana shoots you a look of impatience as to ask. what are you waiting for? The fate of the accused are in your hands.You must visit the Yew Winery and discover evidence to exonerate the accused at once!";

        public override object Complete => "Upon visiting the winery you find the space in shambles. Between the original crime and the sloppy investigation by the Royal Guard it would be easy to overlook what you've found. None the less you have uncovered orders given to the accused demanding they steal from the winery lest their families would be in grave danger. Jaana presents the evidence to the court and the charges are dropped. Jaana thanks you for your help by giving you a Barrister's Robe!";

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }
}
