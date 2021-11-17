using System;
using System.Collections.Generic;
using System.Linq;

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using UnityUseful.CorotineInstructionsWrappers;
using UnityUseful.Misc;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using ContextMenu = Sirenix.OdinInspector.ButtonAttribute;
#endif

[DefaultExecutionOrder(-11000)]
[RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
public class Logger : MonoBehaviour
{
    [Serializable]
    public class LogEntry
    {
        public string Condition;
        public string StackTrace;
        [Space]
        public LogType LogType;
        [Space]
        public TimeSpan Time;
        
        public override string ToString()
        {
            var color = "";

            switch (LogType)
            {
                case LogType.Exception: color = "red"; break;
                case LogType.Warning: color = "yellow"; break;
                case LogType.Error: color = "red"; break;

                default:
                    color = "white";
                    break;
            }

            var text = $"[{Time:hh\\:mm\\:ss}] <color={color}>({LogType})</color>, {Condition}";
            
            return text;
        }
    }

    const int MAX_CAPACITY = 1000;

    [Header("Prefs:")]
    [SerializeField] GameObject log_pref;
    [SerializeField] GameObject func_btn_pref;

    [Header("UI:")]
    [SerializeField] ScrollRect m_scroll_view;
    [SerializeField] TextMeshProUGUI m_stack_text;
    [SerializeField] TMP_InputField m_page_navigation_if;
    [SerializeField] List<Toggle> m_sort_toggles;
    [SerializeField] Canvas m_canvas;
    [SerializeField] CanvasScaler m_canvas_scaler;

    [Header("Other Panels:")]
    [SerializeField] List<GameObject> m_main_scroll_contents;

    [Header("Debug functions")]
    [SerializeField] List<string> m_funcs;
    [Space]
    [SerializeField] Transform m_func_parent;

    [Header("Buttons:")]
    [SerializeField] Toggle m_change_state;
    [SerializeField] FancyToggle m_f_toggle;
    [Space]
    [SerializeField] Button m_clear;
    [SerializeField] Button m_close;
    [SerializeField] Button m_next_page;
    [SerializeField] Button m_prev_page;

    [Header("Initialization")]
    [SerializeField, Range(1, 20)] int m_max_elements = 10;
    [Space]
    [SerializeField] List<LogType> m_allowed_logs;
    [Space]
    [SerializeField] bool m_stop_receiving;
    [SerializeField] bool m_block_change;
    [SerializeField] bool m_allow_console;

    [Header("Runtime")]
    [SerializeField] List<LogEntry> m_logs = new List<LogEntry>(MAX_CAPACITY);
    [SerializeField] List<LogEntry> m_sorted_logs = new List<LogEntry>(MAX_CAPACITY);
    [SerializeField] List<LogItemView> m_log_views;
    [SerializeField] int m_current_page = 0;
    [SerializeField] int m_pages_count = 1;
    
    #region Properties
    
    public bool StopReceiving { get => m_stop_receiving; set => m_stop_receiving = value; }
    public bool AllowConsole { get => m_allow_console; set => m_allow_console = value; }
    public List<string> Funcs { get => m_funcs; }

    public int CurrentPage
    {
        get => m_current_page;
        set
        {
            m_current_page = Mathf.Clamp(value, 0, m_pages_count - 1);

            m_pages_count = ((m_sorted_logs.Count - 1) / m_max_elements) + 1;
            
            var text = $"{m_current_page + 1}/{m_pages_count}";
            m_page_navigation_if.placeholder.GetComponent<TextMeshProUGUI>().text = text;

            UpdateCurrentPage();
        }
    }

    #endregion

    #region MonoCallbacks

    void Awake()
    {
        DontDestroyOnLoad(this);

        m_canvas_scaler = GetComponent<CanvasScaler>();

        #region SortLogsListeners
        
        for (var i = 0; i < m_sort_toggles.Count; i++)
        {
            var index = i;
            var log_type = (LogType)index;
            var current = m_sort_toggles?[i];

            if (current)
            {
                current.isOn = m_allowed_logs.Contains(log_type);
                current.onValueChanged.AddListener(state =>
                {
                    SortText(state, log_type);
                });
            }
        }
        
        #endregion

        #region DragEventOfStateToggle
        
        var drag_event = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Drag
        };

        var rect = m_change_state.GetComponent<RectTransform>();
        drag_event.callback.AddListener(data => { rect.anchoredPosition += ((PointerEventData)data).delta; });
        m_change_state.GetComponent<EventTrigger>().triggers.Add(drag_event);
        
        #endregion

        #region PageToggleInit
        
        var bg_transform = m_change_state.targetGraphic.transform;
        m_change_state.onValueChanged.AddListener(state =>
        {
            var target_angle = Quaternion.Euler(0, 0, state ? 90 : -90);
            this.ProgressTimerV(0.25f, (prog, delta) =>
            {
                bg_transform.rotation = Quaternion.Lerp(bg_transform.rotation, target_angle, prog);
            });
            m_canvas.enabled = state;
        });

        m_f_toggle.OnValueChangedEndAnim += state =>
        {
            //ConditionalLogger.Log($"<b>Logger.OnValueChangedEndAnim</b> {state}");
            var target = m_main_scroll_contents[state];

            m_scroll_view.content.gameObject.SetActive(false);
            m_scroll_view.content = target.GetComponent<RectTransform>();

            target.SetActive(true);
            
            m_page_navigation_if.transform.parent.gameObject.SetActive(state == 0);
        };

        m_main_scroll_contents.ForEach(x => x.SetActive(false));
        m_main_scroll_contents[0].SetActive(true);
        
        #endregion

        m_clear.onClick.AddListener(() =>
        {
            //m_main_scroll_contents[0].transform.DestroyChildren();
            m_logs.Clear();
            m_sorted_logs.Clear();
            m_pages_count = 1;
            CurrentPage = 0;
        });
        m_close.onClick.AddListener(() =>
        {
            m_stack_text.transform.parent.gameObject.SetActive(false);
        });

        m_next_page.onClick.AddListener(()=>CurrentPage++);
        m_prev_page.onClick.AddListener(()=>CurrentPage--);
        m_page_navigation_if.onValueChanged.AddListener(page =>
        {
            if (!string.IsNullOrEmpty(page))
            {
                var page_id = int.Parse(page) - 1;

                CurrentPage = page_id;

                m_page_navigation_if.text = string.Empty;
            }
        });
        
        OnRectTransformDimensionsChange();
    }
    void Start()
    {
        foreach (var func_name in m_funcs)
        {
            var btn = Instantiate(func_btn_pref, m_func_parent);

            var func_btn = btn.GetComponent<Button>();
            func_btn.onClick.AddListener(() => gameObject.SendMessage(func_name));

            var child_text = btn.GetComponentInChildren<TextMeshProUGUI>();
            child_text.text = func_name;

            btn.SetActive(true);
        }

        m_log_views = new List<LogItemView>(m_max_elements);
        
        for (int i = 0; i < m_max_elements; i++)
        {
            m_log_views.Add(null);
            
            var item = GetFromPool(i);

            m_log_views[i] = item;
        }
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ChangeDebuggerState();
#endif
    }
    void OnEnable()
    {
        Application.logMessageReceived += AddLog;
    }
    void OnDisable()
    {
        Application.logMessageReceived -= AddLog;
    }

    void OnRectTransformDimensionsChange()
    {
        if (m_canvas_scaler)
        {
            switch (Screen.orientation)
            {
                case ScreenOrientation.Portrait: m_canvas_scaler.matchWidthOrHeight = 1f; break;
                case ScreenOrientation.Landscape: m_canvas_scaler.matchWidthOrHeight = 0f; break;

                default: break;
            }
        }
    }
    
    void Update()
    {
        if (Input.touchCount == 3)
        {
            ChangeDebuggerState();
        }
    }
    
    #endregion

    [ContextMenu("Debuger state")]
    void ChangeDebuggerState()
    {
        if (AllowConsole && !m_block_change)
        {
            m_block_change = true;
            m_change_state.gameObject.SetActive(!m_change_state.gameObject.activeSelf);
            
            this.TimerV(0.5f, () => m_block_change = false);
        }
    }

    #region ConsoleLogs

    void AddLog(string condition, string stackTrace, LogType type)
    {
        var log_entry = new LogEntry()
        {
            LogType = type,
            Condition = condition,
            StackTrace = stackTrace,
            Time = DateTime.Now.TimeOfDay
        };

        m_logs.Add(log_entry);

        if (m_logs.Count > MAX_CAPACITY)
        {
            m_logs.RemoveAt(0);
        }

        if (StopReceiving || !m_allowed_logs.Contains(type))
        {
            return;
        }

        m_sorted_logs.Add(log_entry);

        if (m_sorted_logs.Count > MAX_CAPACITY)
        {
            m_sorted_logs.RemoveAt(0);
        }
        
        // Force update ui
        CurrentPage = CurrentPage;
    }

    void UpdateCurrentPage()
    {
        var from = CurrentPage * m_max_elements;
        var to = (CurrentPage + 1) * m_max_elements;
        
        for (int i = from; i < to; i++)
        {
            InitItem(i);
        }
    }

    void InitItem(int log_id)
    {
        var item_id = log_id % m_max_elements;
        LogEntry log = null;
            
        if (log_id < m_sorted_logs.Count)
        {
            log = m_sorted_logs[log_id];
        }
        
        var new_item = InitItem(item_id, log);
        
        new_item.name = $"Log {log_id}{(log == null? "(Empty)":"")}";
    }

    LogItemView InitItem(int element_id, LogEntry log_entry)
    {
        var new_item = GetFromPool(element_id);
            
        new_item.name = $"Log {element_id}";
        new_item.Label.text = log_entry?.ToString();
        new_item.Button.onClick.RemoveAllListeners();
        new_item.Button.onClick.AddListener(() =>
        {
            if (log_entry != null)
            {
                var text = $"[{log_entry.Time:hh\\:mm\\:ss}] {log_entry.Condition}\n\n{log_entry?.StackTrace}";
                
                m_stack_text.text = text;
                m_stack_text.transform.parent.gameObject.SetActive(true);
            }
        });
        new_item.gameObject.SetActive(true);

        return new_item;
    }

    LogItemView GetFromPool(int pos_index)
    {
        //Debug.Log($"<b>Logger.GetFromPool</b> pos_index: {pos_index}");
        
        LogItemView new_item;
        var save_index = pos_index % m_max_elements;
        
        if (m_log_views.Count > save_index && m_log_views[save_index])
        {
            new_item = m_log_views[save_index];
        }
        else
        {
            var parent = m_main_scroll_contents[0].transform;
            new_item = Instantiate(log_pref, parent).GetComponent<LogItemView>();
        }

        return new_item;
    }

    public void SortText(bool state, LogType type)
    {
        if (state)
        {
            if (!m_allowed_logs.Contains(type))
            {
                m_allowed_logs.Add(type);
            }
        }
        else
        {
            if (m_allowed_logs.Contains(type))
            {
                m_allowed_logs.Remove(type);
            }
        }
        SortLogs();
    }

    public void SortLogs()
    {
        // var parent = m_main_scroll_contents[0].transform;
        //
        // for (var i = 0; i < m_logs.Count; i++)
        // {
        //     var current_entry = m_logs[i];
        //     var current_go = parent.GetChild(i).gameObject;
        //     var active = m_allowed_logs.Contains(current_entry.LogType);
        //
        //     current_go.SetActive(active);
        // }

        m_sorted_logs = m_logs.Where(x=>m_allowed_logs.Contains(x.LogType)).ToList();
        
        CurrentPage = 0;
        UpdateCurrentPage();
    }
    
    #endregion

    public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }

    #region DEBUG Methods

    [ContextMenu("DEBUG Log")]
    void DEBUG_Log()
    {
        Debug.Log($"<b>Logger.DEBUG_Log</b>");
    }
    
    [ContextMenu("DEBUG Log Error")]
    void DEBUG_LogError()
    {
        Debug.LogError($"<b>Logger.DEBUG_LogError</b>");
    }
    
    [ContextMenu("DEBUG Log Exception")]
    void DEBUG_LogException()
    {
        var nullReferenceException = new NullReferenceException("DEBUG_LogException");
        
        Debug.LogException(nullReferenceException);
    }

    #endregion
}
