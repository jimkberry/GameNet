using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using P2pNet;
using UniLog;

namespace GameNet
{
    public interface IGameNet
    {
        void Init(IGameNetClient _client); // TODO: maybe config stuff too?
        void Connect( string p2pConectionString );
        void Disconnect();
        void CreateGame<T>(T createGameData);
        void JoinGame(string gameP2pChannel);
        void LeaveGame();
        string LocalP2pId();
        void SendP2pMsg(string toChan, string payload);
        void Loop(); /// <summary> needs to be called periodically (drives message pump + group handling)</summary>       
    }

    public interface IGameNetClient
    {
        void SetGameNetInstance(IGameNet iGameNetInstance);     
        void OnGameCreated(string gameP2pChannel);
        void OnGameJoined(string gameId, string localP2pId);
        void OnPlayerJoined(string p2pId, string helloData);
        void OnPlayerLeft(string p2pId);
        void OnP2pMsg(string from, string to, string payload);
        string LocalPlayerData();
    }

    public class GameNetP2pMessage
    {
        public const string MsgBase = "BASEMSG";  // untyped "SendP2pMsg()/OnP2pMsg()" msg.    
        public string appMsgType;
        public string payload; // string or json-encoded application object
    }    

    public abstract class GameNetBase : IGameNet, IP2pNetClient
    {
        protected IGameNetClient client = null;
        protected IP2pNet p2p = null;
        protected string CurrentGameId {private set; get;}
        public UniLogger logger;

        // Some client callbacks can happen as a direct result of a call, but we would like for
        // them to be dispatched during poll(), rather than during th ecall itself. Put them 
        // in this queue and it'll happen that way.
        // OnGameCreated() is an example of one that might take a while, or might
        // happen immediately.
        protected Queue<Action> callbacksForNextPoll; 

        public GameNetBase()
        {
            callbacksForNextPoll = new Queue<Action>();
            logger = UniLogger.GetLogger("GameNet");
        }

        public void Init(IGameNetClient _client)
        {
            client = _client;
            _client.SetGameNetInstance(this);
        }

        //
        // IGameNet
        //
        public virtual void Connect( string p2pConectionString )
        {
            // P2pConnectionString is <p2p implmentation name>::<imp-dependent connection string>
            // Names are: p2predis

            p2p = null;
            string[] parts = p2pConectionString.Split(new string[]{"::"},StringSplitOptions.None); // Yikes! This is fugly.

            switch(parts[0].ToLower())
            {
                case "p2predis":
                    p2p = new P2pRedis(this, parts[1]);
                    break;
                default:
                    throw( new Exception($"Invalid connection type: {parts[0]}"));
            }

            if (p2p == null)
                throw( new Exception("p2p Connect failed"));
        }

        public virtual void Disconnect() 
        { 
            if (p2p?.GetId() != null)
                p2p.Leave(); 
        }

        // TODO: need "Destroy() or Reset() or (Init)" to null out P2pNet instance? Don;t want to destroy instance immediately on Leave()

        public abstract void CreateGame<T>(T t); // really can only be defined in the game-specific implmentation

        protected void _SyncTrivialNewGame()
        {
            // The basics. Implementations *might* call this
            string newGameId = System.Guid.NewGuid().ToString();
            callbacksForNextPoll.Enqueue( () => client.OnGameCreated(newGameId));
        }


        public virtual void JoinGame(string gameP2pChannel)
        {
            string localP2pId = p2p.Join(gameP2pChannel);
            CurrentGameId = gameP2pChannel;
            callbacksForNextPoll.Enqueue( () => client.OnGameJoined(gameP2pChannel, localP2pId));
        }
        public virtual void LeaveGame()
        {
            throw(new Exception("Not implemented yet"));
        }

        public virtual void SendP2pMsg(string toChan, string payload) // TODO: Is there any point in this "generic" message function?
        {
            string appMsgJSON = JsonConvert.SerializeObject(new GameNetP2pMessage(){appMsgType=GameNetP2pMessage.MsgBase, payload=payload});
            p2p.Send(toChan, appMsgJSON);
        }

        public virtual void Loop()
        {
            // Dispatch any locally-enqueued actions
            while(callbacksForNextPoll.Count != 0)
            {
                Action action = callbacksForNextPoll.Dequeue();
                action();
            }

            p2p?.Loop();
        }

        public string LocalP2pId()
        {
            return p2p?.GetId();
        }

        //
        // IP2pNetClient
        //
        public string P2pHelloData() 
        {
            // TODO: might want to put localPlayerData into a larger GameNet-level object
            return client.LocalPlayerData(); // LocalPlayerData() result is already serialized by app
        }
        public void OnPeerJoined(string p2pId, string helloData)
        {
            // See P2pHelloData() comment regarding actual data struct
            client.OnPlayerJoined(p2pId, helloData);
        }
        public void OnPeerLeft(string p2pId)
        {

        }
        public void OnP2pMsg(string from, string to, string payload)
        {
            client.OnP2pMsg(from, to, payload);
        }
    }
}
