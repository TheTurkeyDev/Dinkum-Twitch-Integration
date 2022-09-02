using BepInEx;
using BepInEx.Logging;
using DinkumTwitchIntegration;
using HarmonyLib;
using Mirror;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace DinkumTwitchIntegration
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static readonly System.Random RAND = new System.Random();
        public static ManualLogSource logger;
        public static ConcurrentQueue<RewardData> rewardsQueue = new ConcurrentQueue<RewardData>();
        public static Dictionary<int, CustomLetterTemplate> letters = new Dictionary<int, CustomLetterTemplate>();
        private static int nextId = -200;

        private void Awake()
        {
            // Plugin startup logic
            var harmony = new Harmony("dev.theturkey.dinkumintegration");
            harmony.PatchAll();
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
                    string message = (string)reward.data["metadata"]["message"];
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
                            int count = (int)(values["count"] ?? 20000);
                            NetworkMapSharer.share.localChar.CmdDropItem(itemNo, count, playerPos, targetPos);
                            break;
                        case "ChatMessage":
                            string chatMsg = (string)(values["message"] ?? "");
                            ChatBox.chat.chatOpen = true;
                            ChatBox.chat.chatBox.text = chatMsg;
                            ChatBox.chat.sendEmote(RAND.Next(8));
                            //NetworkMapSharer.share.localChar.CmdSendChatMessage(chatMsg);
                            break;
                        case "ChangeAppearence":
                            var equipItemToChar = NetworkMapSharer.share.localChar.myEquip;
                            equipItemToChar.CmdChangeSkin(RAND.Next(CharacterCreatorScript.create.skinTones.Length));
                            equipItemToChar.CmdChangeNose(RAND.Next(CharacterCreatorScript.create.noseMeshes.Length));
                            equipItemToChar.CmdChangeHairColour(RAND.Next(CharacterCreatorScript.create.allHairColours.Length));
                            equipItemToChar.CmdChangeHairId(RAND.Next(CharacterCreatorScript.create.allHairStyles.Length));
                            equipItemToChar.CmdChangeMouth(RAND.Next(CharacterCreatorScript.create.mouthTypes.Length));
                            equipItemToChar.CmdChangeEyes(RAND.Next(CharacterCreatorScript.create.allEyeTypes.Length), RAND.Next(CharacterCreatorScript.create.eyeColours.Length));

                            /*equipItemToChar.CmdChangeShirtId(RandomObjectGenerator.generate.getRandomShirtOrDressForShop(false).getItemId());
                            equipItemToChar.CmdChangeHeadId(RandomObjectGenerator.generate.getRandomHat(false).getItemId());
                            equipItemToChar.CmdChangePantsId(RandomObjectGenerator.generate.getRandomPants(false).getItemId());
                            equipItemToChar.CmdChangeShoesId(RandomObjectGenerator.generate.getRandomShoes(false).getItemId());
                            equipItemToChar.CmdChangeFaceId(RandomObjectGenerator.generate.getRandomFaceItem(false).getItemId());*/
                            break;
                        case "ShuffleInventory":
                            ShuffleInv(false);
                            break;
                        case "ShuffleHotbar":
                            ShuffleInv(true);
                            break;
                        case "SpawnEntity":
                            int entId = (int)(values["entityId"] ?? 0);
                            int health = (int)(values["health"] ?? 1);
                            Vector3 position = NetworkMapSharer.share.localChar.transform.position;
                            NetworkNavMesh.nav.SpawnAnAnimalOnTile(entId, (int)(position.x / 2), (int)(position.z / 2), null, health);
                            break;
                        case "Bomb":
                            //Bomb is 277
                            NetworkMapSharer.share.localChar.myInteract.CmdSpawnPlaceable(playerPos, 277);
                            break;
                        case "RepairItems":
                            foreach (var s in Inventory.inv.invSlots)
                            {
                                if (s.itemInSlot != null && s.itemInSlot.isATool)
                                {
                                    s.stack += 2000;
                                    s.refreshSlot();
                                }
                            }
                            break;
                        case "SetStamina":
                            int newStamina = (int)(values["value"] ?? 0);
                            NetworkMapSharer.share.localChar.CmdSetNewStamina(newStamina);
                            break;
                        case "HealPlayer":
                            var damageable = NetworkMapSharer.share.localChar.GetComponent<Damageable>();
                            damageable.CmdChangeHealthTo(100);
                            break;
                        case "HurtPlayer":
                            int amount = (int)(values["amount"] ?? 0);
                            NetworkMapSharer.share.localChar.CmdTakeDamage(amount);
                            break;
                        case "Mail":
                            letters.Add(nextId, new CustomLetterTemplate("This is a test!"));
                            MailManager.manage.mailInBox.Add(new Letter(-5, Letter.LetterType.randomLetter)
                            {
                                letterTemplateNo = nextId
                            });
                            nextId -= 1;
                            break;
                        case "Test":
                            //NetworkMapSharer.share.localChar.myInteract.changeTile(4, 0);
                            break;
                    }
                }
            }
        }

        private void ShuffleInv(bool hotbarOnly = false)
        {
            var items = new List<ItemStack>();
            for (int i = 0; i < Inventory.inv.invSlots.Length; i++)
            {
                var s = Inventory.inv.invSlots[i];
                if (s.slotUnlocked && (i < 9 || !hotbarOnly))
                    items.Add(new ItemStack(s?.itemInSlot?.getItemId() ?? -1, s?.stack ?? 0));
            }
            items.Shuffle();
            int currIndex = 0;

            for (int i = 0; i < Inventory.inv.invSlots.Length; i++)
            {
                var s = Inventory.inv.invSlots[i];
                if (s.slotUnlocked && (i < 9 || !hotbarOnly))
                {
                    s.updateSlotContentsAndRefresh(items[currIndex].itemId, items[currIndex].stack);
                    currIndex++;
                }
            }

            Inventory.inv.equipNewSelectedSlot();
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

static class MyExtensions
{
    private static System.Random rng = new System.Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}

[HarmonyPatch(typeof(Letter), nameof(Letter.getMyTemplate))]
class Patch
{
    static bool Prefix(ref LetterTemplate __result, Letter __instance)
    {
        if (__instance.myType == Letter.LetterType.randomLetter && __instance.letterTemplateNo <= -200)
        {
            __result = Plugin.letters[__instance.letterTemplateNo];
            return false;
        }
        return true;
    }
}