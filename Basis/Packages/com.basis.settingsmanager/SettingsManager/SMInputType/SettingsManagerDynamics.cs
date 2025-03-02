using System;

namespace BattlePhaze.SettingsManager
{
    public static class SettingsManagerDynamics
    {
        public static void DynamicExecution(int OptionIndex, SettingsManager Manager, int CurrentIndex)
        {
            if (Manager == null)
                throw new ArgumentNullException(nameof(Manager), "SettingsManager instance cannot be null.");

            if (Manager.Options == null)
                throw new ArgumentNullException(nameof(Manager.Options), "Options list cannot be null.");

            if (OptionIndex < 0 || OptionIndex >= Manager.Options.Count)
                throw new ArgumentOutOfRangeException(nameof(OptionIndex), "OptionIndex is out of range.");

            var option = Manager.Options[OptionIndex];
            if (option == null)
                throw new ArgumentNullException(nameof(option), "Option cannot be null.");

            if (option.SelectableValueList == null)
                throw new ArgumentNullException(nameof(option.SelectableValueList), "SelectableValueList cannot be null.");

            if (CurrentIndex < 0 || CurrentIndex >= option.SelectableValueList.Count)
                throw new ArgumentOutOfRangeException(nameof(CurrentIndex), "CurrentIndex is out of range.");

            option.SelectedValue = option.SelectableValueList[CurrentIndex]?.RealValue;

            SettingsManagerDescriptionSystem.TxtDescriptionSetText(Manager, OptionIndex);
            SettingsManagerStorageManagement.Save(Manager);
            Manager.SendOption(option);
        }
    }
}
