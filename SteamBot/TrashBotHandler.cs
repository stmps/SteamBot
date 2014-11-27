using SteamKit2;
using SteamTrade;
using SteamTrade.TradeOffer;
using System;
using System.Collections.Generic;
using TradeAsset = SteamTrade.TradeOffer.TradeOffer.TradeStatusUser.TradeAsset;
using System.Collections.Specialized;
using System.Net; // for CookieContainer

namespace SteamBot
{
    /// <summary>
    /// A basic handler to replicate the functionality of the
    /// node-steam-trash-bot (github.com/bonnici/node-steam-trash-bot)
    /// Adapted from TradeOfferUserHandler
    /// </summary>
    public class TrashBotHandler : UserHandler
    {
        public TrashBotHandler(Bot bot, SteamID sid) : base(bot, sid) { }

        private bool AutoJoinHomeGroupChat = true;
        private bool AutoSetPersonaState = true;
        private EPersonaState PersonaState = EPersonaState.LookingToTrade;
        private String HomeGroupString = "103582791436780209";
        private SteamID HomeGroup = new SteamID((ulong)103582791436780209);

        private GenericInventory mySteamInventory = new GenericInventory();
        private GenericInventory OtherSteamInventory = new GenericInventory();

        // trashbot accepts *all* trade offers
        // set your inventory privacy appropriately if you don't want the public taking all your items
        public override void OnNewTradeOffer(TradeOffer offer)
        {          
            int theirItemCount = offer.Items.GetTheirItems().Count;
            int myItemCount = offer.Items.GetMyItems().Count;

            if (offer.Accept())
            {
                String offerAcceptMessage = "Offer accepted from " + Bot.SteamFriends.GetFriendPersonaName(OtherSID) + "! (gained " +
                    theirItemCount + " item(s), lost " + myItemCount + " item(s))";

                Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, offerAcceptMessage);
                Bot.SteamFriends.SendChatRoomMessage(HomeGroup, EChatEntryType.ChatMsg, offerAcceptMessage);
                SendClanComment(HomeGroupString, offerAcceptMessage);
                Log.Success(offerAcceptMessage + " from " + Bot.SteamFriends.GetFriendPersonaName(OtherSID));

                if (myItemCount > 0)
                {
                    // todo: post a group/clan comment about new items
                }
            }
            else
                Log.Warn("Trade offer failed");
        }

