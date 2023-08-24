#define MODE_V1

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ConApp.Models;
using ConApp.Utils;
using ConApp.Views;

#if MODE_V1
using GameMaster = ConApp.Models.MasterV1;
using IGamePlayer = WpfLib.OthelloInterface.IOthelloPlayerV1;
#else
using GameMaster = ConApp.Models.Master;
using IGamePlayer = WpfLib.OthelloInterface.IOthelloPlayer;
#endif

namespace ConApp.Controllers
{
    /// <summary>
    /// メインコントローラ
    /// </summary>
    /// - [MVVM]
    ///   - Model(Master)はNotificationObjectを実装して更新を通知する
    ///   - ViewModel(Board/Stone)はNotificationObjectを実装してViewとBindingする
    /// - [MVC]
    ///   - 共通化するためModel(Master)はNotificationObjectを実装して更新を通知する
    ///   - Binding機能が無いため(Stone)にはNotificationObjectを実装しない
    ///   - ただし(Board)にはNotificationObjectを実装して更新をまとめて通知する
    ///   - 更新通知の契機でControllerが(Board/Stone)のプロパティを基にViewを更新する
    internal class MainController
    {
        #region メインコントローラ

        /// <summary>
        /// Model管理クラス
        /// </summary>
        private readonly Board board;

        /// <summary>
        /// Viewクラス
        /// </summary>
        private readonly MainView view;

        /// <summary>
        /// コンストラクタ：Model管理クラスとViewクラスの制御
        /// </summary>
        /// <param name="args">起動引数</param>
        internal MainController(string[] args)
        {
            // Model管理クラス
            board = new Board();
            board.PropertyChanged += StatusChanged;

            // Viewクラス
            view = new MainView(board.Data, board.Status);
            var playerlist = new List<string>();
            var playerkeys = new List<string>();
            foreach (KeyValuePair<string, IGamePlayer> kvp in board.Master.PlayerMap)
            {
                playerlist.Add($"{kvp.Key}\t{kvp.Value.Version}");
                playerkeys.Add(kvp.Key);
            }
            view.PlayerList = playerlist;
            MainView.ShowTitle();

            // 起動引数確認
            if (view.CheckArgs(args))
            {
                // プレイヤー設定
                board.Master.PlayerB = board.Master.PlayerMap.GetValueOrDefault(playerkeys[view.Args[0]], board.Master.PlayerMap.First().Value);
                board.Master.PlayerW = board.Master.PlayerMap.GetValueOrDefault(playerkeys[view.Args[1]], board.Master.PlayerMap.First().Value);
                // 候補表示設定
                board.Master.ShowCandidate = view.Args[2] > 0;

                // 対戦処理
                string str = "";
                board.GameStart();
                // 対戦継続確認
                while (view.IsContinue() && !"q".Equals(str))
                {
                    str = MainView.GetLine();
                    // 入力受付確認
                    if (view.CheckInput())
                    {
                        board.GameSelectPos(view.NormalisePos(str));
                    }
                }
            }
            else
            {
                view.ShowUsage();
            }
        }

        #endregion

        #region 通知

