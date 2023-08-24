﻿using System.Linq;
using WpfLib.OthelloInterface;

namespace ConApp.Models
{
    /// <summary>
    /// 手動プレイヤー
    /// </summary>
    /// 常に計算不可を返す。対戦選択肢対象外。
    internal class PlayerNullV1 : IOthelloPlayerV1
    {
        public string Name { get; set; } = "（手動）";
        public string Version => string.Format(Common.VERSION_FORMAT, 1, 0, 0, Common.OPTION_NOMATCH);
        public int Calc(int color, int[] data) => -1;
        public double[]? Score(int color, int[] data) => null;
    }

    /// <summary>
    /// ランダム戦略プレイヤー
    /// </summary>
    /// 置石位置をランダムで返す。評価選択肢対象外。
    internal class PlayerRandV1 : IOthelloPlayerV1
    {
        public string Name { get; set; } = "ランダム";
        public string Version => string.Format(Common.VERSION_FORMAT, 1, 0, 1, Common.OPTION_NOEVAL);
        public int Calc(int color, int[] data) => ToolsV1.GetRand(color, data);
        public double[]? Score(int color, int[] data) => null;
    }

    /// <summary>
    /// 最大取得数戦略プレイヤー
    /// </summary>
    /// 最大取得数となる置石位置を計算。
    internal class PlayerMaxCountV1 : IOthelloPlayerV1
    {
        public string Name { get; set; } = "最大取得数";
        public string Version => string.Format(Common.VERSION_FORMAT, 1, 0, 1, "");
        public int Calc(int color, int[] data)
        {
            var d = Score(color, data);
            var r = d.Where(n => !double.IsNaN(n));
            return r.Any() ? System.Array.IndexOf(d, r.Max()) : -1;
        }
        public double[] Score(int color, int[] data)
        {
            var res = new double[data.Length];
            for (int i = 0; i < res.Length; i++)
            {
                var count = ToolsV1.GetFlip(color, data, i).Count;
                if (count > 0)
                {
                    res[i] = count;
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
    internal class PlayerMinOpenV1 : IOthelloPlayerV1
    {
        public string Name { get; set; } = "最小開放度";
        public string Version => string.Format(Common.VERSION_FORMAT, 1, 0, 1, "");
        public int Calc(int color, int[] data)
        {
            var d = Score(color, data);
            var r = d.Where(n => !double.IsNaN(n));
            return r.Any() ? System.Array.IndexOf(d, r.Min()) : -1;
        }
        public double[] Score(int color, int[] data)
        {
            var res = new double[data.Length];
            for (int i = 0; i < res.Length; i++)
            {
                var flip = ToolsV1.GetFlip(color, data, i);
                if (flip.Count > 0)
                {
                    // 置石位置はカウント対象外
                    flip.Remove(i);
                    // 反転位置周囲の空白をカウント
                    res[i] = ToolsV1.CountOpen(data, flip);
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
