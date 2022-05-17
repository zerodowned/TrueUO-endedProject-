using Server.Commands;
using Server.ContextMenus;
using Server.Engines.Auction;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Regions;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Server.Engines.VendorSearching
{
    public class VendorSearch
    {
        public static readonly string FilePath = Path.Combine("Saves/Misc", "VendorSearch.bin");
        public static StringList StringList => StringList.Localization;

        public static List<SearchItem> DoSearchAuction(SearchCriteria criteria)
        {
            if (criteria == null || Auction.Auction.Auctions == null || Auction.Auction.Auctions.Count == 0)
            {
                return null;
            }

            List<SearchItem> list = new List<SearchItem>();

            SearchDetail first = null;

            for (var index = 0; index < criteria.Details.Count; index++)
            {
                var d = criteria.Details[index];

                if (d.Attribute is Misc misc && misc == Misc.ExcludeFel)
                {
                    first = d;
                    break;
                }
            }

            bool excludefel = first != null;

            for (var index = 0; index < Auction.Auction.Auctions.Count; index++)
            {
                Auction.Auction pv = Auction.Auction.Auctions[index];

                if (pv.AuctionItem != null && pv.AuctionItem.Map != Map.Internal && pv.AuctionItem.Map != null && pv.OnGoing && (!excludefel || pv.AuctionItem.Map != Map.Felucca))
                {
                    list.Add(new SearchItem(pv.Safe, pv.AuctionItem, (int) pv.Buyout, false));
                }
            }

            switch (criteria.SortBy)
            {
                case SortBy.LowToHigh: list = list.OrderBy(vi => vi.Price).ToList(); break;
                case SortBy.HighToLow: list = list.OrderBy(vi => -vi.Price).ToList(); break;
            }

            return list;
        }

        public static List<SearchItem> DoSearch(SearchCriteria criteria)
        {
            if (criteria == null || PlayerVendor.PlayerVendors == null || PlayerVendor.PlayerVendors.Count == 0)
            {
                return null;
            }

            List<SearchItem> list = new List<SearchItem>();

            SearchDetail first = null;

            for (var index = 0; index < criteria.Details.Count; index++)
            {
                var d = criteria.Details[index];

                if (d.Attribute is Misc misc && misc == Misc.ExcludeFel)
                {
                    first = d;
                    break;
                }
            }

            bool excludefel = first != null;

            for (var i = 0; i < PlayerVendor.PlayerVendors.Count; i++)
            {
                PlayerVendor pv = PlayerVendor.PlayerVendors[i];

                if (pv.Map != Map.Internal && pv.Map != null && pv.Backpack != null && pv.VendorSearch && pv.Backpack.Items.Count > 0 && (!excludefel || pv.Map != Map.Felucca))
                {
                    List<Item> items = GetItems(pv);

                    for (var index = 0; index < items.Count; index++)
                    {
                        Item item = items[index];
                        VendorItem vendorItem = pv.GetVendorItem(item);

                        int price = 0;
                        bool isChild = false;

                        if (vendorItem != null)
                        {
                            price = vendorItem.Price;
                        }
                        else if (item.Parent is Container parent)
                        {
                            vendorItem = GetParentVendorItem(pv, parent);

                            if (vendorItem != null)
                            {
                                isChild = true;
                                price = vendorItem.Price;
                            }
                        }

                        if (price > 0 && CheckMatch(item, price, criteria))
                        {
                            list.Add(new SearchItem(pv, item, price, isChild));
                        }
                    }

                    ColUtility.Free(items);
                }
            }

            switch (criteria.SortBy)
            {
                case SortBy.LowToHigh: list = list.OrderBy(vi => vi.Price).ToList(); break;
                case SortBy.HighToLow: list = list.OrderBy(vi => -vi.Price).ToList(); break;
            }

            return list;
        }

        private static VendorItem GetParentVendorItem(PlayerVendor pv, Container parent)
        {
            while (true)
            {
                VendorItem vendorItem = pv.GetVendorItem(parent);

                if (vendorItem == null)
                {
                    if (parent.Parent is Container container)
                    {
                        parent = container;
                        continue;
                    }
                }

                return vendorItem;
            }
        }

        public static bool CheckMatch(Item item, int price, SearchCriteria searchCriteria)
        {
            if (item is CommodityDeed deed && deed.Commodity != null)
            {
                item = deed.Commodity;
            }

            if (searchCriteria.MinPrice > -1 && price < searchCriteria.MinPrice)
            {
                return false;
            }

            if (searchCriteria.MaxPrice > -1 && price > searchCriteria.MaxPrice)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(searchCriteria.SearchName))
            {
                string name;

                if (item is CommodityDeed commodityDeed && commodityDeed.Commodity is ICommodity commodity)
                {
                    if (!string.IsNullOrEmpty(commodity.Description.String))
                    {
                        name = commodity.Description.String;
                    }
                    else
                    {
                        name = StringList.GetString(commodity.Description.Number);
                    }
                }
                else
                {
                    name = GetItemName(item);
                }

                if (name == null)
                {
                    return false; // TODO? REturn null names?
                }

                if (!CheckKeyword(searchCriteria.SearchName, item) && name.ToLower().IndexOf(searchCriteria.SearchName.ToLower()) < 0)
                {
                    return false;
                }
            }

            if (searchCriteria.SearchType != Layer.Invalid && searchCriteria.SearchType != item.Layer)
            {
                return false;
            }

            if (searchCriteria.Details.Count == 0)
                return true;

            for (var index = 0; index < searchCriteria.Details.Count; index++)
            {
                SearchDetail detail = searchCriteria.Details[index];

                object o = detail.Attribute;
                int value = detail.Value;

                if (value == 0)
                {
                    value = 1;
                }

                if (o is AosAttribute attribute)
                {
                    AosAttributes attrs = RunicReforging.GetAosAttributes(item);

                    if (attrs == null || attrs[attribute] < value)
                    {
                        return false;
                    }
                }
                else if (o is AosWeaponAttribute weaponAttribute)
                {
                    AosWeaponAttributes attrs = RunicReforging.GetAosWeaponAttributes(item);

                    if (weaponAttribute == AosWeaponAttribute.MageWeapon)
                    {
                        if (attrs == null || attrs[weaponAttribute] == 0 || attrs[weaponAttribute] > Math.Max(0, 30 - value))
                        {
                            return false;
                        }
                    }
                    else if (attrs == null || attrs[weaponAttribute] < value)
                        return false;
                }
                else if (o is SAAbsorptionAttribute absorptionAttribute)
                {
                    SAAbsorptionAttributes attrs = RunicReforging.GetSAAbsorptionAttributes(item);

                    if (attrs == null || attrs[absorptionAttribute] < value)
                        return false;
                }
                else if (o is AosArmorAttribute armorAttribute)
                {
                    AosArmorAttributes attrs = RunicReforging.GetAosArmorAttributes(item);

                    if (attrs == null || attrs[armorAttribute] < value)
                    {
                        return false;
                    }
                }
                else if (o is SkillName skillName)
                {
                    if (detail.Category != Category.RequiredSkill)
                    {
                        AosSkillBonuses skillbonuses = RunicReforging.GetAosSkillBonuses(item);

                        if (skillbonuses != null)
                        {
                            bool hasSkill = false;

                            for (int i = 0; i < 5; i++)
                            {
                                SkillName check;
                                double bonus;

                                if (skillbonuses.GetValues(i, out check, out bonus) && check == skillName && bonus >= value)
                                {
                                    hasSkill = true;
                                    break;
                                }
                            }

                            if (!hasSkill)
                                return false;
                        }
                        else if (item is SpecialScroll scroll && value >= 105)
                        {
                            if (scroll.Skill != skillName || scroll.Value < value)
                                return false;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (!(item is BaseWeapon) || ((BaseWeapon) item).DefSkill != skillName)
                    {
                        return false;
                    }
                }
                else if (!CheckSlayer(item, o))
                {
                    return false;
                }
                else if (o is AosElementAttribute elementAttribute)
                {
                    if (item is BaseWeapon wep)
                    {
                        if (detail.Category == Category.DamageType)
                        {
                            int phys, fire, cold, pois, nrgy, chaos, direct;

                            wep.GetDamageTypes(null, out phys, out fire, out cold, out pois, out nrgy, out chaos, out direct);

                            switch (elementAttribute)
                            {
                                case AosElementAttribute.Physical:
                                    if (phys < value) return false;
                                    break;
                                case AosElementAttribute.Fire:
                                    if (fire < value) return false;
                                    break;
                                case AosElementAttribute.Cold:
                                    if (cold < value) return false;
                                    break;
                                case AosElementAttribute.Poison:
                                    if (pois < value) return false;
                                    break;
                                case AosElementAttribute.Energy:
                                    if (nrgy < value) return false;
                                    break;
                                case AosElementAttribute.Chaos:
                                    if (chaos < value) return false;
                                    break;
                                case AosElementAttribute.Direct:
                                    if (direct < value) return false;
                                    break;
                            }
                        }
                        else
                        {
                            switch (elementAttribute)
                            {
                                case AosElementAttribute.Physical:
                                    if (wep.WeaponAttributes.ResistPhysicalBonus < value) return false;
                                    break;
                                case AosElementAttribute.Fire:
                                    if (wep.WeaponAttributes.ResistFireBonus < value) return false;
                                    break;
                                case AosElementAttribute.Cold:
                                    if (wep.WeaponAttributes.ResistColdBonus < value) return false;
                                    break;
                                case AosElementAttribute.Poison:
                                    if (wep.WeaponAttributes.ResistPoisonBonus < value) return false;
                                    break;
                                case AosElementAttribute.Energy:
                                    if (wep.WeaponAttributes.ResistEnergyBonus < value) return false;
                                    break;
                            }
                        }
                    }
                    else if (item is BaseArmor armor && detail.Category == Category.Resists)
                    {
                        switch (elementAttribute)
                        {
                            case AosElementAttribute.Physical:
                                if (armor.PhysicalResistance < value) return false;
                                break;
                            case AosElementAttribute.Fire:
                                if (armor.FireResistance < value) return false;
                                break;
                            case AosElementAttribute.Cold:
                                if (armor.ColdResistance < value) return false;
                                break;
                            case AosElementAttribute.Poison:
                                if (armor.PoisonResistance < value) return false;
                                break;
                            case AosElementAttribute.Energy:
                                if (armor.EnergyResistance < value) return false;
                                break;
                        }
                    }
                    else if (detail.Category != Category.DamageType)
                    {
                        AosElementAttributes attrs = RunicReforging.GetElementalAttributes(item);

                        if (attrs == null || attrs[elementAttribute] < value)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (o is Misc misc)
                {
                    switch (misc)
                    {
                        case Misc.ExcludeFel: break;
                        case Misc.GargoyleOnly:
                            if (!IsGargoyle(item))
                                return false;
                            break;
                        case Misc.NotGargoyleOnly:
                            if (IsGargoyle(item))
                                return false;
                            break;
                        case Misc.ElvesOnly:
                            if (!IsElf(item))
                                return false;
                            break;
                        case Misc.NotElvesOnly:
                            if (IsElf(item))
                                return false;
                            break;
                        case Misc.FactionItem:
                            return false;
                        case Misc.PromotionalToken:
                            if (!(item is PromotionalToken))
                                return false;
                            break;
                        case Misc.Cursed:
                            if (item.LootType != LootType.Cursed)
                                return false;
                            break;
                        case Misc.NotCursed:
                            if (item.LootType == LootType.Cursed)
                                return false;
                            break;
                        case Misc.CannotRepair:
                            if (CheckCanRepair(item))
                                return false;
                            break;
                        case Misc.NotCannotBeRepaired:
                            if (!CheckCanRepair(item))
                                return false;
                            break;
                        case Misc.Brittle:
                            NegativeAttributes neg2 = RunicReforging.GetNegativeAttributes(item);
                            if (neg2 == null || neg2.Brittle == 0)
                                return false;
                            break;
                        case Misc.NotBrittle:
                            NegativeAttributes neg3 = RunicReforging.GetNegativeAttributes(item);
                            if (neg3 != null && neg3.Brittle > 0)
                                return false;
                            break;
                        case Misc.Antique:
                            NegativeAttributes neg4 = RunicReforging.GetNegativeAttributes(item);
                            if (neg4 == null || neg4.Antique == 0)
                                return false;
                            break;
                        case Misc.NotAntique:
                            NegativeAttributes neg5 = RunicReforging.GetNegativeAttributes(item);
                            if (neg5 != null && neg5.Antique > 0)
                                return false;
                            break;
                    }
                }
                else if (o is string s)
                {
                    if (s == "WeaponVelocity" && (!(item is BaseRanged) || ((BaseRanged) item).Velocity < value))
                        return false;

                    if (s == "SearingWeapon" && (!(item is BaseWeapon) || !((BaseWeapon) item).SearingWeapon))
                        return false;

                    if (s == "ArtifactRarity" && (!(item is IArtifact) || ((IArtifact) item).ArtifactRarity < value))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool CheckSlayer(Item item, object o)
        {
            if (o is TalismanSlayerName name && name == TalismanSlayerName.Undead)
            {
                if (!(item is ISlayer) || ((ISlayer)item).Slayer != SlayerName.Silver && ((ISlayer)item).Slayer2 != SlayerName.Silver)
                {
                    if (!(item is BaseTalisman) || ((BaseTalisman)item).Slayer != TalismanSlayerName.Undead)
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (o is SlayerName slayerName && (!(item is ISlayer) || ((ISlayer)item).Slayer != slayerName && ((ISlayer)item).Slayer2 != slayerName))
                {
                    return false;
                }

                if (o is TalismanSlayerName talismanSlayerName && (!(item is BaseTalisman) || ((BaseTalisman)item).Slayer != talismanSlayerName))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CheckCanRepair(Item item)
        {
            NegativeAttributes neg = RunicReforging.GetNegativeAttributes(item);

            return neg != null && neg.NoRepair != 0;
        }

        private static bool CheckKeyword(string searchstring, Item item)
        {
            if (item is CommodityDeed deed && deed.Commodity != null)
            {
                item = deed.Commodity;
            }

            if (item is IResource resource)
            {
                string resName = CraftResources.GetName(resource.Resource);

                if (resName.ToLower().IndexOf(searchstring.ToLower()) >= 0)
                {
                    return true;
                }
            }

            if (item is ICommodity commodity)
            {
                string name = commodity.Description.String;

                if (string.IsNullOrEmpty(name) && commodity.Description.Number > 0)
                {
                    name = StringList.GetString(commodity.Description.Number);
                }

                if (!string.IsNullOrEmpty(name) && name.ToLower().IndexOf(searchstring.ToLower()) >= 0)
                {
                    return true;
                }
            }

            return Keywords.ContainsKey(searchstring.ToLower()) && Keywords[searchstring.ToLower()] == item.GetType();
        }

        public static bool IsGargoyle(Item item)
        {
            return Race.Gargoyle.ValidateEquipment(item);
        }

        public static bool IsElf(Item item)
        {
            return Race.Elf.ValidateEquipment(item);
        }

        public static SearchCriteria AddNewContext(PlayerMobile pm)
        {
            SearchCriteria criteria = new SearchCriteria();

            Contexts[pm] = criteria;

            return criteria;
        }

        public static SearchCriteria GetContext(PlayerMobile pm)
        {
            if (Contexts.ContainsKey(pm))
                return Contexts[pm];

            return null;
        }

        public static Dictionary<PlayerMobile, SearchCriteria> Contexts { get; set; }
        public static List<SearchCategory> Categories { get; set; }

        public static Dictionary<string, Type> Keywords { get; set; }

        public static void Configure()
        {
            EventSink.WorldSave += OnSave;
            EventSink.WorldLoad += OnLoad;
        }

        public static void OnSave(WorldSaveEventArgs e)
        {
            Persistence.Serialize(
                FilePath,
                writer =>
                {
                    writer.Write(0);

                    int count = 0;

                    foreach (var kvp in Contexts)
                    {
                        if (!kvp.Value.IsEmpty) count++;
                    }

                    writer.Write(Contexts == null ? 0 : count);

                    if (Contexts != null)
                    {
                        foreach (KeyValuePair<PlayerMobile, SearchCriteria> kvp in Contexts)
                        {
                            if (!kvp.Value.IsEmpty)
                            {
                                writer.Write(kvp.Key);
                                kvp.Value.Serialize(writer);
                            }
                        }
                    }
                });
        }

        public static void OnLoad()
        {
            Persistence.Deserialize(
                FilePath,
                reader =>
                {
                    int version = reader.ReadInt();
                    int count = reader.ReadInt();

                    for (int i = 0; i < count; i++)
                    {
                        PlayerMobile pm = reader.ReadMobile() as PlayerMobile;
                        SearchCriteria criteria = new SearchCriteria(reader);

                        if (pm != null)
                        {
                            if (Contexts == null)
                                Contexts = new Dictionary<PlayerMobile, SearchCriteria>();

                            Contexts[pm] = criteria;
                        }
                    }
                });
        }

        public static void Initialize()
        {
            CommandSystem.Register("GetOPLString", AccessLevel.Administrator, e =>
                {
                    e.Mobile.BeginTarget(-1, false, TargetFlags.None, (m, targeted) =>
                        {
                            if (targeted is Item item)
                            {
                                Console.WriteLine(GetItemName(item));
                                e.Mobile.SendMessage(GetItemName(item));
                            }
                        });
                });

            Categories = new List<SearchCategory>();

            if (Contexts == null)
            {
                Contexts = new Dictionary<PlayerMobile, SearchCriteria>();
            }

            Timer.DelayCall(TimeSpan.FromSeconds(1), () =>
                {
                    SearchCategory price = new SearchCategory(Category.PriceRange);
                    Categories.Add(price);

                    List<SearchCriteriaCategory> list = new List<SearchCriteriaCategory>();

                    for (var index = 0; index < SearchCriteriaCategory.AllCategories.Length; index++)
                    {
                        var category = SearchCriteriaCategory.AllCategories[index];

                        list.Add(category);
                    }

                    for (var index = 0; index < list.Count; index++)
                    {
                        var x = list[index];

                        SearchCategory cat = new SearchCategory(x.Category);

                        List<SearchCriterionEntry> list1 = new List<SearchCriterionEntry>();

                        for (var i = 0; i < x.Criteria.Length; i++)
                        {
                            var criterion = x.Criteria[i];

                            list1.Add(criterion);
                        }

                        for (var i = 0; i < list1.Count; i++)
                        {
                            var y = list1[i];

                            if (y.PropCliloc != 0)
                            {
                                cat.Register(y.Object, y.Cliloc, y.PropCliloc);
                            }
                            else
                            {
                                cat.Register(y.Object, y.Cliloc);
                            }
                        }

                        Categories.Add(cat);
                    }

                    SearchCategory sort = new SearchCategory(Category.Sort);
                    Categories.Add(sort);
                });

            Keywords = new Dictionary<string, Type>
            {
                ["power scroll"] = typeof(PowerScroll), ["stat scroll"] = typeof(StatCapScroll)
            };

        }

        public static string GetItemName(Item item)
        {
            if (StringList == null || item.Name != null)
            {
                return item.Name;
            }

            ObjectPropertyListPacket opl = new ObjectPropertyListPacket(item);
            item.GetProperties(opl);

            //since the object property list is based on a packet object, the property info is packed away in a packet format
            byte[] data = opl.UnderlyingStream.UnderlyingStream.ToArray();

            int index = 15; // First localization number index
            string basestring = null;

            //reset the number property
            uint number = 0;

            //if there's not enough room for another record, quit
            if (index + 4 >= data.Length)
            {
                return null;
            }

            //read number property from the packet data
            number = (uint)(data[index++] << 24 | data[index++] << 16 | data[index++] << 8 | data[index++]);

            //reset the length property
            ushort length = 0;

            //if there's not enough room for another record, quit
            if (index + 2 > data.Length)
            {
                return null;
            }

            //read length property from the packet data
            length = (ushort)(data[index++] << 8 | data[index++]);

            //determine the location of the end of the string
            int end = index + length;

            //truncate if necessary
            if (end >= data.Length)
            {
                end = data.Length - 1;
            }

            //read the string into a StringBuilder object

            StringBuilder s = new StringBuilder();
            while (index + 2 <= end + 1)
            {
                short next = (short)(data[index++] | data[index++] << 8);

                if (next == 0)
                {
                    break;
                }

                s.Append(Encoding.Unicode.GetString(BitConverter.GetBytes(next)));
            }

            basestring = StringList.GetString((int)number);
            string args = s.ToString();

            if (args == string.Empty)
            {
                return basestring;
            }

            string[] parms = args.Split('\t');

            try
            {
                if (parms.Length > 1)
                {
                    for (int i = 0; i < parms.Length; i++)
                    {
                        parms[i] = parms[i].Trim(' ');

                        if (parms[i].IndexOf("#") == 0)
                        {
                            parms[i] = StringList.GetString(Convert.ToInt32(parms[i].Substring(1, parms[i].Length - 1)));
                        }
                    }
                }
                else if (parms.Length == 1 && parms[0].IndexOf("#") == 0)
                {
                    parms[0] = StringList.GetString(Convert.ToInt32(args.Substring(1, parms[0].Length - 1)));
                }
            }
            catch
            {
                return null;
            }

            StringEntry entry = StringList.GetEntry((int)number);

            if (entry != null)
            {
                return entry.Format(parms);
            }

            return basestring;
        }

        private static List<Item> GetItems(Mobile pv)
        {
            List<Item> list = new List<Item>();

            for (var index = 0; index < pv.Items.Count; index++)
            {
                Item item = pv.Items[index];

                if (item.Movable && item != pv.Backpack && item.Layer != Layer.Hair && item.Layer != Layer.FacialHair)
                {
                    list.Add(item);
                }
            }

            if (pv.Backpack != null)
            {
                GetItems(pv.Backpack, list);
            }

            return list;
        }

        public static void GetItems(Container c, List<Item> list)
        {
            if (c == null || c.Items.Count == 0)
            {
                return;
            }

            for (var index = 0; index < c.Items.Count; index++)
            {
                Item item = c.Items[index];

                if (item is Container container && !IsSearchableContainer(container.GetType()))
                {
                    GetItems(container, list);
                }
                else
                {
                    list.Add(item);
                }
            }
        }

        public static bool CanSearch(Mobile m)
        {
            Region r = m.Region;

            if (r.GetLogoutDelay(m) == TimeSpan.Zero)
            {
                return true;
            }

            return r is GuardedRegion guardRegion && !guardRegion.Disabled || r is HouseRegion houseRegion && houseRegion.House.IsFriend(m);
        }

        private static bool IsSearchableContainer(Type type)
        {
            for (var index = 0; index < _SearchableContainers.Length; index++)
            {
                var t = _SearchableContainers[index];

                if (t == type || type.IsSubclassOf(t))
                {
                    return true;
                }
            }

            return false;
        }

        private static readonly Type[] _SearchableContainers =
        {
            typeof(BaseQuiver),         typeof(BaseResourceSatchel),
            typeof(FishBowl),           typeof(FirstAidBelt),
            typeof(Plants.SeedBox),     typeof(BaseSpecialScrollBook),
            typeof(GardenShedBarrel),   typeof(JewelryBox)
        };
    }

    public enum SortBy
    {
        LowToHigh,
        HighToLow
    }

    public enum Category
    {
        PriceRange,
        Misc,
        Equipment,
        Combat,
        Casting,
        DamageType,
        HitSpell,
        HitArea,
        Resists,
        Stats,
        Slayer1,
        Slayer2,
        Slayer3,
        RequiredSkill,
        Skill1,
        Skill2,
        Skill3,
        Skill4,
        Skill5,
        Skill6,
        Sort,
        Auction
    }

    public enum Misc
    {
        ExcludeFel,
        GargoyleOnly,
        NotGargoyleOnly,
        ElvesOnly,
        NotElvesOnly,
        FactionItem,
        PromotionalToken,
        Cursed,
        NotCursed,
        CannotRepair,
        NotCannotBeRepaired,
        Brittle,
        NotBrittle,
        Antique,
        NotAntique
    }

    public class SearchCategory
    {
        public Category Category { get; }
        public int Label => (int)Category;

        public List<Tuple<object, int, int>> Objects { get; }

        public SearchCategory(Category category)
        {
            Category = category;

            Objects = new List<Tuple<object, int, int>>();
        }

        public void Register(object o, int label)
        {
            Tuple<object, int, int> first = null;

            for (var index = 0; index < Objects.Count; index++)
            {
                var t = Objects[index];

                if (t.Item1 == o)
                {
                    first = t;
                    break;
                }
            }

            if (first == null)
            {
                Objects.Add(new Tuple<object, int, int>(o, label, 0));
            }
        }

        public void Register(object o, int label, int pcliloc)
        {
            Tuple<object, int, int> first = null;

            for (var index = 0; index < Objects.Count; index++)
            {
                var t = Objects[index];

                if (t.Item1 == o)
                {
                    first = t;
                    break;
                }
            }

            if (first == null)
            {
                Objects.Add(new Tuple<object, int, int>(o, label, pcliloc));
            }
        }
    }

    public class SearchCriteria
    {
        public Layer SearchType { get; set; }
        public string SearchName { get; set; }
        public SortBy SortBy { get; set; }
        public bool Auction { get; set; }
        public long MinPrice { get; set; }
        public long MaxPrice { get; set; }

        public bool EntryPrice { get; set; }

        public List<SearchDetail> Details { get; set; }

        public SearchCriteria()
        {
            Details = new List<SearchDetail>();

            MinPrice = 0;
            MaxPrice = 175000000;
            SearchType = Layer.Invalid;
        }

        public void Reset()
        {
            Details.Clear();
            Details.TrimExcess();
            Details = new List<SearchDetail>();

            MinPrice = 0;
            MaxPrice = 175000000;
            SortBy = SortBy.LowToHigh;
            Auction = false;
            SearchName = null;
            SearchType = Layer.Invalid;
            EntryPrice = false;
        }

        public int GetValueForDetails(object o)
        {
            SearchDetail detail = null;

            for (var index = 0; index < Details.Count; index++)
            {
                var d = Details[index];

                if (d.Attribute == o)
                {
                    detail = d;
                    break;
                }
            }

            return detail != null ? detail.Value : 0;
        }

        public void TryAddDetails(object o, int name, int propname, int value, Category cat)
        {
            SearchDetail d = null;

            for (var index = 0; index < Details.Count; index++)
            {
                var det = Details[index];

                if (det.Attribute == o)
                {
                    d = det;
                    break;
                }
            }

            if (o is Layer layerObject)
            {
                SearchDetail layer = null;

                for (var index = 0; index < Details.Count; index++)
                {
                    var det = Details[index];

                    if (det.Attribute is Layer attribute && attribute != layerObject)
                    {
                        layer = det;
                        break;
                    }
                }

                if (layer != null)
                {
                    Details.Remove(layer);
                }

                Details.Add(new SearchDetail(layerObject, name, propname, value, cat));
                SearchType = layerObject;
            }
            else if (d == null)
            {
                d = new SearchDetail(o, name, propname, value, cat);

                Details.Add(d);
            }
            else if (d.Value != value)
            {
                d.Value = value;
            }

            /*if (d.Attribute is TalismanSlayerName && (TalismanSlayerName)d.Attribute == TalismanSlayerName.Undead)
            {
                TryAddDetails(SlayerName.Silver, name, value, cat);
            }*/
        }

        public bool IsEmpty => Details.Count == 0 && !EntryPrice && string.IsNullOrEmpty(SearchName) && SearchType == Layer.Invalid;

        public SearchCriteria(GenericReader reader)
        {
            int version = reader.ReadInt();

            Details = new List<SearchDetail>();

            if (version > 1)
                Auction = reader.ReadBool();

            if (version != 0)
                EntryPrice = reader.ReadBool();

            SearchType = (Layer)reader.ReadInt();
            SearchName = reader.ReadString();
            SortBy = (SortBy)reader.ReadInt();
            MinPrice = reader.ReadLong();
            MaxPrice = reader.ReadLong();

            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                Details.Add(new SearchDetail(reader));
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(2);

            writer.Write(Auction);
            writer.Write(EntryPrice);
            writer.Write((int)SearchType);
            writer.Write(SearchName);
            writer.Write((int)SortBy);
            writer.Write(MinPrice);
            writer.Write(MaxPrice);

            writer.Write(Details.Count);

            for (int i = 0; i < Details.Count; i++)
            {
                Details[i].Serialize(writer);
            }
        }
    }

    public class SearchDetail
    {
        public enum AttributeID
        {
            None = 0,
            AosAttribute,
            AosArmorAttribute,
            AosWeaponAttribute,
            AosElementAttribute,
            SkillName,
            SAAbosorptionAttribute,
            ExtendedWeaponAttribute,
            NegativeAttribute,
            SlayerName,
            String,
            TalismanSlayerName,
            TalismanSkill,
            TalismanRemoval,
            Int
        }

        public object Attribute { get; set; }
        public int Label { get; }
        public int PropLabel { get; }
        public int Value { get; set; }
        public Category Category { get; }

        public SearchDetail(object o, int label, int proplabel, int value, Category category)
        {
            Attribute = o;
            Label = label;
            PropLabel = proplabel;
            Value = value;
            Category = category;
        }

        public SearchDetail(GenericReader reader)
        {
            int version = reader.ReadInt(); // version

            if (version > 0)
            {
                PropLabel = reader.ReadInt();
            }

            ReadAttribute(reader);

            Label = reader.ReadInt();
            Value = reader.ReadInt();
            Category = (Category)reader.ReadInt();
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(1);

            writer.Write(PropLabel);

            WriteAttribute(writer);

            writer.Write(Label);
            writer.Write(Value);
            writer.Write((int)Category);
        }

        private void WriteAttribute(GenericWriter writer)
        {
            int attrID = GetAttributeID(Attribute);
            writer.Write(attrID);

            switch (attrID)
            {
                case 0: break;
                case 1: writer.Write((int)(AosAttribute)Attribute); break;
                case 2: writer.Write((int)(AosArmorAttribute)Attribute); break;
                case 3: writer.Write((int)(AosWeaponAttribute)Attribute); break;
                case 4: writer.Write((int)(AosElementAttribute)Attribute); break;
                case 5: writer.Write((int)(SkillName)Attribute); break;
                case 6: writer.Write((int)(SAAbsorptionAttribute)Attribute); break;
                case 7: writer.Write((int)(ExtendedWeaponAttribute)Attribute); break;
                case 8: writer.Write((int)(NegativeAttribute)Attribute); break;
                case 9: writer.Write((int)(SlayerName)Attribute); break;
                case 10: writer.Write((string)Attribute); break;
                case 11: writer.Write((int)(TalismanSlayerName)Attribute); break;
                case 12: writer.Write((int)(TalismanSkill)Attribute); break;
                case 13: writer.Write((int)(TalismanRemoval)Attribute); break;
                case 14: writer.Write((int)Attribute); break;
            }
        }

        private void ReadAttribute(GenericReader reader)
        {
            switch (reader.ReadInt())
            {
                case 0: break;
                case 1: Attribute = (AosAttribute)reader.ReadInt(); break;
                case 2: Attribute = (AosArmorAttribute)reader.ReadInt(); break;
                case 3: Attribute = (AosWeaponAttribute)reader.ReadInt(); break;
                case 4: Attribute = (AosElementAttribute)reader.ReadInt(); break;
                case 5: Attribute = (SkillName)reader.ReadInt(); break;
                case 6: Attribute = (SAAbsorptionAttribute)reader.ReadInt(); break;
                case 7: Attribute = (ExtendedWeaponAttribute)reader.ReadInt(); break;
                case 8: Attribute = (NegativeAttribute)reader.ReadInt(); break;
                case 9: Attribute = (SlayerName)reader.ReadInt(); break;
                case 10: Attribute = reader.ReadString(); break;
                case 11: Attribute = (TalismanSlayerName)reader.ReadInt(); break;
                case 12: Attribute = (TalismanSkill)reader.ReadInt(); break;
                case 13: Attribute = (TalismanRemoval)reader.ReadInt(); break;
                case 14: Attribute = reader.ReadInt(); break;
            }
        }

        public static int GetAttributeID(object o)
        {
            if (o is AosAttribute)
                return (int)AttributeID.AosAttribute;

            if (o is AosArmorAttribute)
                return (int)AttributeID.AosArmorAttribute;

            if (o is AosWeaponAttribute)
                return (int)AttributeID.AosWeaponAttribute;

            if (o is AosElementAttribute)
                return (int)AttributeID.AosElementAttribute;

            if (o is SkillName)
                return (int)AttributeID.SkillName;

            if (o is SAAbsorptionAttribute)
                return (int)AttributeID.SAAbosorptionAttribute;

            if (o is ExtendedWeaponAttribute)
                return (int)AttributeID.ExtendedWeaponAttribute;

            if (o is NegativeAttribute)
                return (int)AttributeID.NegativeAttribute;

            if (o is SlayerName)
                return (int)AttributeID.SlayerName;

            if (o is TalismanSlayerName)
                return (int)AttributeID.TalismanSlayerName;

            if (o is string)
                return (int)AttributeID.String;

            if (o is TalismanSkill)
                return (int)AttributeID.TalismanSkill;

            if (o is TalismanRemoval)
                return (int)AttributeID.TalismanRemoval;

            if (o is int)
                return (int)AttributeID.Int;

            return (int)AttributeID.None;
        }

    }

    public class SearchVendors : ContextMenuEntry
    {
        public PlayerMobile Player { get; }

        public SearchVendors(PlayerMobile pm)
            : base(1154679, -1)
        {
            Player = pm;

            Enabled = VendorSearch.CanSearch(pm);
        }

        public override void OnClick()
        {
            if (VendorSearch.CanSearch(Player))
            {
                BaseGump.SendGump(new VendorSearchGump(Player));
            }
        }
    }

    public class SearchItem
    {
        public PlayerVendor Vendor { get; }
        public IAuctionItem AuctionSafe { get; }
        public Item Item { get; }
        public int Price { get; }
        public bool IsChild { get; }
        public bool IsAuction { get; }

        public Map Map => Vendor != null ? Vendor.Map : AuctionSafe != null ? AuctionSafe.Map : null;

        public SearchItem(PlayerVendor vendor, Item item, int price, bool isChild)
        {
            Vendor = vendor;
            Item = item;
            Price = price;
            IsChild = isChild;
            IsAuction = false;
        }

        public SearchItem(IAuctionItem auctionsafe, Item item, int price, bool isChild)
        {
            AuctionSafe = auctionsafe;
            Item = item;
            Price = price;
            IsChild = isChild;
            IsAuction = true;
        }
    }
}
