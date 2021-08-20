using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;

namespace SaberMod
{
    public class ItemModuleElectroStaff : ItemModule
    {

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            if (item.gameObject.GetComponent<ItemElectroStaff>() == null)
            {
                item.gameObject.AddComponent<ItemElectroStaff>();
            }
        }

        public ItemModuleElectroStaff() : base()
        {
            return;
        }
    }
}
