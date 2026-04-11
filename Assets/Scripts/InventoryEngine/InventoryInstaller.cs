using InventoryEngine.Items;
using UnityEngine;
using Zenject;

namespace InventoryEngine
{
    /// <summary>
    /// Установка инвенторей
    /// </summary>
    public class InventoryInstaller : MonoInstaller
    {
        [SerializeField] private Inventory _achievement;
        
        public override void InstallBindings()
        {
            Container.Bind(typeof(IRecipientInventoryItem<InventoryItem>),
                                            typeof(IInventoryEvent), 
                                            typeof(IInventoryItems))
                                        .WithId("AchievementsInventory")
                                        .FromInstance(_achievement)
                                        .AsSingle();
        }
    }
}