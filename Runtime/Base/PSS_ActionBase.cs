using UdonSharp;
using UnityEngine;

namespace PuruSignals
{
    public abstract class PSS_ActionBase : PSS_ModuleBase
    {
        // Заполняется редактором (PSS_ChannelEditor).
        // Хранит ссылку на канал для доступа к triggeredPlayer и т.п.
        [HideInInspector] public PSS_ChannelLocal channel;
        [HideInInspector] public int priority = 0;
        [HideInInspector] public float weight = 1f;

        // Вызывается PSS_ChannelLocal через прямой вызов метода.
        // Не переопределять в наследниках — реализовывать OnExecute().
        public void Execute()
        {
            OnExecute();
        }

        protected abstract void OnExecute();
    }
}
