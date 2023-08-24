using ConApp.Controllers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ConApp.Views
{
    /// <summary>
    /// メインビュー
    /// </summary>
    /// - [MVVM]
    ///   - XAMLで実装する
    /// - [MVC]
    ///   - Consoleで実装する
    internal class MainView
    {
        #region メインビュー

        /// <summary>
        /// 石データ参照先
        /// </summary>
        /// コンストラクタで参照設定。Model管理クラスで変換済み。
        private readonly Stone[] Data;

        /// <summary>
        /// ステータスデータ参照先
        /// </summary>
        /// コンストラクタで参照設定。Model管理クラスで変換済み。
        private readonly ObservableCollection<string> Status;

        /// <summary>
        /// プレイヤー情報リスト
        /// </summary>
        internal List<string> PlayerList = new List<string>();

        /// <summary>
        /// 起動引数
        /// </summary>
        internal int[] Args = new int[] { 0, 0, 0 };

        /// <summary>
        /// ステータス（一時保存）
        /// </summary>
        private string SaveStat = "";

        /// <summary>
        /// 改行フラグ
        /// </summary>
        private bool AddLF = false;

        /// <summary>
        /// 使用方法
        /// </summary>
        /// @note 使用方法の１行目がタイトルとなる
        private const string INFO_USAGE = "Console Othello for .NET 6.0 [Version 1.0.0]\n\n" +
            "ConApp [black] [white] [candidate]\n\n" +
            "Option\n" +
            "  black:\tBlack player number\n" +
            "  white:\tWhite player number\n" +
            "  candidate:\t0: Hide, 1: Show(・), 2: Show( 0-63), 3: Show(a1-h8)\n\n";

        /// <summary>
        /// 盤サイズ
        /// </summary>
        /// @note 盤サイズはコンストラクタで石データ数から判断する
        private readonly int Size = 8;

        /// <summary>
        /// 列用文字
        /// </summary>
        /// @note 盤サイズ(@ref ConApp.Models.Common.SIZE)は列用文字数まで変更可能だが９以上は候補表示が崩れる
        private static readonly string[] L0 = new string[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", };

        /// <summary>
        /// 候補表示パタ－ン( 0-63)
        /// </summary>
        private readonly List<string> S2 = new List<string>();

        /// <summary>
        /// 候補表示パタ－ン(a1-h8)
        /// </summary>
        private readonly List<string> S3 = new List<string>();

        /// <summary>
        /// コンストラクタ：盤サイズ設定と盤面表示準備
        /// </summary>
        /// <param name="data">石データ参照先</param>
        /// <param name="status">ステータスデータ参照先</param>
        internal MainView(Stone[] data, ObservableCollection<string> status)
        {
            Data = data;
            Status = status;

            // 盤サイズ変更対応
            Size = Convert.ToInt32(Math.Sqrt(Data.Length));
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    S2.Add($"{i * Size + j,2}");
                    S3.Add($"{L0[j]}{i + 1}");
                }
            }
            // 警告表示
            if (Size != 8)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"The board size is [{Size}].");
                Console.WriteLine("The display will collapse when the option is [candidate=2,3].");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// 盤面情報取得
        /// </summary>
        /// <returns>盤面情報</returns>
        private string[] GetBoardInfo()
        {
            var ret = new string[Size + 2];
            var rcd = Status[1].Split(",");
            // 石数＆プレイヤー名称
            ret[0] += $"　　　{Status[2],-6}　　{Status[3]}";
            ret[1] += $"　　　{Status[4],-6}　　{Status[5]}";
            // 履歴は標準の盤サイズのみ対応
            if (Size == 8 && !"".Equals(rcd[0]))
            {
                try
                {
                    ret[3] = "　　　";
                    ret[7] = "　　　";
                    for (int i = 0; i < 24 && i < rcd.Length; i++)
                    {
                        ret[3] += $"{rcd[i],2},";
                        ret[7] += "-1".Equals(rcd[i]) ? "-1," : $"{S3[int.Parse(rcd[i])]},";
                    }
                    ret[4] = "　　　";
                    ret[8] = "　　　";
                    for (int i = 24; i < 48 && i < rcd.Length; i++)
                    {
                        ret[4] += $"{rcd[i],2},";
                        ret[8] += "-1".Equals(rcd[i]) ? "-1," : $"{S3[int.Parse(rcd[i])]},";
                    }
                    ret[5] = "　　　";
                    ret[9] = "　　　";
                    for (int i = 48; i < rcd.Length; i++)
                    {
                        ret[5] += $"{rcd[i],2},";
                        ret[9] += "-1".Equals(rcd[i]) ? "-1," : $"{S3[int.Parse(rcd[i])]},";
                    }
                }
                catch (Exception)
                {
                }
            }
            return ret;
        }

        /// <summary>
        /// 盤面表示
        /// </summary>
        internal void ShowBoard()
        {
            // 盤面生成
            var info = GetBoardInfo();
            var str = AddLF ? "\n\n  ｜" : "\n  ｜";
            for (int i = 0; i < Size; i++)
            {
                str += $"{L0[i],2}";
            }
            str += info[0];
            str += "\n―＋";
            for (int i = 0; i < Size; i++)
            {
                str += "―";
            }
            str += info[1];
            str += "\n";
            int p;
            for (int i = 0; i < Size; i++)
            {
                str += $"{i + 1,2}｜";
                for (int j = 0; j < Size; j++)
                {
                    p = i * Size + j;
                    str += Data[p].Color == "Black" ? "●" : Data[p].Color == "White" ? "○" : Data[p].BackColor == "LimeGreen" ? Args[2] == 1 ? "・" : Args[2] == 2 ? S2[p] : Args[2] == 3 ? S3[p] : "　" : "　";
                }
                str += info[2 + i];
                str += "\n";
            }
            // 盤面表示
            if (Status[0].EndsWith("パス"))
            {
                // パス時は表示更新不要。カーソル復帰。
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write("              ");
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"{Status[0]} > ");
                AddLF = false;
            }
            else if (Status[0].Split("／").Length == 2)
            {
                // 終了時は表示更新不要。カーソル復帰。
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.WriteLine($"{Status[0]}");
                AddLF = false;
            }
            else if (SaveStat.Equals(Status[0]) || Status[0].EndsWith("計算中…"))
            {
                // ステータス未更新時は表示更新不要。カーソル改行復帰。
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write($"{Status[0]} > ");
                AddLF = true;
            }
            else
            {
                // ステータス更新時は表示更新必要。
                Console.WriteLine(str);
                Console.Write($"{Status[0]} > ");
                AddLF = false;
            }
            SaveStat = Status[0];
        }

        /// <summary>
        /// タイトル表示
        /// </summary>
        internal static void ShowTitle()
        {
            Console.Title = INFO_USAGE.Split("\n")[0];
        }

        /// <summary>
        /// 使用方法表示
        /// </summary>
        internal void ShowUsage()
        {
            string str = "\n" + INFO_USAGE + "Player list\n";
            for (int i = 0; i < PlayerList.Count; i++)
            {
                str += $"  {i}:\t{PlayerList[i]}\n";
            }
            Console.WriteLine(str);
        }

        /// <summary>
        /// 起動引数確認
        /// </summary>
        /// <param name="args">起動引数</param>
        /// <returns>結果</returns>
        /// @note 判断は使用方法(@ref INFO_USAGE)参照
        internal bool CheckArgs(string[] args)
        {
            bool ret = false;
            try
            {
                int b, w, s;
                switch (args.Length)
                {
                    case 2:
                        b = int.Parse(args[0]);
                        w = int.Parse(args[1]);
                        if (b >= 0 && b < PlayerList.Count && w >= 0 && w < PlayerList.Count)
                        {
                            Args[0] = b;
                            Args[1] = w;
                            Args[2] = 0;
                            ret = true;
                        }
                        break;
                    case 3:
                        b = int.Parse(args[0]);
                        w = int.Parse(args[1]);
                        s = int.Parse(args[2]);
                        if (b >= 0 && b < PlayerList.Count && w >= 0 && w < PlayerList.Count && s >= 0 && s <= 3)
                        {
                            Args[0] = b;
                            Args[1] = w;
                            Args[2] = s;
                            ret = true;
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {
            }
            return ret;
        }

        /// <summary>
        /// 入力受付確認
        /// </summary>
        /// <returns>結果</returns>
        /// ステータスが計算中のリターン入力はカーソルを復帰して破棄
        /// @note 判断はステータス文字列(ConApp.Models.Common.StatusString)参照
        internal bool CheckInput()
        {
            var ret = true;
            if (Status[0].EndsWith("計算中…"))
            {
                Console.SetCursorPosition(15, Console.CursorTop - 1);
                ret = false;
            }
            return ret;
        }

        /// <summary>
        /// 対戦継続確認
        /// </summary>
        /// <returns>結果</returns>
        /// ステータスが対戦結果表示状態なら終了
        /// @note 判断はステータス文字列(ConApp.Models.Common.StatusString)参照
        internal bool IsContinue()
        {
            var ret = true;
            if (Status[0].Split("／").Length == 2)
            {
                ret = false;
            }
            return ret;
        }

        /// <summary>
        /// 位置文字列->位置数値変換
        /// </summary>
        /// <param name="str">位置文字列( 0-63), (a1-h8)</param>
        /// <returns>位置数値</returns>
        internal int NormalisePos(string str)
        {
            // ( 0-63)形式
            var ret = S2.IndexOf(str.PadLeft(2));
            // (a1-h8)形式
            if (ret == -1)
            {
                ret = S3.IndexOf(str);
            }
            return ret;
        }

        /// <summary>
        /// 入力文字列取得
        /// </summary>
        /// <returns>入力文字列</returns>
        internal static string GetLine()
        {
            return Console.ReadLine() ?? "";
        }

        # endregion
    }

}
