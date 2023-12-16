using System;
using System.Collections.Generic;

namespace IctBaden.Stonehenge.Vue.Test
{
    public class VueTestData
    {
        public Dictionary<string, string> StartVmParameters { get; set; } = new();
        public int StartVmOnLoadCalled { get; set; }

        public string CurrentRoute { get; set; } = string.Empty;

        public event Func<string, string> DoAction = s => s;

        public string ExecAction(string action) => DoAction.Invoke(action);
    }
}