using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using GameFoundation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    /// <summary>Night forecast and prepaid workshop orders share the confirmation window.</summary>
    public sealed class NextDayEconomyView : MonoBehaviour
    {
        private RectTransform night, workshops;
        private ScrollRect nightScroll, orderScroll;
        private Text nightTitle, workshopTitle, note;
        private readonly Text[] nightLabels = new Text[4], nightAmounts = new Text[4];
        private readonly GameObject[] orders = new GameObject[CraftProduction.WorkshopCount];
        private readonly Text[] remaining = new Text[CraftProduction.WorkshopCount];
        private readonly Image[,] icons = new Image[CraftProduction.WorkshopCount, 3];
        private readonly Text[,] amounts = new Text[CraftProduction.WorkshopCount, 3];
        private readonly Text[] equals = new Text[CraftProduction.WorkshopCount];
        private readonly Text[] plus = new Text[CraftProduction.WorkshopCount];
        private Font font;
        private static readonly Color Ink = new(.94f,.90f,.81f), Muted = new(.69f,.64f,.75f), Green = new(.55f,.85f,.54f);

        public static void Refresh(GameObject popup)
        {
            if (popup == null) return;
            var view = popup.GetComponent<NextDayEconomyView>() ?? popup.AddComponent<NextDayEconomyView>();
            view.RefreshData();
        }

        private void Build()
        {
            font = transform.Find("Title").GetComponent<Text>().font;
            night = Panel("Night economy", -286);
            workshops = Panel("Workshop economy", 286);
            nightTitle = Label(night, "Night heading", 536, 36, 0, 219, 24);
            workshopTitle = Label(workshops, "Workshop heading", 536, 36, 0, 219, 24);
            nightTitle.color = workshopTitle.color = Muted;
            nightScroll = PortalExpeditionPanel.CreateVerticalScroll(night, "Night rows", 540, 420, -20);
            orderScroll = PortalExpeditionPanel.CreateVerticalScroll(workshops, "Order rows", 540, 420, -20);
            for (int i = 0; i < 4; i++)
            {
                var row = Row(nightScroll.content, i);
                nightLabels[i] = Label(row, "Label", 380, 58, -64, 0, 22);
                var icon = Rect(row, "Icon", 48, 48, 176, 0).gameObject.AddComponent<Image>();
                ResourceIconSizing.Apply(icon, ResourceCatalog.Find(i == 0 ? "Chicken" : "Berry")?.resourceIcon);
                icon.raycastTarget = false;
                nightAmounts[i] = Label(row, "Amount", 56, 58, 234, 0, 22);
                nightAmounts[i].color = Green;
            }
            for (int i = 0; i < orders.Length; i++)
            {
                var row = Row(orderScroll.content, i);
                orders[i] = row.gameObject;
                for (int j = 0; j < 3; j++)
                {
                    float x = -212 + 118 * j;
                    icons[i,j] = Rect(row, "Resource", 48, 48, x, 8).gameObject.AddComponent<Image>();
                    icons[i,j].raycastTarget = false;
                    amounts[i,j] = Label(row, "Amount", 38, 36, x + 42, 8, 22);
                }
                plus[i] = Label(row, "Plus", 24, 36, -136, 8, 22);
                plus[i].text = "+";
                equals[i] = Label(row, "Equals", 24, 36, -18, 0, 22);
                equals[i].text = "=";
                remaining[i] = Label(row, "Days", 160, 64, 182, 0, 22);
                remaining[i].alignment = TextAnchor.MiddleCenter;
            }
            note = Label(transform, "Night production note", 1096, 56, 0, -294, 22);
            note.alignment = TextAnchor.MiddleCenter;
        }

        private void RefreshData()
        {
            if (night == null) Build();
            nightTitle.text = GameText.Get("За день");
            workshopTitle.text = GameText.Get("Заказы мастерских");
            note.text = GameText.Get("Ночью мастерские не работают. Материалы заказов уже списаны.\nОдин день производства проходит за завершённую вылазку.");
            var p = HousingDistrict.Forecast();
            string[] names = { "Новые цыплята", "Еда от яиц", "Еда от лишних цыплят", "Еда с полей" };
            int[] values = { p.ChicksKept, p.Eggs, p.Chicks - p.ChicksKept, p.FieldFood };
            for (int i = 0; i < 4; i++)
            {
                nightLabels[i].transform.parent.gameObject.SetActive(values[i] > 0);
                nightLabels[i].text = GameText.Get(names[i]);
                nightAmounts[i].text = "+" + values[i];
            }
            int visible = 0;
            for (int i = 0; i < orders.Length; i++)
            {
                var shop = CraftProduction.Read(i);
                var order = shop.orders.Count == 0 ? null : shop.orders[0];
                orders[i].SetActive(order != null);
                if (order == null) continue;
                ((RectTransform)orders[i].transform).anchoredPosition = new Vector2(0, -40 - visible++ * 84);
                SetResource(i, 0, order.input, order.price);
                SetResource(i, 1, order.fuel, order.fuelCost);
                SetResource(i, 2, order.resource, order.amount);
                plus[i].gameObject.SetActive(order.fuelCost > 0);
                remaining[i].text = DaysText(order.days);
                LayoutFormula(i, order.fuelCost > 0);
            }
            orderScroll.content.sizeDelta = new Vector2(0, visible * 84);
            nightScroll.content.sizeDelta = new Vector2(0, 4 * 84);
            Layout(GetComponent<ForestForagingView>()?.block.activeSelf == true);
        }

        private void SetResource(int row, int column, string id, int count)
        {
            var resource = string.IsNullOrEmpty(id) ? null : ResourceCatalog.Find(id);
            icons[row,column].gameObject.SetActive(resource != null && count > 0);
            ResourceIconSizing.Apply(icons[row,column], resource?.resourceIcon);
            amounts[row,column].rectTransform.anchoredPosition = new Vector2(icons[row,column].rectTransform.anchoredPosition.x + icons[row,column].rectTransform.sizeDelta.x * .5f + 27, 8);
            amounts[row,column].text = resource != null && count > 0 ? (column == 2 ? "+" : "") + count : "";
            amounts[row,column].color = column == 2 ? Green : Ink;
        }

        private static string DaysText(int days)
        {
            string source = days % 10 == 1 && days % 100 != 11 ? "Осталось:\n{0} день" :
                days % 10 >= 2 && days % 10 <= 4 && (days % 100 < 12 || days % 100 > 14) ? "Осталось:\n{0} дня" : "Осталось:\n{0} дней";
            return GameText.Format(source, days);
        }

        private static float TextWidth(Text label)
        {
            var display = label.GetComponentInChildren<TMPro.TMP_Text>();
            float width = display != null ? display.GetPreferredValues(label.text, Mathf.Infinity, Mathf.Infinity).x : label.preferredWidth;
            return Mathf.Ceil(width) + 4;
        }

        private static void CenterResourcePair(RectTransform icon, Text amount, float y)
        {
            float width = TextWidth(amount), iconWidth = icon.rect.width;
            float left = -(iconWidth + 12 + width) * .5f;
            icon.anchoredPosition = new Vector2(left + iconWidth*.5f,y);
            amount.rectTransform.sizeDelta = new Vector2(width,48);
            amount.alignment = TextAnchor.MiddleLeft;
            amount.rectTransform.anchoredPosition = new Vector2(left + iconWidth + 12 + width*.5f,y);
        }

        private void LayoutFormula(int row, bool fuel)
        {
            float x = -254;
            for (int j = 0; j < 3; j++)
            {
                if (j == 1 && !fuel) continue;
                if (j > 0)
                {
                    var sign = j == 1 ? plus[row] : equals[row];
                    sign.alignment = TextAnchor.MiddleCenter;
                    sign.rectTransform.anchoredPosition = new Vector2(x+18,0);
                    x += 36;
                }
                var icon = icons[row,j].rectTransform;
                icon.anchoredPosition = new Vector2(x+icon.rect.width*.5f,0);
                x += icon.rect.width + 8;
                float textWidth = TextWidth(amounts[row,j]);
                amounts[row,j].rectTransform.sizeDelta = new Vector2(textWidth,48);
                amounts[row,j].rectTransform.anchoredPosition = new Vector2(x+textWidth*.5f,0);
                x += textWidth;
            }
        }

        public void Layout(bool foraging)
        {
            if (night == null) return;
            int dailyRows = ArrangeRows(nightScroll), orderRows = ArrangeRows(orderScroll);
            int rows = Mathf.Min(5, Mathf.Max(dailyRows, orderRows));
            bool hasEconomy = rows > 0;
            night.gameObject.SetActive(dailyRows > 0);
            workshops.gameObject.SetActive(orderRows > 0);
            note.gameObject.SetActive(hasEconomy);
            transform.Find("Close")?.gameObject.SetActive(false);
            float listHeight = rows * 84;
            float panelHeight = listHeight + 80;
            float height = (hasEconomy ? 406 + panelHeight : 350) + (foraging ? 248 : 0);
            float top = height * .5f;
            Place("Artwork Frame", 1280, height, 0, 0);
            Place("Title", 1088, 50, 0, top - 56);
            Place("Description", 1200, 126, 0, top - 148);
            var forecast = transform.Find("Description");
            for (int i = 0; i < forecast.childCount; i++)
            {
                var row = (RectTransform)forecast.GetChild(i);
                // Match the visible border, inset from the panel sprite's rect.
                row.sizeDelta = new Vector2(274,126);
                row.anchoredPosition = new Vector2(-459 + 306*i,0);
                var label = (RectTransform)row.Find("Label");
                label.sizeDelta = new Vector2(266,44);
                var amount = (RectTransform)row.Find("Amount");
                CenterResourcePair((RectTransform)row.Find("Icon"), amount.GetComponent<Text>(), -24);
            }
            night.sizeDelta = workshops.sizeDelta = new Vector2(588,panelHeight);
            float panelY = top - 222 - panelHeight*.5f;
            night.anchoredPosition = new Vector2(orderRows > 0 ? -306 : 0, panelY);
            workshops.anchoredPosition = new Vector2(dailyRows > 0 ? 306 : 0, panelY);
            nightTitle.rectTransform.anchoredPosition = workshopTitle.rectTransform.anchoredPosition = new Vector2(0,panelHeight*.5f-31);
            ConfigureScroll(nightScroll,listHeight);
            ConfigureScroll(orderScroll,listHeight);
            float panelBottom = top - 222 - panelHeight;
            note.rectTransform.anchoredPosition = new Vector2(0,panelBottom - 52);
            Place("Forest Foraging", 1096,224,0,hasEconomy ? panelBottom - 216 : top - 347);
            Place("Confirm", 340,56,-182,-top+52);
            Place("Cancel", 340,56,182,-top+52);
            // The optional foraging section must not push a five-row window off screen.
            var canvas = GetComponentInParent<Canvas>()?.rootCanvas.transform as RectTransform;
            float fit = canvas == null ? 1 : Mathf.Min(1, (canvas.rect.height - 120) / height,
                (canvas.rect.width - 64) / 1280);
            foreach (string name in new[] { "Artwork Frame", "Title", "Description", "Night economy", "Workshop economy",
                "Night production note", "Forest Foraging", "Confirm", "Cancel" })
            {
                var rect = transform.Find(name) as RectTransform;
                if (rect == null) continue;
                rect.localScale = new Vector3(fit,fit,1);
                rect.anchoredPosition *= fit;
            }
        }

        private static int ArrangeRows(ScrollRect scroll)
        {
            int count = 0;
            foreach (RectTransform row in scroll.content)
            {
                if (!row.gameObject.activeSelf) continue;
                row.anchoredPosition = new Vector2(0,-40-count*84);
                if (row.TryGetComponent<Image>(out var background))
                    background.color = count%2 == 0 ? new Color(.16f,.13f,.19f) : new Color(.10f,.08f,.12f);
                count++;
            }
            scroll.content.sizeDelta = new Vector2(0,count*84);
            return count;
        }

        private static void ConfigureScroll(ScrollRect scroll, float height)
        {
            ((RectTransform)scroll.transform).sizeDelta = new Vector2(540,height);
            bool overflow = scroll.content.sizeDelta.y > height;
            scroll.viewport.offsetMax = new Vector2(overflow ? -16 : 0,0);
            scroll.vertical = overflow;
            ((RectTransform)scroll.verticalScrollbar.transform).anchoredPosition = new Vector2(266,0);
            ((RectTransform)scroll.verticalScrollbar.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
            if (!overflow) scroll.content.anchoredPosition = Vector2.zero;
        }

        private void Place(string name, float w, float h, float x, float y)
        {
            var r = transform.Find(name) as RectTransform;
            if (r == null) return;
            r.sizeDelta = new Vector2(w,h); r.anchoredPosition = new Vector2(x,y);
        }
        private RectTransform Panel(string name, float x)
        {
            var r = Rect(transform,name,588,500,x,-42);
            var image = r.gameObject.AddComponent<Image>();
            var source = transform.Find("Artwork Frame").GetComponent<Image>();
            image.sprite = source.sprite; image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            image.raycastTarget = false;
            return r;
        }
        private RectTransform Row(Transform parent, int index)
        {
            var r = Rect(parent,"Economy row",540,80,0,-40-index*84);
            r.anchorMin = r.anchorMax = new Vector2(.5f,1);
            var image = r.gameObject.AddComponent<Image>();
            image.color = index%2 == 0 ? new Color(.16f,.13f,.19f) : new Color(.10f,.08f,.12f);
            image.raycastTarget = false;
            return r;
        }
        private Text Label(Transform parent,string name,float w,float h,float x,float y,int size)
        {
            var t = Rect(parent,name,w,h,x,y).gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.color = Ink; t.alignment = TextAnchor.MiddleLeft;
            t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.gameObject.AddComponent<QuestReadingText>();
            return t;
        }
        private static RectTransform Rect(Transform parent,string name,float w,float h,float x,float y)
        {
            var r = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent,false); r.sizeDelta = new Vector2(w,h); r.anchoredPosition = new Vector2(x,y);
            return r;
        }
    }
}
