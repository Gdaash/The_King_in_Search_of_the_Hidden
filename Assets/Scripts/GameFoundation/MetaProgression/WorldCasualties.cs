using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    public static class WorldCasualties
    {
        private static readonly Dictionary<ResourceType,int> dead=new(), missing=new();
        private static readonly HashSet<int> deaths=new();
        private static bool active;
        public static IReadOnlyDictionary<ResourceType,int> Dead=>dead;
        public static IReadOnlyDictionary<ResourceType,int> Missing=>missing;
        public static bool HasLosses=>dead.Count>0||missing.Count>0;
        public static void Begin(){dead.Clear();missing.Clear();deaths.Clear();active=true;}
        public static void End()=>active=false;
        public static void Death(ResourceType type,GameObject unit)
        {
            if(!active||type==null||unit==null||!deaths.Add(unit.GetEntityId().GetHashCode()))return;
            dead.TryGetValue(type,out int count);dead[type]=count+1;
        }
        public static void DidNotReturn(ResourceType type,int count)
        {if(!active||type==null||count<=0)return;missing.TryGetValue(type,out int before);missing[type]=before+count;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){Begin();End();}
    }
    public sealed class WorldLossIdentity:MonoBehaviour
    {
        public ResourceType resource;
        public void RecordDeath()=>WorldCasualties.Death(resource,gameObject);
    }
}
