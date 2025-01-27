using System;
using System.Collections.Generic;
using System.Text;
using Firewind.Core;
using HabboEvents;
namespace Firewind.Messages
{
    partial class GameClientMessageHandler
    {
        internal void CheckRelease()
        {
            string clientVersion = Request.ReadString();

            if (clientVersion != "RELEASE63-201207100852-501822384") // not using current supported client
            {
                Session.SendMOTD(String.Format("Something went wrong while trying to connect.\nDEBUG: Server doesn't support {0}!\rPlease use {1}.", clientVersion, "RELEASE63-201207100852-501822384"));
            }
        }

        internal void InitCrypto()
        {
            Session.TimePingedReceived = DateTime.Now;
            Response.Init(Outgoing.SendBannerMessageComposer);
            Response.AppendString("nocryptonotoken");
            Response.AppendBoolean(false);
            SendResponse();
        }

        internal void InitSecretKey()
        {
            Session.TimePingedReceived = DateTime.Now;
            Response.Init(Outgoing.SecretKeyComposer);
            Response.AppendString("nocryptonokey");
            SendResponse();
        }

        internal void setClientVars()
        {
            string swfs = Request.ReadString();
            string vars = Request.ReadString();
        }

        internal void setUniqueIDToClient()
        {
            string Id = Request.ReadString();
            Session.MachineId = Id;
        }

        internal void SSOLogin()
        {
            if (Session.GetHabbo() == null)
            {
                Session.tryLogin(Request.ReadString());
                Session.TimePingedReceived = DateTime.Now;
                //if (Session.tryLogin(Request.PopFixedString()))
                //{
                //    //RegisterCatalog();
                //    //RegisterHelp();
                //    //RegisterNavigator();
                //    //RegisterMessenger();
                //    //RegisterUsers();
                //    //RegisterRooms();
                //    //RegisterGroups();
                //    //RegisterQuests();
                //}
            }
            else
                Session.SendNotif(LanguageLocale.GetValue("user.allreadylogedon"));
        }

        //internal void RegisterHandshake()
        //{
        //    RequestHandlers.Add(206, new RequestHandler(SendSessionParams));
        //    RequestHandlers.Add(415, new RequestHandler(SSOLogin));
        //}

        //internal void UnRegisterHandshake()
        //{
        //    RequestHandlers.Remove(206);
        //    RequestHandlers.Remove(415);
        //}
    }
}
