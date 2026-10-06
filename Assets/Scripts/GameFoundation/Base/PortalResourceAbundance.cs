using UnityEngine;

namespace GameFoundation.Base
{
    public static class PortalResourceAbundance
    {
        public static Color ColorFor(string key)
        {
            float amount = key switch
            {
                "base.portal.abundance.absent" => 0f,
                "base.portal.abundance.rare" => .2f,
                "base.portal.abundance.few" => .4f,
                "base.portal.abundance.sometimes" => .5f,
                "base.portal.abundance.average" => .7f,
                "base.portal.abundance.many" => 1f,
                _ => -1f
            };
            if (amount < 0f) return new Color(.94f, .90f, .79f);
            var low = new Color(1f, .36f, .38f);
            var middle = new Color(1f, .80f, .36f);
            var high = new Color(.43f, .86f, .46f);
            return amount <= .5f ? Color.Lerp(low, middle, amount * 2f) :
                Color.Lerp(middle, high, (amount - .5f) * 2f);
        }
    }
}
