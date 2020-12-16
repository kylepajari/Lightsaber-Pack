using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace SaberMod
{
    public class ItemModuleSaber : ItemModule
    {

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            if (item.gameObject.GetComponent<ItemSaber>() == null)
            {
                item.gameObject.AddComponent<ItemSaber>();
            }
        }

        public ItemModuleSaber() : base()
        {
            return;
        }
    }
}
