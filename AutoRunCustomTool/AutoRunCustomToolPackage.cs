using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using VSLangProj;
using IExtenderProvider = EnvDTE.IExtenderProvider;
using System.Threading;
using System.Threading.Tasks;

namespace ThomasLevesque.AutoRunCustomTool
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidAutoRunCustomToolPkgString)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class AutoRunCustomToolPackage : AsyncPackage
    {
        public AutoRunCustomToolPackage()
        {
            Debug.WriteLine("Entering constructor for: {0}", this);
        }

        #region Package Members

        private DTE _dte;
        private Events _events;
        private DocumentEvents _documentEvents;
        private OutputWindowPane _outputPane;
        private ErrorListProvider _errorListProvider;
        private readonly Dictionary<int, IExtenderProvider> _registerExtenderProviders = new Dictionary<int, IExtenderProvider>();

        public const string TargetsPropertyName = "RunCustomToolOn";

        void RegisterExtenderProvider()
        {
            var provider = new PropertyExtenderProvider(_dte, this);
            string name = PropertyExtenderProvider.ExtenderName;
            RegisterExtenderProvider(VSConstants.CATID.CSharpFileProperties_string, name, provider);
            RegisterExtenderProvider(VSConstants.CATID.VBFileProperties_string, name, provider);
        }

        void RegisterExtenderProvider(string extenderCatId, string name, IExtenderProvider extenderProvider)
        {
            int cookie = _dte.ObjectExtenders.RegisterExtenderProvider(extenderCatId, name, extenderProvider);
            _registerExtenderProviders.Add(cookie, extenderProvider);
        }

        void DocumentEvents_DocumentSaved(Document doc)
        {
            var docItem = doc.ProjectItem;
            if (docItem == null)
                return;

            string docFullPath = (string)GetPropertyValue(docItem, "FullPath");

            var projectName = docItem.ContainingProject.UniqueName;
            IVsSolution solution = (IVsSolution)GetGlobalService(typeof(SVsSolution));
            IVsHierarchy project;
            solution.GetProjectOfUniqueName(projectName, out project);

            var docErrors = _errorListProvider.Tasks.Cast<ErrorTask>().Where(t => t.Document == docFullPath).ToList();
            foreach (var errorTask in docErrors)
            {
                _errorListProvider.Tasks.Remove(errorTask);
            }

            var targets = new List<string>();

            string customTool = GetPropertyValue(docItem, "CustomTool") as string;
            if (customTool == "AutoRunCustomTool")
            {
                LogWarning(project, docFullPath, "Setting Custom Tool to 'AutoRunCustomTool' is still supported for compatibility, but is deprecated. Use the 'Run custom tool on' property instead");
                string targetName = GetPropertyValue(docItem, "CustomToolNamespace") as string;
                if (string.IsNullOrEmpty(targetName))
                {
                    LogError(project, docFullPath, "The target file is not specified. Enter its relative path in the 'Custom tool namespace' property");
                    return;
                }
                targets.Add(targetName);
            }
            else
            {
                IVsBuildPropertyStorage storage = project as IVsBuildPropertyStorage;
                if (storage == null)
                    return;

                uint itemId;
                if (project.ParseCanonicalName(docFullPath, out itemId) != 0)
                    return;

                string runCustomToolOn;
                if (storage.GetItemAttribute(itemId, TargetsPropertyName, out runCustomToolOn) != 0)
                    return;

                if (runCustomToolOn == null)
                    return;
                targets.AddRange(runCustomToolOn.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            foreach (var targetName in targets)
            {
                string dir = Path.GetDirectoryName(docFullPath);
                // ReSharper disable once AssignNullToNotNullAttribute
                string targetPath = Path.GetFullPath(Path.Combine(dir, targetName));
                var targetItem = _dte.Solution.FindProjectItem(targetPath);
                if (targetItem == null)
                {
                    LogError(project, docFullPath, "Target item '{0}' was not found", targetPath);
                    continue;
                }

                string targetCustomTool = (string)GetPropertyValue(targetItem, "CustomTool");
                if (string.IsNullOrEmpty(targetCustomTool))
                {
                    LogError(project, docFullPath, "Target item '{0}' doesn't define a custom tool", targetPath);
                    continue;
                }

                var vsTargetItem = (VSProjectItem)targetItem.Object;
                LogActivity("Running custom tool on '{0}'", targetPath);
                vsTargetItem.RunCustomTool();
            }
        }

        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Debug.WriteLine("Entering Initialize() of: {0}", this);
            await base.InitializeAsync(cancellationToken, progress);
            _dte = (DTE)(await GetServiceAsync(typeof(DTE)));
            _events = _dte.Events;
            _documentEvents = _events.DocumentEvents;
            _documentEvents.DocumentSaved += DocumentEvents_DocumentSaved;

            var window = _dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);

            var outputWindow = (OutputWindow)window.Object;

            _outputPane = outputWindow.OutputWindowPanes
                                      .Cast<OutputWindowPane>()
                                      .FirstOrDefault(p => p.Name == "AutoRunCustomTool")
                          ?? outputWindow.OutputWindowPanes.Add("AutoRunCustomTool");
            _errorListProvider = new ErrorListProvider(this)
            {
                ProviderName = "AutoRunCustomTool",
                ProviderGuid = Guid.NewGuid()
            };
            RegisterExtenderProvider();
        }

        private void LogActivity(string format, params object[] args)
        {
            _outputPane.Activate();
            _outputPane.OutputString(string.Format(format, args) + Environment.NewLine);
        }

        private void LogError(IVsHierarchy project, string document, string format, params object[] args)
        {
            string text = string.Format(format, args);
            LogErrorTask(project, document, TaskErrorCategory.Error, text);
        }

        private void LogWarning(IVsHierarchy project, string document, string format, params object[] args)
        {
            string text = string.Format(format, args);
            LogErrorTask(project, document, TaskErrorCategory.Warning, text);
        }

        private void LogErrorTask(IVsHierarchy project, string document, TaskErrorCategory errorCategory, string text)
        {
            var task = new ErrorTask
            {
                Category = TaskCategory.BuildCompile,
                ErrorCategory = errorCategory,
                Text = "AutoRunCustomTool: " + text,
                Document = document,
                HierarchyItem = project,
                Line = -1,
                Column = -1
            };
            _errorListProvider.Tasks.Add(task);
            string prefix = "";
            switch (errorCategory)
            {
                case TaskErrorCategory.Error:
                    prefix = "Error: ";
                    break;
                case TaskErrorCategory.Warning:
                    prefix = "Warning: ";
                    break;
            }
            _outputPane.OutputString(prefix + text + Environment.NewLine);            
        }

        private static object GetPropertyValue(ProjectItem item, object index)
        {
            try
            {
                var prop = item.Properties.Item(index);
                if (prop != null)
                    return prop.Value;
            }
            catch (ArgumentException) { }
            return null;
        }

        #endregion
    }
}
