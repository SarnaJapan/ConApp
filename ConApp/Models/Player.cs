﻿using System.Linq;
using WpfLib.OthelloInterface;

namespace ConApp.Models
{
    /// <summary>
    /// 手動プレイヤー
    /// </summary>
    /// 常に計算不可を返す。対戦選択肢対象外。
    internal class PlayerNull : IOthelloPlayer
    {
        public string Name { get; set; } = "（手動）";
        public string Version => string.Format(Common.VERSION_FORMAT, 2, 0, 0, Common.OPTION_NOMATCH);
        public ulong Calc(ulong p, ulong o) => 0;
        public double[]? Score(ulong p, ulong o) => null;
    }

    /// <summary>
    /// ランダム戦略プレイヤー
    /// </summary>
    /// 置石位置をランダムで返す。評価選択肢対象外。
    internal class PlayerRand : IOthelloPlayer
    {
        public string Name { get; set; } = "ランダム";
        public string Version => string.Format(Common.VERSION_FORMAT, 2, 0, 1, Common.OPTION_NOEVAL);
        public ulong Calc(ulong p, ulong o) => Tools.GetRand(p, o);
        public double[]? Score(ulong p, ulong o) => null;
    }

    /// <summary>
    /// 最大取得数戦略プレイヤー
    /// </summary>
    /// 最大取得数となる置石位置を計算。
    internal class PlayerMaxCount : IOthelloPlayer
    {
        public string Name { get; set; } = "最大取得数";
        public string Version => string.Format(Common.VERSION_FORMAT, 2, 0, 1, "");
        public ulong Calc(ulong p, ulong o)
        {
            var d = Score(p, o);
            var r = d.Where(n => !double.IsNaN(n));
            return r.Any() ? Tools.Pos2Bit(System.Array.IndexOf(d, r.Max())) : 0;
        }
        public double[] Score(ulong p, ulong o)
        {
            var res = new double[64];
            var lm = Tools.LegalMove(p, o);
            ulong p_, o_, s;
            for (int i = 0; i < 64; i++)
            {
                s = Tools.Pos2Bit(i);
                if ((lm & s) != 0)
                {
                    p_ = p;
                    o_ = o;
                    // 一時反転
                    Tools.Flip(ref p_, ref o_, s);
                    // 自分の石をカウント
                    res[i] = Tools.BitCount(p_);
                }
                else
                {
                    res[i] = double.NaN;
                }
            }
            return res;
        }
    }

    /// <summary>
    /// 最小開放度戦略プレイヤー
    /// </summary>
    /// 最小開放度となる置石位置を計算。
    internal class PlayerMinOpen : IOthelloPlayer
    {
        public string Name { get; set; } = "最小開放度";
        public string Version => string.Format(Common.VERSION_FORMAT, 2, 0, 1, "");
        public ulong Calc(ulong p, ulong o)
        {
            var d = Score(p, o);
            var r = d.Where(n => !double.IsNaN(n));
            return r.Any() ? Tools.Pos2Bit(System.Array.IndexOf(d, r.Min())) : 0;
        }
        public double[] Score(ulong p, ulong o)
        {
            var res = new double[64];
            var lm = Tools.LegalMove(p, o);
            var e = ~(p | o);
            ulong p_, o_, s;
            for (int i = 0; i < 64; i++)
            {
                s = Tools.Pos2Bit(i);
                if ((lm & s) != 0)
                {
                    p_ = p;
                    o_ = o;
                    // 一時反転
                    Tools.Flip(ref p_, ref o_, s);
                    // 反転位置周囲の空白をカウント
                    res[i] = Tools.CountOpen(p_ & o, e);
                }
                else
                {
                    res[i] = double.NaN;
                }
            }
            return res;
        }
    }

}
