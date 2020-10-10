using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class FinishCompiling {
    const string compilingKey = "Compiling";
    const string compilingTSKey = "CompilingTimestamp";

    public static Action OnFinishedCompiling;

    static bool compiling;
    static FinishCompiling() {
        compiling = EditorPrefs.GetBool(compilingKey, false);
        EditorApplication.update += Update;
    }
 
    static void Update() {
        if(compiling && !EditorApplication.isCompiling) {
            var beginBinary = long.Parse(EditorPrefs.GetString(compilingTSKey, DateTime.Now.ToBinary().ToString()));
            var compileBegin = DateTime.FromBinary(beginBinary);
            Debug.Log(string.Format("Compiling DONE {0}, elapsed {1}", DateTime.Now, DateTime.Now - compileBegin));
            OnFinishedCompiling?.Invoke();
            compiling = false;
            EditorPrefs.SetBool(compilingKey, false);
        }
        else if (!compiling && EditorApplication.isCompiling) {
            compiling = true;
            EditorPrefs.SetBool(compilingKey, true);
            EditorPrefs.SetString(compilingTSKey, DateTime.Now.ToBinary().ToString());
        }
    }
}