        /// <summary>
        /// ステータス変更通知処理
        /// </summary>
        /// <param name="sender">送信元</param>
        /// <param name="e">イベント</param>
        /// 通知されたステータスを確認してビューを更新する
        private void StatusChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!"Status".Equals(e.PropertyName))
            {
                return;
            }
            var p = sender as Board;
            if (p == null)
            {
                return;
            }
            view.ShowBoard();
        }

        #endregion
    }

    /// <summary>
    /// 石
    /// </summary>
    internal class Stone
    {
        #region 石

        /// <summary>
        /// 行位置
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// 列位置
        /// </summary>
        public int ColumnIndex { get; set; }

        /// <summary>
        /// <see cref="BackColor"/>
        /// </summary>
        private string backColor = "";
        /// <summary>
        /// 背景色
        /// </summary>
        public string BackColor
        {
            get => backColor;
            set
            {
                if (value != backColor)
                {
                    backColor = value;
                }
            }
        }

        /// <summary>
        /// <see cref="Color"/>
        /// </summary>
        private string color = "";
        /// <summary>
        /// 石色
        /// </summary>
        public string Color
        {
            get => color;
            set
            {
                if (value != color)
                {
                    color = value;
                }
            }
        }

        /// <summary>
        /// <see cref="LastColor"/>
        /// </summary>
        private string lastColor = "";
        /// <summary>
        /// 更新色
        /// </summary>
        public string LastColor
        {
            get => lastColor;
            set
            {
                if (value != lastColor)
                {
                    lastColor = value;
                }
            }
        }

        /// <summary>
        /// <see cref="this[int id]"/>
        /// </summary>
        /// @note インデクサは[(文字色,文字列),情報数]の構成。ビューと一致させること。
        private readonly string[] info = new string[] { "White", "", "White", "", "White", "", "White", "", };
        /// <summary>
        /// インデクサ
        /// </summary>
        public string this[int id]
        {
            get => info[id];
            set
            {
                if (value != info[id])
                {
                    info[id] = value;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 盤
    /// </summary>
    internal class Board : NotificationObject
    {
        #region 盤

        /// <summary>
        /// ゲームマスター
        /// </summary>
        /// メインモデルとして起動時に生成する
        public GameMaster Master = new GameMaster();

        /// <summary>
        /// 石データ
        /// </summary>
        public Stone[] Data = new Stone[Common.SIZE * Common.SIZE];

        /// <summary>
        /// コンストラクタ：ゲームマスターと石データの初期設定
        /// </summary>
        /// - 変更通知イベントハンドラ登録
        ///   - 各データをモデル用からビュー用に変換する処理を登録する
        /// - 関連ビューモデル生成
        ///   - 盤(@ref Board)が所有する石(@ref Stone)を生成する
        public Board()
        {
            Master.PropertyChanged += BackPropertyChanged;
            Master.PropertyChanged += DataPropertyChanged;
            Master.PropertyChanged += InfoPropertyChanged;
            Master.PropertyChanged += StatusPropertyChanged;

            for (int i = 0; i < Common.SIZE; i++)
            {
                for (int j = 0; j < Common.SIZE; j++)
                {
                    Data[i * Common.SIZE + j] = new Stone { RowIndex = i, ColumnIndex = j, BackColor = "Green", Color = "Transparent", LastColor = "Transparent", };
                }
            }
        }

        #endregion

        #region プロパティ

        /// <summary>
        /// <see cref="Status"/>
        /// </summary>
        /// @note ステータスは[(ステータス,ツールチップ),領域数]の構成。ビューと一致させること。
        private ObservableCollection<string> status = new ObservableCollection<string>() { "", "", "", "", "", "", };
        /// <summary>
        /// ステータス
        /// </summary>
        public ObservableCollection<string> Status
        {
            get => status;
            set
            {
                status = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region コマンド

        /// <summary>
        /// 開始コマンド
        /// </summary>
        public void GameStart()
        {
            Master.GameStart();
        }

        /// <summary>
        /// 選択コマンド
        /// </summary>
        public void GameSelectPos(int pos)
        {
            Master.GameSelectPos(pos);
        }

        #endregion

        #region 通知

        /// <summary>
        /// 背景色変更通知処理
        /// </summary>
        /// <param name="sender">送信元</param>
        /// <param name="e">イベント</param>
        /// 通知された合法手の配列を基に背景色を設定する
        private void BackPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!"Back".Equals(e.PropertyName))
            {
                return;
            }
            var p = sender as GameMaster;
            if (Data.Length != p?.Back.Length)
            {
                return;
            }
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i].BackColor = p.Back[i] ? "LimeGreen" : "Green";
            }
        }

        /// <summary>
        /// 石色変更通知処理
        /// </summary>
        /// <param name="sender">送信元</param>
        /// <param name="e">イベント</param>
        /// 通知された配列と既存の石色を基に石色と更新色を設定する
        private void DataPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!"Data".Equals(e.PropertyName))
            {
                return;
            }
            var p = sender as GameMaster;
            if (Data.Length != p?.Data.Length)
            {
                return;
            }
            for (int i = 0; i < Data.Length; i++)
            {
                // 新規なら更新色を着手色に設定。反転なら更新色をダミーに設定。
                // 連続更新時に更新色が残り反転処理が重複するため消去すること。
                // 反転用データトリガ判定のため、石色->更新色の順序とすること。
                Data[i].LastColor = "Transparent";
                switch (p.Data[i])
                {
                    case Common.BLACK:
                        string b = (Data[i].Color == "Transparent") ? "Gray" : (Data[i].Color == "White") ? "Red" : "Black";
                        Data[i].Color = "Black";
                        Data[i].LastColor = b;
                        break;
                    case Common.WHITE:
                        string w = (Data[i].Color == "Transparent") ? "Gray" : (Data[i].Color == "Black") ? "Red" : "White";
                        Data[i].Color = "White";
                        Data[i].LastColor = w;
                        break;
                    default:
                        Data[i].Color = "Transparent";
                        Data[i].LastColor = "Transparent";
                        break;
                }
            }
        }

        /// <summary>
        /// 情報変更通知処理
        /// </summary>
        /// <param name="sender">送信元</param>
        /// <param name="e">イベント</param>
        /// 通知された各種情報用の配列をインデクサに変換する
        /// @note 配列は[((文字色,文字列),情報数),石数]の構成。ビューと一致させること。
        private void InfoPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!"Info".Equals(e.PropertyName))
            {
                return;
            }
            var p = sender as GameMaster;
            if (Data.Length * 8 != p?.Info.Length)
            {
                return;
            }
            for (int i = 0; i < Data.Length; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Data[i][j] = p.Info[i * 8 + j];
                }
            }
        }

        /// <summary>
        /// ステータス変更通知処理
        /// </summary>
        /// <param name="sender">送信元</param>
        /// <param name="e">イベント</param>
        /// 通知されたステータスをビューのステータスに変換する
        /// @note 通知ステータス(@ref ConApp.Models.GameStatus)とステータス文字列(@ref ConApp.Models.Common::StatusString)から生成。ビューと一致させること。
        private void StatusPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!"Status".Equals(e.PropertyName))
            {
                return;
            }
            var p = sender as GameMaster;
            if (p == null || !Common.StatusString.ContainsKey(p.Status.StatusB) || !Common.StatusString.ContainsKey(p.Status.StatusW))
            {
                return;
            }
            var sb = Common.StatusString[p.Status.StatusB];
            var sw = Common.StatusString[p.Status.StatusW];
            status[0] = sb.Equals("") ? ("○：" + sw) : sw.Equals("") ? ("●：" + sb) : ("●：" + sb + "／○：" + sw);
            status[1] = string.Join(",", p.Status.Record);
            status[2] = "●：" + p.Status.CountB;
            status[3] = Master.PlayerB.Name;
            status[4] = "○：" + p.Status.CountW;
            status[5] = Master.PlayerW.Name;
            Status = status;
        }

        #endregion
    }

}
