using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Data;

namespace UI
{
    public class PoiMarker : MonoBehaviour
    {
        [SerializeField] TMP_Text name;
        [SerializeField] Image icon;
        [SerializeField] Button button;

        Transform _t;
        public Transform t
        {
            get
            {
                if (_t == null)
                    _t = transform;
                return _t;
            }
        }

        public void SetTextActive(bool active)
        {
            name.enabled = active;
        }

        public void Setup(PoiData data, Sprite sprite)
        {
            name.text = data.name;
            icon.sprite = sprite;
            switch (data.type)
            {
                case PoiType.Red :
                    icon.color = Color.red;
                    break;
                case PoiType.Green :
                    icon.color = Color.green;
                    break;
                case PoiType.Blue :
                    icon.color = Color.blue;
                    break;
            }
        }

        public void Clean()
        {
            name.text = "";
            icon.sprite = null;
            button.onClick.RemoveAllListeners();
        }

        public void SetOnClickBehaviour(Action action)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action());
        }
    }
}
