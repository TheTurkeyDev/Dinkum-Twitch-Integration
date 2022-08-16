using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace DinkumTwitchIntegration
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static readonly System.Random RAND = new System.Random();
        public static ManualLogSource logger;
        public static ConcurrentQueue<RewardData> rewardsQueue = new ConcurrentQueue<RewardData>();

        private void Awake()
        {
            // Plugin startup logic
            logger = Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            IntegationSocket.Start("core-keeper", 23491);
        }

        private void Update()
        {
            DateTime currentTime = DateTime.UtcNow;
            if (rewardsQueue.TryDequeue(out RewardData reward))
            {
                if ((currentTime - reward.added).TotalMilliseconds < reward.delay)
                {
                    rewardsQueue.Enqueue(reward);
                }
                else
                {
                    string userName = (string)reward.data["metadata"]["user"];
                    var values = reward.data["values"];
                    Vector3 targetPos = NetworkMapSharer.share.localChar.myInteract.tileHighlighter.transform.position;
                    targetPos.y = ((Component)(object)NetworkMapSharer.share.localChar).transform.position.y;
                    Vector3 playerPos = ((Component)(object)NetworkMapSharer.share.localChar).transform.position;
                    switch (reward.action)
                    {
                        case "InventoryBomb":
                            int radius = (int)(values["radius"] ?? 7);
                            foreach (var slot in Inventory.inv.invSlots)
                            {
                                if (slot.stack == 0)
                                    continue;
                                var pos = GetAvailableGroundPos(slot, playerPos, radius);
                                if (pos != null)
                                {
                                    NetworkMapSharer.share.localChar.CmdDropItem(slot.itemNo, slot.stack, playerPos, pos ?? targetPos);
                                    slot.updateSlotContentsAndRefresh(-1, 0);
                                    Inventory.inv.equipNewSelectedSlot();
                                }
                            }
                            break;
                        /*case "Test":
                            Logger.LogInfo("=== Items ===");
                            for (int index = 0; index < Mobs; ++index)
                                Logger.LogInfo($"{Inventory.inv.allItems[index].getItemId()}: {Inventory.inv.allItems[index].itemName}");
                            break;*/
                        case "SpawnItem":
                            int itemNo = (int)(values["itemNo"] ?? 0);
                            int count = (int)(values["count"] ?? 1);
                            var item = GetItemFromId(itemNo);
                            if (item?.isATool ?? false)
                                count = 10;
                            NetworkMapSharer.share.localChar.CmdDropItem(itemNo, count, playerPos, targetPos);
                            break;
                        case "ChatMessage":
                            string chatMsg = (string)(values["message"] ?? "");
                            NetworkMapSharer.share.localChar.CmdSendChatMessage(chatMsg);
                            break;
                        case "ChangeAppearence":
                            var equipItemToChar = NetworkMapSharer.share.localChar.myEquip;
                            equipItemToChar.CmdChangeSkin(RAND.Next(CharacterCreatorScript.create.skinTones.Length));
                            equipItemToChar.CmdChangeNose(RAND.Next(CharacterCreatorScript.create.noseMeshes.Length));
                            equipItemToChar.CmdChangeHairColour(RAND.Next(CharacterCreatorScript.create.allHairColours.Length));
                            equipItemToChar.CmdChangeHairId(RAND.Next(CharacterCreatorScript.create.allHairStyles.Length));
                            equipItemToChar.CmdChangeMouth(RAND.Next(CharacterCreatorScript.create.mouthTypes.Length));
                            equipItemToChar.CmdChangeEyes(RAND.Next(CharacterCreatorScript.create.allEyeTypes.Length), RAND.Next(CharacterCreatorScript.create.eyeColours.Length));
                            equipItemToChar.CmdChangeShirtId(CharacterCreatorScript.create.allShirts[RAND.Next(CharacterCreatorScript.create.allShirts.Length)].getItemId());
                            break;
                    }
                }
            }
        }

        private InventoryItem GetItemFromId(int id)
        {
            for (int i = 0; i < Inventory.inv.allItems.Length; i++)
            {
                if (Inventory.inv.allItems[i].getItemId() == id)
                {
                    return Inventory.inv.allItems[i];
                }
            }

            return null;
        }

        private Vector3? GetAvailableGroundPos(InventorySlot slot, Vector3 startPos, int radius)
        {
            var position = new Vector3(startPos.x, startPos.y, startPos.z);

            do
            {
                var xx = RAND.Next((radius * 2) + 1) - radius;
                var zz = RAND.Next((radius * 2) + 1) - radius;
                position += new Vector3(xx, 0, zz);
            }
            while (!checkIfDropCanFitOnGround(slot, position));

            return position;
        }

        private bool checkIfDropCanFitOnGround(InventorySlot slot, Vector3 position)
        {
            return WorldManager.manageWorld.checkIfDropCanFitOnGround(slot.itemNo, slot.stack, position, NetworkMapSharer.share.localChar.myInteract.insideHouseDetails);
        }
    }
}
