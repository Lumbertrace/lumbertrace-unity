using System;
using UnityEngine;

namespace Lumbertrace.Unity
{
    public class LumbertraceClientDetails 
    {
        private string _device;
        private string _platform;
        private string _version;
        
        public string Device => _device;
        public string Platform => _platform;
        public string Version => _version;

        public LumbertraceClientDetails(string device, string platform, string version)
        {
            _device = device;
            _platform = platform;
            _version = version;
        }

        public static LumbertraceClientDetails CreateWithAutoFill()
        {
            string device = SystemInfo.deviceName;
            string version = Application.version;
            string platform = Application.platform.ToString();
                
            return new LumbertraceClientDetails(device, platform, version);
        }
    }
}