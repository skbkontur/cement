using System.Collections.Generic;

namespace Common
{
    public static class PolicyMapper
    {
        public static LocalChangesPolicy GetLocalChangesPolicy(Dictionary<string, object> parsedArgs)
        {
            var policyMapping = new Dictionary<string, LocalChangesPolicy>
            {
                {"force", LocalChangesPolicy.ForceLocal},
                {"reset", LocalChangesPolicy.Reset},
                {"pullAnyway", LocalChangesPolicy.Pull}
            };

            foreach (var key in policyMapping.Keys)
            {
                if ((int) parsedArgs[key] == 1)
                {
                    return policyMapping[key];
                }
            }
            return LocalChangesPolicy.FailOnLocalChanges;
        }
    }
}
