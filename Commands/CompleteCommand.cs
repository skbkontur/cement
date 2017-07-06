using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Logging;

namespace Commands
{
    public class CompleteCommand : Command
    {
        public CompleteCommand()
            : base(new CommandSettings
            {
                LogPerfix = "COMPLETE",
                LogFileName = null,
                MeasureElapsedTime = false,
                Location = CommandSettings.CommandLocation.Any,
                IsHiddenCommand = true,
                NoElkLog = true
            })
        {
        }

        private string[] otherArgs;

        protected override int Execute()
        {
            var buffer = otherArgs.Length == 0
                ? ""
                : otherArgs[0];

            if (otherArgs.Length > 1)
            {
                int pos;
                if (int.TryParse(otherArgs[1], out pos) && buffer.Length > pos)
                    buffer = buffer.Substring(0, pos);
            }

            LogHelper.SaveLog($"[COMPLETE] '{buffer}'");
            var result = new CompleteCommandAutomata(Log).Complete(buffer);
            PrintList(result);

            return 0;
        }

        private static void PrintList(IEnumerable<string> list)
        {
            Console.WriteLine(string.Join("\n", list.OrderBy(x => x)));
        }

        protected override void ParseArgs(string[] args)
        {
            otherArgs = args.Skip(1).ToArray();
        }

        public override string HelpMessage => "";
    }
}