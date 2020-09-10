using System;
using System.Diagnostics;

using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;

public static class ConditionalLogger
{
    const string m_logs_define_key = "ENABLE_LOGGING";
    const string m_dev_build_define = "DEVELOPMENT_BUILD";
    const string m_editor_define = "UNITY_EDITOR";

    [Conditional(m_logs_define_key),Conditional(m_dev_build_define), Conditional(m_editor_define)]
    public static void Log(object msg, Object context = null) 
    {
        Debug.Log(msg,context);
    }

    [Conditional(m_logs_define_key), Conditional(m_dev_build_define), Conditional(m_editor_define)]
    public static void LogError(object msg, Object context = null)
    {
        Debug.LogError(msg, context);
    }

    [Conditional(m_logs_define_key), Conditional(m_dev_build_define), Conditional(m_editor_define)]
    public static void LogWarning(object msg, Object context = null)
    {
        Debug.LogWarning(msg, context);
    }

    [Conditional(m_logs_define_key), Conditional(m_dev_build_define), Conditional(m_editor_define)]
    public static void LogException(Exception e)
    {
        Debug.LogException(e);
    }
}

