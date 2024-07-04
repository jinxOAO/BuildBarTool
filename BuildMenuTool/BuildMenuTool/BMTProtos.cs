using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonAPI.Systems.ModLocalization;

namespace BuildBarTool
{
    internal class BBTProtos
    {
        public static void AddLocalizationProtos()
        {
            LocalizationModule.RegisterTranslation("gmLockedItemText", "Locked  ", "锁定", "Locked  ");
            LocalizationModule.RegisterTranslation("切换快捷键", "CapsLock\n↑ Hotkey Row ↓", "CapsLock\n↑快捷键切换↓", "CapsLock\n↑ Hotkey Row ↓");
        }

    }
}
