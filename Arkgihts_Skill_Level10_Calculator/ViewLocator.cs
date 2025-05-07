using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Arkgihts_Skill_Level10_Calculator.ViewModels;

namespace Arkgihts_Skill_Level10_Calculator;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        /*var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }*/

        return new TextBlock { Text = "Not Found: " };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}