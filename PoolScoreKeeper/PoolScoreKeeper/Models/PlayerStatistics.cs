﻿namespace PoolScoreKeeper.Models
{
    public class PlayerStatistics
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int WinningEightBalls { get; set; }
        public int LosingEightBalls { get; set; }

        public string WinRatio
        {
            get
            {
                if(Losses == 0)
                    return ((double)Wins * 100).ToString("N0") + "%";

                return (((double)Wins/(double)Losses) * 100).ToString("N0") + "%";
            }
        }

    }
}