        public override void OnMessage(string message, EChatEntryType type) {
            SteamID TargetFriend;
            SteamID TargetGroup;
            String SendMessage;

            if (IsAdmin)
            {
                message = message.ToLower();
                // send friend request
                // send group invite
                // send trade request
                // send message
                String[] adminCommands = message.Split(' ');
                
                switch (adminCommands[0])
                {
                    case ".help":
                        Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, ".addfriend <friend ID> - Send a friend request");
                        Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, ".removefriend <friend ID> - Remove a friend");
                        Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, ".invitegroup <friend ID> <group ID> - Send a group (clan) invite");
                        Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, ".traderequest <friend ID> - Send a new trade request");
                        Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, ".msg <friend ID> <message> - Send a chat message (individual or group)");
                        Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, ".help - Display this help");
                        Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg, "IDs must in 64-bit format. See http://steamidconverter.com");
                        break;
                    case ".addfriend":
                        TargetFriend = new SteamID(Convert.ToUInt64(adminCommands[1]));
                        Log.Info(string.Format("Sending a friend request to " + adminCommands[1]));
                        Bot.SteamFriends.AddFriend(TargetFriend);
                        break;
                    case ".removefriend":
                        TargetFriend = new SteamID(Convert.ToUInt64(adminCommands[1])); 
                        Log.Info(string.Format("Removing " + Bot.SteamFriends.GetFriendPersonaName(TargetFriend) + " from friends list"));
                        Bot.SteamFriends.RemoveFriend(TargetFriend);
                        break;
                    case ".invitegroup":
                        TargetFriend = new SteamID(Convert.ToUInt64(adminCommands[1]));
                        TargetGroup = new SteamID(Convert.ToUInt64(adminCommands[2])); // 64-bit group id
                        Log.Info(string.Format("Trying to invite {0} to {1}", Bot.SteamFriends.GetFriendPersonaName(TargetFriend), Bot.SteamFriends.GetClanName(TargetGroup)));
                        Bot.InviteUserToGroup(TargetFriend, TargetGroup);
                        break;
                    case ".traderequest":
                        break;
                    case ".msg":
                        TargetFriend = new SteamID(Convert.ToUInt64(adminCommands[1]));
                        SendMessage = message.Substring(message.IndexOf(".msg") + 4);
                        Log.Info(string.Format("Sending message to {0}: {1}", TargetFriend, SendMessage));
                        Bot.SteamFriends.SendChatMessage(TargetFriend, EChatEntryType.ChatMsg, SendMessage);
                        break;
                    default:
                        break;
                }

            }
        }

        public override bool OnGroupAdd() { return false; }

        // todo: some kind of remote friends list admin
        public override bool OnFriendAdd() { return IsAdmin; }

        public override void OnFriendRemove() { }

        public override void OnLoginCompleted() {
            if (AutoSetPersonaState)
                Bot.SteamFriends.SetPersonaState(PersonaState);

            if (AutoJoinHomeGroupChat)
                Bot.SteamFriends.JoinChat(HomeGroup);
        }

        public override bool OnTradeRequest() {
            Bot.log.Success(Bot.SteamFriends.GetFriendPersonaName(OtherSID) + " (" + OtherSID.ToString() + ") has requested to trade with me!");
            return true; }

        public override void OnTradeError(string error) {
            Bot.SteamFriends.SendChatMessage(OtherSID,
                                       EChatEntryType.ChatMsg,
                                       "Oh, there was an error: " + error + "."
                                       );
            Bot.log.Warn(error);
        }

        public override void OnTradeTimeout() {
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                "Sorry, but you were AFK and the trade was canceled.");
            Bot.log.Info("User was kicked because he was AFK.");
        }

        public override void OnTradeSuccess() {
            // Trade completed successfully
            Log.Success("Trade Complete.");
            Bot.SteamFriends.SendChatMessage(OtherSID, EChatEntryType.ChatMsg,
                "Trade Complete.");
        }

        public override void OnTradeInit() {
/*            List<long> contextId = new List<long>();
            // SteamInventory AppId = 753 
            // *  Context Id      Description
            //      1           Gifts (Games), must be public on steam profile in order to work.
            //      3           Coupons
            //      6           Trading Cards, Emoticons & Backgrounds. 

            contextId.Add(1);
            contextId.Add(3);
            contextId.Add(6);

            mySteamInventory.load(753, contextId, Bot.SteamClient.SteamID);
            OtherSteamInventory.load(753, contextId, OtherSID);

            if (!mySteamInventory.isLoaded | !OtherSteamInventory.isLoaded)
            {
                Trade.SendMessage("Couldn't open an inventory.");
                if (OtherSteamInventory.errors.Count > 0)
                {
                    Trade.SendMessage("User Errors:");
                    foreach (string error in OtherSteamInventory.errors)
                    {
                        Trade.SendMessage(" * " + error);
                    }
                }

                if (mySteamInventory.errors.Count > 0)
                {
                    Trade.SendMessage("Bot Errors:");
                    foreach (string error in mySteamInventory.errors)
                    {
                        Trade.SendMessage(" * " + error);
                    }
                }
            }
 */
        }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem) {
            /*
            GenericInventory.ItemDescription tmpDescription = OtherSteamInventory.getDescription(inventoryItem.Id);
            Trade.SendMessage("Object AppID: " + inventoryItem.AppId);
            Trade.SendMessage("Object ContextId: " + inventoryItem.ContextId);
            Trade.SendMessage("Type: " + tmpDescription.type);
            Trade.SendMessage("Marketable: " + (tmpDescription.marketable ? "Yes" : "No"));
            Trade.SendMessage("URL: " + tmpDescription.url);
            Trade.SendMessage("Name: " + tmpDescription.name);
             */
        }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeMessage(string message) { }

        public override void OnTradeReady(bool ready) {
            //Because SetReady must use its own version, it's important
            //we poll the trade to make sure everything is up-to-date.
            Trade.Poll();
            if (!ready)
            {
                Trade.SetReady(false);
            }
            else
            {
                Trade.SetReady(true);
            }
        }

        public override void OnTradeAccept() {
            if (IsAdmin)
            {
                //Even if it is successful, AcceptTrade can fail on
                //trades with a lot of items so we use a try-catch
                try
                {
                    Trade.AcceptTrade();
                }
                catch
                {
                    Log.Warn("The trade might have failed, but we can't be sure.");
                }

                Log.Success("Trade Complete!");
            }
        }

        public void SendClanComment(String SteamID, string Comment) {
            // commentType is Clan, ForumTopic or Profile
            CookieContainer cookies = new CookieContainer();
            var data = new NameValueCollection();
            
            cookies.Add(new Cookie("sessionid", Bot.sessionId, String.Empty, "steamcommunity.com"));
            cookies.Add(new Cookie("steamLogin", Bot.token, String.Empty, "steamcommunity.com"));

            data.Add("comment", Comment);
            data.Add("count", "6");
            data.Add("sessionid", Bot.sessionId);

            Log.Debug("Sending comment: http://steamcommunity.com/comment/Clan/post/" + SteamID + "/-1/" + "POST" + data + cookies + false);
            string response = SteamWeb.Fetch("http://steamcommunity.com/comment/Clan/post/" + SteamID + "/-1/", "POST", data, cookies, false);
            Log.Debug(response);
        }
    }
}
