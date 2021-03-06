﻿namespace Middle_Notes.Saver.Models.Options
{
    public class SavingOptions
    {
        public string Operation { get; set; }
        public int MaxDegreeOfParallelism { get; set; }
        public int MillisecondDelayAfterCalls { get; set; }
        public int MillisecondDelayAfterPageLoading { get; set; }
        public float MillisecondDelayAfterExceptions { get; set; }
    }
}
