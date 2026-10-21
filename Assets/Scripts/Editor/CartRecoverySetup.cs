using GameFoundation.Base;
using GameFoundation.Quests;
using GameFoundation.Localization;
using UnityEditor;

public static class CartRecoverySetup
{
    public static void Apply()
    {
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        void Add(string key,string ru,string en)
        {
            var entry=table.entries.Find(e=>e.key==key);if(entry==null){entry=new LocalizationTable.Entry{key=key};table.entries.Add(entry);}entry.values=new(){ru,en};
        }
        Add("royal.cart_recovery",CartRecoveryService.RescueText,"Knight, how did you manage to lose your very last cart? Not even a wheel left! Never mind, keep your chin up. I found a couple of carts for you through the royal scribe. But he puts even kindness in his ledger: each cart adds one unit of magical ore to your next royal tribute.");
        Add("royal.cart_debt",RoyalTributeService.DebtLineTemplate,"And another {0} units of magical ore for the carts supplied to you. Royal assistance must be accounted for, too.");
        Add("royal.month_paid","Подать получена: {0} единиц магической руды. Следующая норма – тридцать единиц через тридцать дней. Не заставляйте корону напоминать дважды.","Tribute received: {0} units of magical ore. The next quota is thirty units in thirty days. Do not make the Crown remind you twice.");
        Add("royal.month_failed","К сроку вы не собрали {0} единиц магической руды. Корона взыскивает оговорённый штраф. Новая норма – тридцать единиц через тридцать дней. Служба продолжается.","You failed to gather {0} units of magical ore by the deadline. The Crown imposes the agreed penalty. The new quota is thirty units in thirty days. Your service continues.");
        Add("royal.month_status","Подготовь {0} единиц магической руды. Писарь заберёт подать в установленный срок.","Prepare {0} units of magical ore. The scribe will collect the tribute on its due date.");
        var quest=AssetDatabase.LoadAssetAtPath<QuestDefinition>(RoyalTributeSetup.QuestPath);
        quest.description="Каждые тридцать дней королевский писарь собирает тридцать единиц магической руды. К ближайшей подати прибавляется плата за телеги, полученные у короны. За исполнение положено десять влияния. За просрочку корона конфискует припасы, материалы и оружие, а влияние уменьшится на тридцать. Людей и телеги оставят. После расчёта сразу начинается новый тридцатидневный срок.";
        Add("royal.month_description",quest.description,"Every thirty days, the royal scribe collects thirty units of magical ore. Carts supplied by the Crown add a surcharge to the next tribute. Meeting the quota earns ten influence. Missing the deadline means confiscation of supplies, materials and weapons, and a loss of thirty influence. People and carts are spared. A new thirty-day term begins immediately after settlement.");
        EditorUtility.SetDirty(quest);EditorUtility.SetDirty(table);AssetDatabase.SaveAssets();
    }
}
