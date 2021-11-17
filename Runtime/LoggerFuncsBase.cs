using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

#if UNITY_NOTIFICATIONS
using NotificationSamples; 
#endif

[DefaultExecutionOrder(-11001)]
[RequireComponent(typeof(Logger))]
public class LoggerFuncsBase : MonoBehaviour
{
    [SerializeField] protected Logger _logger;

    public virtual BindingFlags ParseFlags => BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public;

    #region Properties
#if UNITY_ANDROID && UNITY_NOTIFICATIONS
    protected virtual GameNotificationsManager m_notification_manager
    {
        get
        {
            Debug.LogError("Need to override GameNotificationsManager.get");
            return null;
        }
    }

#endif
    protected virtual List<string> m_functions
    {
        get
        {
            var list = new List<string>()
            {
                nameof(ClearPrefs),
#if !UNITY_EDITOR && UNITY_NOTIFICATIONS

#if UNITY_ANDROID
		        nameof(AndroidNotificationDelayed),
		        nameof(AndroidAppInfo),
# elif UNTIY_IOS
		        nameof(GetIOSLocalNotification),  
#endif

#endif
            };

            return list;
        }
    } 
#endregion

    protected virtual void Awake()
    {
        var method_names = GetPublicMethodNames();

        _logger = GetComponent<Logger>();
        _logger.Funcs.AddRange(method_names);
    }

    protected List<string> GetPublicMethodNames()
    {
        List<string> method_names;
        try
        {
            var method_info_array = GetType().GetMethods(ParseFlags);

            method_names = method_info_array.Select(x => x.Name).ToList();
            method_names.Remove(".ctor");
            method_names.RemoveAll(x=>x.StartsWith("set_")||x.StartsWith("get_"));
        }
        catch (Exception e)
        {
            ConditionalLogger.LogError($"<b>LoggerFuncs.MethodInfo</b> Exception msg:{e.Message} stack: {e.StackTrace}");
            method_names = m_functions;
        }

        return method_names;
    }

    public void ClearPrefs()
    {
        PlayerPrefs.DeleteAll();
    }

#if UNITY_ANDROID
    void AndroidAppInfo()
    {
        try
        {
            using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivityObject = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                string packageName = currentActivityObject.Call<string>("getPackageName");

                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                using (AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("fromParts", "package", packageName, null))
                using (var intentObject = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS", uriObject))
                {
                    intentObject.Call<AndroidJavaObject>("addCategory", "android.intent.category.DEFAULT");
                    intentObject.Call<AndroidJavaObject>("setFlags", 0x10000000);
                    currentActivityObject.Call("startActivity", intentObject);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    } 
#endif
#if !UNITY_EDITOR && UNITY_NOTIFICATIONS

#if UNITY_ANDROID


    public void AndroidNotificationDelayed()
    {
        var notification = m_notification_manager.CreateNotification();
        notification.Body = "this is notification Body";
        notification.Title = "This is title";
        notification.DeliveryTime = DateTime.Now.AddSeconds(5);
        m_notification_manager.ScheduleNotification(notification);
    }

#endif

#if UNITY_IOS
    public void GetIOSLocalNotification()
    {
        var notification = UnityEngine.iOS.NotificationServices.GetLocalNotification(0);
        ConditionalLogger.Log($"GetIOSLocalNotification. #0 ticks: {notification.fireDate.Ticks} alertBody: {notification.alertBody} ");
        ConditionalLogger.Log($"GetIOSLocalNotification local_length: {UnityEngine.iOS.NotificationServices.localNotificationCount}, scheduled_length: {UnityEngine.iOS.NotificationServices.scheduledLocalNotifications.Length}");
    }
#endif

#endif
    public void ApplicationPage() 
    {
#if UNITY_ANDROID
        AndroidAppInfo();
#endif
    }
    public void GetVersion()
    {
        ConditionalLogger.Log($"<b>LoggerFuncs.GetVersion</b> bundle: {Application.identifier}(version: {Application.version}) unity_version: {Application.unityVersion}");
    }
}
