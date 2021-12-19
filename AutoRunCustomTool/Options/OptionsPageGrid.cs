using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace ThomasLevesque.AutoRunCustomTool.Options
{
    public class OptionsPageGrid : DialogPage
    {
        [Category("AutoRunCustomTool")]
        [DisplayName("Trigger on extensions")]
        [Description("What extension(s) trigger \"Target files\" to run custom tools")]
        public string TriggerExtenstions { get; set; }

        [Category("AutoRunCustomTool")]
        [DisplayName("Target files")]
        [Description("The files to trigger custom tools on, relative to project path")]
        public string TargetFiles { get; set; }
    }
}
