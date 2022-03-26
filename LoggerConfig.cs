using NLog;
using NLog.Config;
using NLog.Targets;

namespace GridSplitNameKeeper
{
    public class LoggerConfig
    {
        public static void Set()
        {
            var rules = LogManager.Configuration.LoggingRules;


            for (int i = rules.Count - 1; i >= 0; i--) {

                var rule = rules[i];

                if (rule.LoggerNamePattern != "GridSplitNameKeeper")continue;
                rules.RemoveAt(i);
            }

            var config = PluginCore.Instance.Config;

            if (string.IsNullOrEmpty(config.LogFileName))
            {
                LogManager.Configuration.Reload();
                return;
            }

            var logTarget = new FileTarget
            {
                FileName = "Logs/" + config.LogFileName,
                Layout ="${var:logStamp} ${var:logContent}"
            };
            
            var fullRule = new LoggingRule("GridSplitNameKeeper",LogLevel.Debug, logTarget){Final = true};
            
            LogManager.Configuration.LoggingRules.Insert(0,fullRule);
            LogManager.Configuration.Reload();
        }
    }
}