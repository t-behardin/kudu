﻿namespace Kudu.Core.Commands
{
    public class CommandResult
    {
        public string Output { get; set; }
        public string Error { get; set; }
        public int ExitCode { get; set; }
    }
}
