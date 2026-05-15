using UdonSharp;
using UnityEngine;

namespace PuruSignals
{
    public enum LtcgiMode { Global, PerScreen }
    public enum LtcgiOp   { True, False, Toggle }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [PSS_Module("Action/LTCGI/LtcgiControl")]
    [AddComponentMenu("PSS/Actions/LTCGI/PSS_LtcgiControl [Action]")]
    public class PSS_LtcgiControl : PSS_ActionBase
    {
        [PSS_Field("Adapter", tooltip: "LTCGI_UdonAdapter из сцены")]
        public LTCGI_UdonAdapter adapter;

        [PSS_Field("Mode", tooltip: "Global — вся система, PerScreen — конкретные экраны")]
        public LtcgiMode mode = LtcgiMode.Global;

        [PSS_Field("Operation")]
        public LtcgiOp operation = LtcgiOp.Toggle;

        [PSS_Field("Screens", isList: true, tooltip: "Только для режима PerScreen. Один LTCGI_Screen на объект.")]
        public GameObject[] screens;

        // Кэш per-screen — заполняется в Start(), _GetIndex вызывать только там
        private int[]   _indices;
        private Color[] _onColors;
        private bool[]  _screenStates;

        // Состояние глобального режима
        private bool _globalState = true;

        private void Start()
        {
            if (mode != LtcgiMode.PerScreen || adapter == null || screens == null) return;

            _indices      = new int[screens.Length];
            _onColors     = new Color[screens.Length];
            _screenStates = new bool[screens.Length];

            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i] == null) { _indices[i] = -1; continue; }
                _indices[i]      = adapter._GetIndex(screens[i]);
                _onColors[i]     = adapter._GetColor(_indices[i]);
                _screenStates[i] = true;
            }
        }

        protected override void OnExecute()
        {
            if (adapter == null) return;

            if (mode == LtcgiMode.Global)
            {
                switch (operation)
                {
                    case LtcgiOp.True:   _globalState = true;          break;
                    case LtcgiOp.False:  _globalState = false;         break;
                    case LtcgiOp.Toggle: _globalState = !_globalState; break;
                }
                adapter._SetGlobalState(_globalState);
            }
            else
            {
                if (_indices == null) return;

                for (int i = 0; i < _indices.Length; i++)
                {
                    if (_indices[i] < 0) continue;

                    bool next;
                    switch (operation)
                    {
                        case LtcgiOp.True:   next = true;               break;
                        case LtcgiOp.False:  next = false;              break;
                        case LtcgiOp.Toggle: next = !_screenStates[i];  break;
                        default:             next = true;               break;
                    }
                    _screenStates[i] = next;
                    adapter._SetColor(_indices[i], next ? _onColors[i] : Color.black);
                }
            }
        }
    }
}
