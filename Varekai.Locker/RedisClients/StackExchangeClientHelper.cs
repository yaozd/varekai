using System;
using StackExchange.Redis;

namespace Varekai.Locker.RedisClients
{
    public static class StackExchangeClientHelper
    {
        public static string ExecSetScript(
            Func<IDatabase> stackExchangeDatabase,
            Func<string> script,
            Func<object> parameters,
            Func<string> successResult,
            Func<string> failureResult)
        {
            return ExecScript(
                stackExchangeDatabase,
                script,
                parameters,
                res => res != null && res.Equals("OK"),
                successResult,
                failureResult
            );
        }

        public static string ExecReleaseOrConfirmScript(
            Func<IDatabase> stackExchangeDatabase,
            Func<string> script,
            Func<object> parameters,
            Func<string> successResult,
            Func<string> failureResult)
        {
            return ExecScript(
                stackExchangeDatabase,
                script,
                parameters,
                res => res != null && res.Equals("1"),
                successResult,
                failureResult
            );
        }

        public static string ExecScript(
            Func<IDatabase> stackExchangeDatabase,
            Func<string> script,
            Func<object> parameters,
            Func<string, bool> testResultCorrectness,
            Func<string> successResult,
            Func<string> failureResult)
        {
            var database = stackExchangeDatabase();

            var result = database
                .ScriptEvaluate(
                    LuaScript.Prepare(script()),
                    parameters())
                .ToString();

            return testResultCorrectness(result)
                ? successResult()
                : failureResult();
        }
    }
}

