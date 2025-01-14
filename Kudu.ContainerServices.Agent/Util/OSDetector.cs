﻿using System;

namespace Kudu.ContainerServices.Agent.Util
{
    public class OSDetector
    {
        public static bool IsOnWindows()
        {
            switch (System.Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                case PlatformID.Xbox:
                    return true;
                default:
                    return false;
            }
        }
    }
}
