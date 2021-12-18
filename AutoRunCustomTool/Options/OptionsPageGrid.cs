using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace ThomasLevesque.AutoRunCustomTool.Options
{
    public class OptionsPageGrid : DialogPage
    {
        [Category("AutoRunCustomTool")]
        [DisplayName("Extensions")]
        [Description("What extension(s) to run the custom tool on")]
        public string ListenToExtension { get; set; }

        [Category("AutoRunCustomTool")]
        [DisplayName("File to run tool on")]
        [Description("The file that we should trigger a custom tool on save")]
        public string ToolToRun { get; set; }
    }
}
