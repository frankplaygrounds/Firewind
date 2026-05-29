using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Firewind.Messages.StaticMessageHandlers
{
    class SharedPacketLib
    {
        internal static void CheckHabboRelease(GameClientMessageHandler handler)
        {
            handler.CheckRelease();
        }

        internal static void InitCrypto(GameClientMessageHandler handler)
        {
            handler.InitCrypto();
        }

        internal static void SecretKey(GameClientMessageHandler handler)
        {
            handler.InitSecretKey();
        }

        internal static void setVars(GameClientMessageHandler handler)
        {
            handler.setClientVars();
        }

        internal static void setUniqueId(GameClientMessageHandler handler)
        {
            handler.setUniqueIDToClient();
        }

        internal static void PrepareCampaing(GameClientMessageHandler handler)
        {
            handler.PrepareCampaing();
        }

        internal static void SendCampaingData(GameClientMessageHandler handler)
        {
            handler.SendCampaingData();
        }

        internal static void GetCatalogIndex(GameClientMessageHandler handler)
        {
            handler.GetCatalogIndex();
        }

        internal static void GetCatalogPage(GameClientMessageHandler handler)
        {
            handler.GetCatalogPage();
        }

        internal static void RedeemVoucher(GameClientMessageHandler handler)
        {
            handler.RedeemVoucher();
        }

        internal static void HandlePurchase(GameClientMessageHandler handler)
        {
            handler.HandlePurchase();
        }

        internal static void SendStream(GameClientMessageHandler handler)
        {
            handler.SendToStream();
        }


        internal static void StreamLike(GameClientMessageHandler handler)
        {
            handler.StreamLike();
        }

        internal static void Stream(GameClientMessageHandler handler)
        {
            handler.InitStream();
        }

        internal static void PurchaseFromCatalogAsGift(GameClientMessageHandler handler)
        {
            handler.PurchaseFromCatalogAsGift();
        }

        internal static void GetRecyclerRewards(GameClientMessageHandler handler)
        {
            handler.GetRecyclerRewards();
        }

        internal static void CanGift(GameClientMessageHandler handler)
        {
            handler.CanGift();
        }

        internal static void GetMarketplaceConfiguration(GameClientMessageHandler handler)
        {
            handler.GetMarketplaceConfiguration();
        }

        internal static void GetCataData2(GameClientMessageHandler handler)
        {
            handler.GetCataData2();
        }

        //internal static void MarketplaceCanSell(GameClientMessageHandler handler)
        //{
        //    handler.MarketplaceCanSell();
        //}

        //internal static void MarketplacePostItem(GameClientMessageHandler handler)
        //{
        //    handler.MarketplacePostItem();
        //}

        //internal static void MarketplaceGetOwnOffers(GameClientMessageHandler handler)
        //{
        //    handler.MarketplaceGetOwnOffers();
        //}

        //internal static void MarketplaceTakeBack(GameClientMessageHandler handler)
        //{
        //    handler.MarketplaceTakeBack();
        //}

        //internal static void MarketplaceClaimCredits(GameClientMessageHandler handler)
        //{
        //    handler.MarketplaceClaimCredits();
        //}

        //internal static void MarketplaceGetOffers(GameClientMessageHandler handler)
        //{
        //    handler.MarketplaceGetOffers();
        //}

        //internal static void MarketplacePurchase(GameClientMessageHandler handler)
        //{
        //    handler.MarketplacePurchase();
        //}

        internal static void CheckPetName(GameClientMessageHandler handler)
        {
            handler.CheckPetName();
        }

        internal static void Pong(GameClientMessageHandler handler)
        {
            handler.Pong();
        }

        internal static void RemoveHanditem(GameClientMessageHandler handler)
        {
            handler.RemoveHanditem();
        }

        internal static void GiveHanditem(GameClientMessageHandler handler)
        {
            handler.GiveHanditem();
        }

        //internal static void GetGroupdetails(GameClientMessageHandler handler)
        //{
        //    handler.GetGroupdetails();
        //}

        internal static void SSOLogin(GameClientMessageHandler handler)
        {
            handler.SSOLogin();
        }

        internal static void InitHelpTool(GameClientMessageHandler handler)
        {
            handler.InitHelpTool();
        }

        //internal static void GetHelpCategories(GameClientMessageHandler handler)
        //{
        //    handler.GetHelpCategories();
        //}

        //internal static void ViewHelpTopic(GameClientMessageHandler handler)
        //{
        //    handler.ViewHelpTopic();
        //}

        //internal static void SearchHelpTopics(GameClientMessageHandler handler)
        //{
        //    handler.SearchHelpTopics();
        //}

        //internal static void GetTopicsInCategory(GameClientMessageHandler handler)
        //{
        //    handler.GetTopicsInCategory();
        //}

        internal static void SubmitHelpTicket(GameClientMessageHandler handler)
        {
            handler.SubmitHelpTicket();
        }

        internal static void DeletePendingCFH(GameClientMessageHandler handler)
        {
            handler.DeletePendingCFH();
        }

        internal static void ModGetUserInfo(GameClientMessageHandler handler)
        {
            handler.ModGetUserInfo();
        }

        internal static void ModGetUserChatlog(GameClientMessageHandler handler)
        {
            handler.ModGetUserChatlog();
        }

        internal static void ModGetRoomChatlog(GameClientMessageHandler handler)
        {
            handler.ModGetRoomChatlog();
        }

        internal static void ModGetRoomTool(GameClientMessageHandler handler)
        {
            handler.ModGetRoomTool();
        }

        internal static void ModPickTicket(GameClientMessageHandler handler)
        {
            handler.ModPickTicket();
        }

        internal static void ModReleaseTicket(GameClientMessageHandler handler)
        {
            handler.ModReleaseTicket();
        }

        internal static void CloseIssues(GameClientMessageHandler handler)
        {
            handler.CloseIssues();
        }

        internal static void ModGetTicketChatlog(GameClientMessageHandler handler)
        {
            handler.ModGetTicketChatlog();
        }

        internal static void ModGetRoomVisits(GameClientMessageHandler handler)
        {
            handler.ModGetRoomVisits();
        }

        internal static void ModSendRoomAlert(GameClientMessageHandler handler)
        {
            handler.ModSendRoomAlert();
        }

        internal static void ModPerformRoomAction(GameClientMessageHandler handler)
        {
            handler.ModPerformRoomAction();
        }

        internal static void ModSendUserCaution(GameClientMessageHandler handler)
        {
            handler.ModSendUserCaution();
        }

        internal static void ModSendUserMessage(GameClientMessageHandler handler)
        {
            handler.ModSendUserMessage();
        }

        internal static void ModKickUser(GameClientMessageHandler handler)
        {
            handler.ModKickUser();
        }

        internal static void ModBanUser(GameClientMessageHandler handler)
        {
            handler.ModBanUser();
        }

        internal static void CallGuideBot(GameClientMessageHandler handler)
        {
            handler.CallGuideBot();
        }

        internal static void InitMessenger(GameClientMessageHandler handler)
        {
            handler.InitMessenger();
        }

        internal static void FriendsListUpdate(GameClientMessageHandler handler)
        {
            handler.FriendsListUpdate();
        }

        internal static void RemoveBuddy(GameClientMessageHandler handler)
        {
            handler.RemoveBuddy();
        }

        internal static void SearchHabbo(GameClientMessageHandler handler)
        {
            handler.SearchHabbo();
        }

        internal static void AcceptRequest(GameClientMessageHandler handler)
        {
            handler.AcceptRequest();
        }

        internal static void DeclineFriend(GameClientMessageHandler handler)
        {
            handler.DeclineFriend();
        }

        internal static void RequestBuddy(GameClientMessageHandler handler)
        {
            handler.RequestBuddy();
        }

        internal static void SendInstantMessenger(GameClientMessageHandler handler)
        {
            handler.SendInstantMessenger();
        }

        internal static void FollowBuddy(GameClientMessageHandler handler)
        {
            handler.FollowBuddy();
        }

        internal static void AnswerQuestion(GameClientMessageHandler handler)
        {
            handler.AnswerInfobusPoll();
        }
        internal static void SendInstantInvite(GameClientMessageHandler handler)
        {
            handler.SendInstantInvite();
        }

        internal static void AddFavorite(GameClientMessageHandler handler)
        {
            handler.AddFavorite();
        }

        internal static void RemoveFavorite(GameClientMessageHandler handler)
        {
            handler.RemoveFavorite();
        }

        internal static void GoToHotelView(GameClientMessageHandler handler)
        {
            handler.GoToHotelView();
        }

        internal static void GetFlatCats(GameClientMessageHandler handler)
        {
            handler.GetFlatCats();
        }

        internal static void EnterInquiredRoom(GameClientMessageHandler handler)
        {
            handler.EnterInquiredRoom();
        }

        internal static void GetPubs(GameClientMessageHandler handler)
        {
            handler.GetPubs();
        }

        //internal static void GetGuestRoom(GameClientMessageHandler handler)
        //{
        //    handler.GetGuestRoom();
        //}

        internal static void PopularRoomsSearch(GameClientMessageHandler handler)
        {
            handler.PopularRoomsSearch();
        }

        internal static void GetHighRatedRooms(GameClientMessageHandler handler)
        {
            handler.GetHighRatedRooms();
        }

        internal static void GetFriendsRooms(GameClientMessageHandler handler)
        {
            handler.GetFriendsRooms();
        }

        internal static void GetRoomsWithFriends(GameClientMessageHandler handler)
        {
            handler.GetRoomsWithFriends();
        }

        internal static void GetOwnRooms(GameClientMessageHandler handler)
        {
            handler.GetOwnRooms();
        }

        internal static void GetFavoriteRooms(GameClientMessageHandler handler)
        {
            handler.GetFavoriteRooms();
        }

        internal static void GetRecentRooms(GameClientMessageHandler handler)
        {
            handler.GetRecentRooms();
        }

        internal static void GetEvents(GameClientMessageHandler handler)
        {
            handler.GetEvents();
        }

        internal static void GetPopularTags(GameClientMessageHandler handler)
        {
            handler.GetPopularTags();
        }

        internal static void PerformSearch(GameClientMessageHandler handler)
        {
            handler.PerformSearch();
        }

        internal static void PerformSearch2(GameClientMessageHandler handler)
        {
            handler.PerformSearch2();
        }

        internal static void OpenFlat(GameClientMessageHandler handler)
        {
            handler.OpenFlat();
        }

        internal static void GetAdvertisement(GameClientMessageHandler handler)
        {
            handler.GetAdvertisement();
        }

        internal static void OpenPub(GameClientMessageHandler handler)
        {
            handler.OpenConnection();
        }

        internal static void GetInventory(GameClientMessageHandler handler)
        {
            handler.GetInventory();
        }

        internal static void GetFurnitureAliases(GameClientMessageHandler handler)
        {
            handler.GetFurnitureAliases();
        }

        internal static void GetRoomEntryData(GameClientMessageHandler handler)
        {
            handler.GetRoomEntryData();
        }

        internal static void GetRoomAd(GameClientMessageHandler handler)
        {
            handler.GetRoomAd();
        }

        internal static void GoToFlat(GameClientMessageHandler handler)
        {
            handler.GoToFlat();
        }

        internal static void OpenFlatConnection(GameClientMessageHandler handler)
        {
            handler.OpenFlatConnection();
        }

        internal static void ClearRoomLoading(GameClientMessageHandler handler)
        {
            handler.ClearRoomLoading();
        }

        internal static void Talk(GameClientMessageHandler handler)
        {
            handler.Talk();
        }

        internal static void Shout(GameClientMessageHandler handler)
        {
            handler.Shout();
        }

        internal static void Whisper(GameClientMessageHandler handler)
        {
            handler.Whisper();
        }

        internal static void Move(GameClientMessageHandler handler)
        {
            handler.Move();
        }

        internal static void CanCreateRoom(GameClientMessageHandler handler)
        {
            handler.CanCreateRoom();
        }

        internal static void CreateRoom(GameClientMessageHandler handler)
        {
            handler.CreateRoom();
        }

        internal static void GetRoomSettings(GameClientMessageHandler handler)
        {
            handler.GetRoomSettings();
        }

        internal static void SaveRoomSettings(GameClientMessageHandler handler)
        {
            handler.SaveRoomSettings();
        }

        internal static void GiveRights(GameClientMessageHandler handler)
        {
            handler.GiveRights();
        }

        internal static void RemoveRights(GameClientMessageHandler handler)
        {
            handler.RemoveRights();
        }

        internal static void TakeAllRights(GameClientMessageHandler handler)
        {
            handler.TakeAllRights();
        }

        internal static void KickUser(GameClientMessageHandler handler)
        {
            handler.KickUser();
        }

        internal static void BanUser(GameClientMessageHandler handler)
        {
            handler.BanUser();
        }

        internal static void SetHomeRoom(GameClientMessageHandler handler)
        {
            handler.SetHomeRoom();
        }

        internal static void DeleteRoom(GameClientMessageHandler handler)
        {
            handler.DeleteRoom();
        }

        internal static void LookAt(GameClientMessageHandler handler)
        {
            handler.LookAt();
        }

        internal static void StartTyping(GameClientMessageHandler handler)
        {
            handler.StartTyping();
        }

        internal static void StopTyping(GameClientMessageHandler handler)
        {
            handler.StopTyping();
        }

        internal static void IgnoreUser(GameClientMessageHandler handler)
        {
            handler.IgnoreUser();
        }

        internal static void UnignoreUser(GameClientMessageHandler handler)
        {
            handler.UnignoreUser();
        }

        internal static void CanCreateRoomEvent(GameClientMessageHandler handler)
        {
            handler.CanCreateRoomEvent();
        }

        internal static void StartEvent(GameClientMessageHandler handler)
        {
            handler.StartEvent();
        }

        internal static void StopEvent(GameClientMessageHandler handler)
        {
            handler.StopEvent();
        }

        internal static void EditEvent(GameClientMessageHandler handler)
        {
            handler.EditEvent();
        }

        internal static void Wave(GameClientMessageHandler handler)
        {
            handler.Wave();
        }

        internal static void Sign(GameClientMessageHandler handler)
        {
            handler.Sign();
        }

        internal static void GetUserTags(GameClientMessageHandler handler)
        {
            handler.GetUserTags();
        }

        internal static void GetUserBadges(GameClientMessageHandler handler)
        {
            handler.GetUserBadges();
        }

        internal static void RateRoom(GameClientMessageHandler handler)
        {
            handler.RateRoom();
        }

        internal static void Dance(GameClientMessageHandler handler)
        {
            handler.Dance();
        }

        internal static void AnswerDoorbell(GameClientMessageHandler handler)
        {
            handler.AnswerDoorbell();
        }

        internal static void ApplyRoomEffect(GameClientMessageHandler handler)
        {
            handler.ApplyRoomEffect();
        }

        internal static void PlacePostIt(GameClientMessageHandler handler)
        {
            handler.PlacePostIt();
        }

        internal static void PlaceItem(GameClientMessageHandler handler)
        {
            handler.PlaceItem();
        }

        internal static void TakeItem(GameClientMessageHandler handler)
        {
            handler.TakeItem();
        }

        internal static void MoveItem(GameClientMessageHandler handler)
        {
            handler.MoveItem();
        }

        internal static void MoveWallItem(GameClientMessageHandler handler)
        {
            handler.MoveWallItem();
        }

        internal static void TriggerItem(GameClientMessageHandler handler)
        {
            handler.TriggerItem();
        }

        internal static void TriggerItemDiceSpecial(GameClientMessageHandler handler)
        {
            handler.TriggerItemDiceSpecial();
        }

        internal static void OpenPostit(GameClientMessageHandler handler)
        {
            handler.OpenPostit();
        }

        internal static void SavePostit(GameClientMessageHandler handler)
        {
            handler.SavePostit();
        }

        internal static void DeletePostit(GameClientMessageHandler handler)
        {
            handler.DeletePostit();
        }

        internal static void OpenPresent(GameClientMessageHandler handler)
        {
            handler.OpenPresent();
        }

        internal static void GetMoodlight(GameClientMessageHandler handler)
        {
            handler.GetMoodlight();
        }

        internal static void UpdateMoodlight(GameClientMessageHandler handler)
        {
            handler.UpdateMoodlight();
        }

        internal static void SwitchMoodlightStatus(GameClientMessageHandler handler)
        {
            handler.SwitchMoodlightStatus();
        }

        internal static void InitTrade(GameClientMessageHandler handler)
        {
            handler.InitTrade();
        }

        internal static void OfferTradeItem(GameClientMessageHandler handler)
        {
            handler.OfferTradeItem();
        }

        internal static void TakeBackTradeItem(GameClientMessageHandler handler)
        {
            handler.TakeBackTradeItem();
        }

        internal static void StopTrade(GameClientMessageHandler handler)
        {
            handler.StopTrade();
        }

        internal static void AcceptTrade(GameClientMessageHandler handler)
        {
            handler.AcceptTrade();
        }

        internal static void UnacceptTrade(GameClientMessageHandler handler)
        {
            handler.UnacceptTrade();
        }

        internal static void CompleteTrade(GameClientMessageHandler handler)
        {
            handler.CompleteTrade();
        }

        internal static void GiveRespect(GameClientMessageHandler handler)
        {
            handler.GiveRespect();
        }

        internal static void ApplyEffect(GameClientMessageHandler handler)
        {
            handler.ApplyEffect();
        }

        internal static void EnableEffect(GameClientMessageHandler handler)
        {
            handler.EnableEffect();
        }

        internal static void RecycleItems(GameClientMessageHandler handler)
        {
            handler.RecycleItems();
        }

        internal static void RedeemExchangeFurni(GameClientMessageHandler handler)
        {
            handler.RedeemExchangeFurni();
        }

        internal static void KickBot(GameClientMessageHandler handler)
        {
            handler.KickBot();
        }

        internal static void PlacePet(GameClientMessageHandler handler)
        {
            handler.PlacePet();
        }

        internal static void GetPetInfo(GameClientMessageHandler handler)
        {
            handler.GetPetInfo();
        }

        internal static void PickUpPet(GameClientMessageHandler handler)
        {
            handler.PickUpPet();
        }

        internal static void RespectPet(GameClientMessageHandler handler)
        {
            handler.RespectPet();
        }

        internal static void AddSaddle(GameClientMessageHandler handler)
        {
            handler.AddSaddle();
        }

        internal static void RemoveSaddle(GameClientMessageHandler handler)
        {
            handler.RemoveSaddle();
        }

        internal static void Ride(GameClientMessageHandler handler)
        {
            handler.MountPet();
        }

        internal static void SetLookTransfer(GameClientMessageHandler handler)
        {
            handler.SetLookTransfer();
        }

        internal static void GetPetCommands(GameClientMessageHandler handler)
        {
            handler.GetPetCommands();
        }

        internal static void PetRaces(GameClientMessageHandler handler)
        {
            handler.PetRaces();
        }

        internal static void SaveWired(GameClientMessageHandler handler)
        {
            handler.SaveWired();
        }

        internal static void SaveWiredCondition(GameClientMessageHandler handler)
        {
            handler.SaveWiredConditions();
        }

        internal static void ApplySnapshot(GameClientMessageHandler handler)
        {
            handler.ApplySnapshot();
        }

        internal static void GetMusicData(GameClientMessageHandler handler)
        {
            handler.GetMusicData();
        }

        internal static void AddPlaylistItem(GameClientMessageHandler handler)
        {
            handler.AddPlaylistItem();
        }

        internal static void RemovePlaylistItem(GameClientMessageHandler handler)
        {
            handler.RemovePlaylistItem();
        }

        internal static void GetDisks(GameClientMessageHandler handler)
        {
            handler.GetDisks();
        }

        internal static void GetPlaylists(GameClientMessageHandler handler)
        {
            handler.GetPlaylists();
        }

        internal static void GetUserInfo(GameClientMessageHandler handler)
        {
            handler.GetUserInfo();
        }

        internal static void LoadProfile(GameClientMessageHandler handler)
        {
            handler.LoadProfile();
        }

        internal static void GetCreditsInfo(GameClientMessageHandler handler)
        {
            handler.GetCreditsInfo();
        }

        internal static void ScrGetUserInfo(GameClientMessageHandler handler)
        {
            handler.ScrGetUserInfo();
        }

        internal static void GetBadges(GameClientMessageHandler handler)
        {
            handler.GetBadges();
        }

        internal static void UpdateBadges(GameClientMessageHandler handler)
        {
            handler.UpdateBadges();
        }

        internal static void GetAchievements(GameClientMessageHandler handler)
        {
            handler.GetAchievements();
        }

        internal static void ChangeLook(GameClientMessageHandler handler)
        {
            handler.ChangeLook();
        }

        internal static void ChangeMotto(GameClientMessageHandler handler)
        {
            handler.ChangeMotto();
        }

        internal static void GetWardrobe(GameClientMessageHandler handler)
        {
            handler.GetWardrobe();
        }

        internal static void SaveWardrobe(GameClientMessageHandler handler)
        {
            handler.SaveWardrobe();
        }

        internal static void GetPetsInventory(GameClientMessageHandler handler)
        {
            handler.GetPetsInventory();
        }

        internal static void OpenQuests(GameClientMessageHandler handler)
        {
            handler.OpenQuests();
        }

        internal static void StartQuest(GameClientMessageHandler handler)
        {
            handler.StartQuest();
        }

        internal static void StopQuest(GameClientMessageHandler handler)
        {
            handler.StopQuest();
        }

        internal static void GetCurrentQuest(GameClientMessageHandler handler)
        {
            handler.GetCurrentQuest();
        }

        internal static void MannequeNameChange(GameClientMessageHandler handler)
        {
            handler.MannequeNameChange();
        }

        internal static void FindFriends(GameClientMessageHandler handler)
        {
            handler.FindFriends();
        }
        internal static void MannequeFigureChange(GameClientMessageHandler handler)
        {
            handler.MannequeFigureChange();
        }

        internal static void SetAdParameters(GameClientMessageHandler handler)
        {
            handler.SetAdParameters();
        }

        internal static void GetGuestRoom(GameClientMessageHandler handler)
        {
            handler.GetGuestRoom();
        }

        internal static void GetHabboGroupBadges(GameClientMessageHandler handler)
        {
            handler.GetHabboGroupBadges();
        }

        internal static void StartGuildPurchase(GameClientMessageHandler handler)
        {
            handler.StartGuildPurchase();
        }

        internal static void CreateGuild(GameClientMessageHandler handler)
        {
            handler.CreateGuild();
        }

        internal static void GetGuildInfo(GameClientMessageHandler handler)
        {
            handler.GetGuildInfo();
        }

        internal static void GetGroupBadgeParts(GameClientMessageHandler handler)
        {
            handler.GetGroupBadgeParts();
        }

        internal static void GetGuildFurniInfo(GameClientMessageHandler handler)
        {
            handler.GetGuildFurniInfo();
        }

        internal static void GetGuildManageInfo(GameClientMessageHandler handler)
        {
            handler.GetGuildManageInfo();
        }

        internal static void GetHabboGroupsWhereMember(GameClientMessageHandler handler)
        {
            handler.GetHabboGroupsWhereMember();
        }

        internal static void GetGroupMemberList(GameClientMessageHandler handler)
        {
            handler.GetGroupMemberList();
        }

        internal static void SelectFavouriteHabboGroup(GameClientMessageHandler handler)
        {
            handler.SelectFavouriteHabboGroup();
        }

        internal static void DeselectFavouriteHabboGroup(GameClientMessageHandler handler)
        {
            handler.DeselectFavouriteHabboGroup();
        }

        internal static void PromoteGroupMember(GameClientMessageHandler handler)
        {
            handler.PromoteGroupMember();
        }

        internal static void DemoteGroupMember(GameClientMessageHandler handler)
        {
            handler.DemoteGroupMember();
        }

        internal static void KickGroupMember(GameClientMessageHandler handler)
        {
            handler.KickGroupMember();
        }

        internal static void AcceptMembershipRequest(GameClientMessageHandler handler)
        {
            handler.AcceptMembershipRequest();
        }

        internal static void DeclineMembershipRequest(GameClientMessageHandler handler)
        {
            handler.DeclineMembershipRequest();
        }

        internal static void UpdateGuildBadge(GameClientMessageHandler handler)
        {
            handler.UpdateGuildBadge();
        }

        internal static void UpdateGuildIdentity(GameClientMessageHandler handler)
        {
            handler.UpdateGuildIdentity();
        }

        internal static void UpdateGuildColors(GameClientMessageHandler handler)
        {
            handler.UpdateGuildColors();
        }

        internal static void UpdateGuildSettings(GameClientMessageHandler handler)
        {
            handler.UpdateGuildSettings();
        }

        internal static void JoinGroup(GameClientMessageHandler handler)
        {
            handler.JoinGroup();
        }

        internal static void FindHotGroups(GameClientMessageHandler handler)
        {
            handler.FindHotGroups();
        }

        internal static void EventLog(GameClientMessageHandler handler)
        {
            handler.EventLog();
        }

        internal static void PerformanceLog(GameClientMessageHandler handler)
        {
            handler.PerformanceLog();
        }

        internal static void GetBadgePointLimits(GameClientMessageHandler handler)
        {
            // TODO: Send BadgePointLimits message in return?
        }

        internal static void GetSoundSettings(GameClientMessageHandler handler)
        {
            handler.GetSoundSettings();
        }

        internal static void GetBotInventory(GameClientMessageHandler handler)
        {
            handler.GetBotInventory();
        }

        internal static void PlaceBot(GameClientMessageHandler handler)
        {
            throw new NotImplementedException();
        }
    }
}
