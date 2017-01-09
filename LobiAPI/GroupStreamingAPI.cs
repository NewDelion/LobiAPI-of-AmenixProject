using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LobiAPI.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LobiAPI
{
    /**
     * ストリームに流れてくるユーザ情報はUserMinimal
     */
    #region StreamEventHandler
    public delegate void StreamChatEventHandler(string group_id, Chat chat);
    public delegate void StreamChatDeletedEventHandler(string group_id, string chat_id);
    public delegate void StreamPartEventHandler(string group_id, UserMinimal user);
    public delegate void StreamArchiveEventHandler(string group_id);
    public delegate void StreamConnectedEvent(string group_id);
    public delegate void StreamDisconnectedEvent(string group_id);
    public delegate void StreamFailConnectEvent(string group_id, Exception ex);
    #endregion

    public class GroupStreamingAPI
    {
        private Dictionary<string, GroupStream> StreamCollection = new Dictionary<string, GroupStream>();
        private readonly string AccessToken;

        public GroupStreamingAPI(string access_token)
        {
            this.AccessToken = access_token;
        }

        public void StreamOpen(string group_id)
        {
            if (StreamExists(group_id))
                throw new Exception("既に指定されたグループのストリームが登録されています");
            StreamCollection.Add(group_id, new GroupStream(group_id, AccessToken) { RetryLimit = -1 });
        }
        public void StreamConnect(string group_id)
        {
            if (!StreamExists(group_id))
                throw new Exception("指定されたグループのストリームは登録されていません");
            StreamCollection[group_id].Connect();//既に接続されていた場合、例外が飛んでくる
        }
        public void StreamClose(string group_id)
        {
            if (!StreamExists(group_id))
                throw new Exception("指定されたグループのストリームは登録されていません");//要るか？？
            StreamCollection[group_id].Disconnect();//接続されていない場合、例外が飛んでくる
            StreamCollection.Remove(group_id);
        }
        public void StreamCloseOther(string group_id)
        {
            foreach (var id in StreamCollection.Select(d=>d.Key).ToArray())
            {
                if (id.Equals(group_id))
                    continue;
                StreamCollection[id].Disconnect();
                StreamCollection.Remove(id);
            }
        }
        public void StreamCloseAll()
        {
            foreach (var stream in StreamCollection)
                if (stream.Value.Connected)
                    stream.Value.Disconnect();
            StreamCollection.Clear();
        }
        public bool StreamExists(string group_id)
        {
            return StreamCollection.ContainsKey(group_id);
        }
        public bool StreamIsConnected(string group_id)
        {
            if (!StreamExists(group_id))
                throw new Exception("指定されたグループのストリームは登録されていません");
            return StreamCollection[group_id].Connected;
        }
        public int StreamCount()
        {
            return StreamCollection.Count;
        }

        #region AddHandler
        public void AddHandler(string group_id, StreamChatEventHandler handler)
        {
            if (!StreamExists(group_id))
                throw new KeyNotFoundException("指定されたグループIDのストリームはありません。");
            StreamCollection[group_id].ChatEvent += handler;
        }
        public void AddHandler(string group_id, StreamChatDeletedEventHandler handler)
        {
            if (!StreamExists(group_id))
                throw new KeyNotFoundException("指定されたグループIDのストリームはありません。");
            StreamCollection[group_id].ChatDeletedEvent += handler;
        }
        public void AddHandler(string group_id, StreamPartEventHandler handler)
        {
            if (!StreamExists(group_id))
                throw new KeyNotFoundException("指定されたグループIDのストリームはありません。");
            StreamCollection[group_id].PartEvent += handler;
        }
        public void AddHandler(string group_id, StreamArchiveEventHandler handler)
        {
            if (!StreamExists(group_id))
                throw new KeyNotFoundException("指定されたグループIDのストリームはありません。");
            StreamCollection[group_id].ArchiveEvent += handler;
        }
        public void AddHandler(string group_id, StreamConnectedEvent handler)
        {
            if (!StreamExists(group_id))
                throw new KeyNotFoundException("指定されたグループIDのストリームはありません。");
            StreamCollection[group_id].ConnectedEvent += handler;
        }
        public void AddHandler(string group_id, StreamDisconnectedEvent handler)
        {
            if (!StreamExists(group_id))
                throw new KeyNotFoundException("指定されたグループIDのストリームはありません。");
            StreamCollection[group_id].DisconnectedEvent += handler;
        }
        public void AddHandler(string group_id, StreamFailConnectEvent handler)
        {
            if (!StreamExists(group_id))
                throw new KeyNotFoundException("指定されたグループIDのストリームはありません。");
            StreamCollection[group_id].FailConnectEvent += handler;
        }
        #endregion

        private class GroupStream
        {
            #region イベント
            public event StreamConnectedEvent ConnectedEvent;
            public event StreamDisconnectedEvent DisconnectedEvent;
            public event StreamFailConnectEvent FailConnectEvent;
            public event StreamChatEventHandler ChatEvent;
            public event StreamChatDeletedEventHandler ChatDeletedEvent;
            public event StreamPartEventHandler PartEvent;
            public event StreamArchiveEventHandler ArchiveEvent;
            #endregion

            #region ストリーム情報・設定など
            /// <summary>
            /// グループID
            /// </summary>
            public readonly string GroupID;

            /// <summary>
            /// アクセストークン
            /// </summary>
            public readonly string AccessToken;

            /// <summary>
            /// ネットワークの問題などにより予期せずストリームが切断された場合に自動的に再接続するか
            /// </summary>
            public bool AutoReconnect { get; set; } = true;

            /// <summary>
            /// 再接続のリトライ回数制限。-1で制限なし
            /// </summary>
            public int RetryLimit { get; set; } = 3;

            /// <summary>
            /// 再接続のクールタイム
            /// </summary>
            public int RetryCoolTimeMilliseconds { get; set; } = 100;

            /// <summary>
            /// 接続状態
            /// </summary>
            public bool Connected { get; private set; } = false;

            /// <summary>
            /// ストリームのタスクオブジェクト
            /// </summary>
            private Task ReaderTask = null;

            /// <summary>
            /// 読み込みタスクのキャンセルトークン
            /// </summary>
            private CancellationTokenSource token = null;
            #endregion

            public GroupStream(string group_id, string access_token)
            {
                this.GroupID = group_id;
                this.AccessToken = access_token;
            }

            public void Connect()
            {
                if (Connected)
                    throw new Exception("既に接続されています");
                ReaderTask = Reader();
            }
            public void Disconnect()
            {
                if (!Connected)
                    throw new Exception("接続されていません");//要るかな？？
                token?.Cancel();
                ReaderTask.Wait(1000);//1秒待ってみようかな(てきとう)
                ReaderTask = null;
            }

            private async Task Reader()
            {
                using (HttpClient client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan })
                {
                    for (int i = 1; AutoReconnect && (RetryLimit == -1 || i <= RetryLimit); i++)
                    {
                        try
                        {
                            using (var response = await client.GetAsync($"https://stream.lobi.co/2/group/{GroupID}?token={AccessToken}", HttpCompletionOption.ResponseHeadersRead))
                            using (var reader = new System.IO.StreamReader(await response.Content.ReadAsStreamAsync()))
                            {
                                i = 0;//カウンタ初期化
                                Connected = true;//Connected!!
                                ConnectedEvent?.Invoke(GroupID);
                                while (!reader.EndOfStream)
                                {
                                    token = new CancellationTokenSource();//トークン初期化
                                    var jobj = await _Read(reader, token.Token);//Jsonデータ読み取り
                                    var ev = jobj["event"]?.ToString();
                                    if (ev == "chat")
                                        ChatEvent?.Invoke(GroupID, JsonConvert.DeserializeObject<Chat>(jobj["chat"].ToString()));
                                    else if (ev == "chat_deleted")
                                        ChatDeletedEvent?.Invoke(GroupID, jobj["id"].ToString());
                                    else if (ev == "part")
                                        PartEvent?.Invoke(GroupID, JsonConvert.DeserializeObject<UserMinimal>(jobj["user"].ToString()));
                                    else if (ev == "archive")
                                        ArchiveEvent?.Invoke(GroupID);
                                }
                            }
                            token = null;
                            Connected = false;
                            DisconnectedEvent?.Invoke(GroupID);
                        }
                        catch (OperationCanceledException)
                        {
                            token = null;
                            Connected = false;
                            DisconnectedEvent?.Invoke(GroupID);
                            return;
                        }
                        catch (Exception ex)
                        {
                            if (Connected)
                            {
                                token = null;
                                Connected = false;
                                DisconnectedEvent?.Invoke(GroupID);//必要かな？？
                            }
                            else
                            {
                                token = null;
                                Connected = false;//一応ね
                            }
                            FailConnectEvent?.Invoke(GroupID, ex);
                        }
                        Thread.Sleep(RetryCoolTimeMilliseconds);//クールタイム
                    }
                }
            }

            private async Task<JObject> _Read(System.IO.StreamReader reader, CancellationToken token)
            {
                //boundary
                await reader.ReadLineAsync().WithCancellation(token);
                token.ThrowIfCancellationRequested();
                //Content-Type(イベントが発生するか定期タイムスタンプが来るまで止まる。逆に言うとEmpty以降はawait要らないかも)
                await reader.ReadLineAsync().WithCancellation(token);
                token.ThrowIfCancellationRequested();
                //Empty
                await reader.ReadLineAsync().WithCancellation(token);
                token.ThrowIfCancellationRequested();
                //Json
                string json = await reader.ReadLineAsync().WithCancellation(token);
                token.ThrowIfCancellationRequested();
                return JObject.Parse(json);
            }
        }
    }

    static class CancelEx
    {
        public static Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            return task.IsCompleted // fast-path optimization
                ? task
                : task.ContinueWith(
                    completedTask => completedTask.GetAwaiter().GetResult(),
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }
    }
}
