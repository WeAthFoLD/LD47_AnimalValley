using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

using UDebug = UnityEngine.Debug;

public static class XDebug {
    public const string DEBUG_MACRO = "DEBUG";

    public static void Watch(string format, params object[] pars) {
        UDebug.Log(string.Format(format, pars) +
                   "\nCPAPI:{\"cmd\":\"Watch\" }");
    }

    [Conditional(DEBUG_MACRO)]
    public static void Log(string format, params object[] pars) {
        UDebug.Log(string.Format(format, pars));
    }

    [Conditional(DEBUG_MACRO)]
    public static void Error(string format, params object[] args) {
        UDebug.LogError(string.Format(format, args));
    }

    [Conditional(DEBUG_MACRO)]
    public static void Warning(string format, params object[] args) {
        UDebug.LogWarning(string.Format(format, args));
    }

    [Conditional(DEBUG_MACRO)]
    public static void Assert(bool condition, string msg = "Assertion error") {
        if (!condition) {
            throw new Exception("Assertion error: " + msg);
        }
    }

    #if UNITY_EDITOR

    public static void Test(bool condition, string testName) {
        string prefix = condition ? "<color=green>TEST PASS:</color>" : "<color=red>TEST FAIL:</color>";
        UDebug.Log(prefix + " " + testName);
    }

    #endif

}
