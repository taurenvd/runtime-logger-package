using TMPro;
using UnityEngine;
using UnityEngine.UI;

using System.Collections;
using System.Collections.Generic;

public class LogItemView : MonoBehaviour
{
    [SerializeField] Button m_button;
    [Space]
    [SerializeField] TextMeshProUGUI m_text;

    #region Properties

    public Button Button => m_button;
    public TextMeshProUGUI Label => m_text;
    
    #endregion

}
