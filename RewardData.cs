using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DinkumTwitchIntegration
{
    public class RewardData
    {
        public string action;
        public JObject data;
        public DateTime added;
        public int delay;

        public RewardData(string action, JObject data, int delay, DateTime added)
        {
            this.action = action;
            this.data = data;
            this.delay = delay;
            this.added = added;
        }
    }
}
